using Microsoft.Extensions.Logging;
using YekAbr.Domain.Entities;
using YekAbr.Domain.Enums;
using YekAbr.Domain.Interfaces;
using YekAbr.Domain.Models;
using YekAbr.Services.DTOs.Cloud;
using YekAbr.Services.Interfaces.Cloud;

namespace YekAbr.Infrastructure.Services.Cloud;

public sealed class CloudTransferExecutor : ICloudTransferExecutor
{
    private readonly ICloudTransferJobRepository _jobRepository;
    private readonly ICloudAccountCredentialService _credentialService;
    private readonly ICloudProviderClientFactory _providerFactory;
    private readonly ILogger<CloudTransferExecutor> _logger;

    public CloudTransferExecutor(
        ICloudTransferJobRepository jobRepository,
        ICloudAccountCredentialService credentialService,
        ICloudProviderClientFactory providerFactory,
        ILogger<CloudTransferExecutor> logger)
    {
        _jobRepository = jobRepository;
        _credentialService = credentialService;
        _providerFactory = providerFactory;
        _logger = logger;
    }

    public async Task ExecuteAsync(Guid jobId, CancellationToken cancellationToken = default)
    {
        var job = await _jobRepository.GetByIdAsync(jobId, cancellationToken);
        if (job is null)
        {
            _logger.LogWarning("Transfer job {JobId} was not found.", jobId);
            return;
        }

        if (job.Status == CloudTransferStatus.Cancelled)
        {
            return;
        }

        if (job.Status == CloudTransferStatus.Pending)
        {
            job.Status = CloudTransferStatus.Running;
            job.StartedAtUtc ??= DateTime.UtcNow;
            job.UpdatedAtUtc = DateTime.UtcNow;
            _jobRepository.Update(job);
            await _jobRepository.SaveChangesAsync(cancellationToken);
        }

        if (job.Status != CloudTransferStatus.Running)
        {
            return;
        }

        try
        {
            if (await IsCancellationRequestedAsync(job.Id, cancellationToken))
            {
                await MarkCancelledAsync(job, cancellationToken);
                return;
            }

            var sourceAccount = job.SourceConnectedCloudAccount
                ?? await RequireOwnedActiveAccountAsync(job.UserId, job.SourceConnectedCloudAccountId, cancellationToken);
            var destinationAccount = job.DestinationConnectedCloudAccount
                ?? await RequireOwnedActiveAccountAsync(job.UserId, job.DestinationConnectedCloudAccountId, cancellationToken);

            if (!sourceAccount.IsActive || !destinationAccount.IsActive)
            {
                throw new InvalidOperationException("یکی از حساب‌های مبدأ یا مقصد غیرفعال است.");
            }

            var sourceProvider = _providerFactory.GetFileProvider(sourceAccount.Provider);
            var destinationProvider = _providerFactory.GetFileProvider(destinationAccount.Provider);

            var sourceToken = await _credentialService.GetValidAccessTokenAsync(sourceAccount, cancellationToken);
            var destinationToken = await _credentialService.GetValidAccessTokenAsync(destinationAccount, cancellationToken);

            var sourceItem = await sourceProvider.GetItemAsync(sourceToken, job.SourceItemId, cancellationToken);
            var destinationParentId = NormalizeParentId(job.DestinationParentFolderId, destinationAccount.RootFolderId);

            var totals = await CountItemsAsync(sourceProvider, sourceToken, sourceItem, cancellationToken);
            job.TotalItems = totals.ItemCount;
            job.TotalBytes = totals.ByteCount;
            job.ProcessedItems = 0;
            job.TransferredBytes = 0;
            job.ProgressPercentage = 0;
            job.UpdatedAtUtc = DateTime.UtcNow;
            _jobRepository.Update(job);
            await _jobRepository.SaveChangesAsync(cancellationToken);

            var context = new TransferExecutionContext(
                job,
                sourceProvider,
                destinationProvider,
                sourceAccount,
                destinationAccount);

            await CopyItemAsync(
                context,
                sourceItem,
                destinationParentId,
                cancellationToken);

            job = await ReloadJobAsync(job.Id, cancellationToken);
            if (job.CancellationRequestedAtUtc.HasValue && job.Status == CloudTransferStatus.Running)
            {
                await MarkCancelledAsync(job, cancellationToken);
                return;
            }

            if (job.Status == CloudTransferStatus.Running)
            {
                job.Status = CloudTransferStatus.Completed;
                job.ProgressPercentage = 100;
                job.CompletedAtUtc = DateTime.UtcNow;
                job.UpdatedAtUtc = DateTime.UtcNow;
                job.FailureReason = null;
                _jobRepository.Update(job);
                await _jobRepository.SaveChangesAsync(cancellationToken);
            }
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            // Cooperative cancellation from job cancel request.
            job = await ReloadJobAsync(jobId, cancellationToken);
            await MarkCancelledAsync(job, cancellationToken);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Transfer job {JobId} failed.", jobId);
            job = await ReloadJobAsync(jobId, CancellationToken.None);
            if (job.Status is CloudTransferStatus.Completed or CloudTransferStatus.Cancelled)
            {
                return;
            }

            job.Status = CloudTransferStatus.Failed;
            job.FailureReason = string.IsNullOrWhiteSpace(exception.Message)
                ? "انتقال فایل با خطای ناشناخته مواجه شد."
                : exception.Message;
            job.CompletedAtUtc = DateTime.UtcNow;
            job.UpdatedAtUtc = DateTime.UtcNow;
            _jobRepository.Update(job);
            await _jobRepository.SaveChangesAsync(CancellationToken.None);
        }
    }

    private async Task CopyItemAsync(
        TransferExecutionContext context,
        CloudItem sourceItem,
        string destinationParentId,
        CancellationToken cancellationToken)
    {
        await ThrowIfCancelledAsync(context.Job.Id, cancellationToken);

        if (sourceItem.ItemType == CloudItemType.Folder)
        {
            var destinationToken = await RefreshDestinationTokenAsync(context, cancellationToken);
            var createdFolder = await context.DestinationProvider.CreateFolderAsync(
                destinationToken,
                new CreateCloudFolderRequest
                {
                    Name = sourceItem.Name,
                    ParentFolderId = destinationParentId
                },
                cancellationToken);

            await MarkItemProcessedAsync(context.Job.Id, bytesTransferred: 0, cancellationToken);

            string? pageToken = null;
            do
            {
                await ThrowIfCancelledAsync(context.Job.Id, cancellationToken);

                var sourceToken = await RefreshSourceTokenAsync(context, cancellationToken);
                var page = await context.SourceProvider.ListItemsAsync(
                    sourceToken,
                    new ListCloudItemsRequest
                    {
                        ParentId = sourceItem.Id,
                        PageSize = 100,
                        PageToken = pageToken,
                        IncludeFiles = true,
                        IncludeFolders = true
                    },
                    cancellationToken);

                foreach (var child in page.Items)
                {
                    await CopyItemAsync(context, child, createdFolder.Id, cancellationToken);
                }

                pageToken = page.NextPageToken;
            }
            while (!string.IsNullOrWhiteSpace(pageToken));

            return;
        }

        var downloadToken = await RefreshSourceTokenAsync(context, cancellationToken);
        await using var download = await context.SourceProvider.DownloadFileAsync(
            downloadToken,
            sourceItem.Id,
            cancellationToken);

        var uploadToken = await RefreshDestinationTokenAsync(context, cancellationToken);
        await context.DestinationProvider.UploadFileAsync(
            uploadToken,
            new UploadCloudFileRequest
            {
                Content = download.Content,
                FileName = download.FileName,
                ContentType = download.ContentType,
                ParentFolderId = destinationParentId,
                ContentLength = download.ContentLength ?? sourceItem.Size
            },
            cancellationToken);

        await MarkItemProcessedAsync(
            context.Job.Id,
            bytesTransferred: download.ContentLength ?? sourceItem.Size ?? 0,
            cancellationToken);
    }

    private async Task<(int ItemCount, long ByteCount)> CountItemsAsync(
        ICloudFileProviderClient provider,
        string accessToken,
        CloudItem root,
        CancellationToken cancellationToken)
    {
        if (root.ItemType == CloudItemType.File)
        {
            return (1, root.Size ?? 0);
        }

        var itemCount = 1;
        long byteCount = 0;
        string? pageToken = null;

        do
        {
            var page = await provider.ListItemsAsync(
                accessToken,
                new ListCloudItemsRequest
                {
                    ParentId = root.Id,
                    PageSize = 100,
                    PageToken = pageToken,
                    IncludeFiles = true,
                    IncludeFolders = true
                },
                cancellationToken);

            foreach (var child in page.Items)
            {
                var childTotals = await CountItemsAsync(provider, accessToken, child, cancellationToken);
                itemCount += childTotals.ItemCount;
                byteCount += childTotals.ByteCount;
            }

            pageToken = page.NextPageToken;
        }
        while (!string.IsNullOrWhiteSpace(pageToken));

        return (itemCount, byteCount);
    }

    private async Task MarkItemProcessedAsync(Guid jobId, long bytesTransferred, CancellationToken cancellationToken)
    {
        var job = await ReloadJobAsync(jobId, cancellationToken);
        if (job.Status != CloudTransferStatus.Running)
        {
            return;
        }

        job.ProcessedItems = Math.Min(job.ProcessedItems + 1, Math.Max(job.TotalItems, job.ProcessedItems + 1));
        job.TransferredBytes = (job.TransferredBytes ?? 0) + Math.Max(0, bytesTransferred);

        if (job.TotalItems > 0)
        {
            job.ProgressPercentage = Math.Clamp(
                (int)Math.Floor(job.ProcessedItems * 100d / job.TotalItems),
                0,
                99);
        }

        job.UpdatedAtUtc = DateTime.UtcNow;
        _jobRepository.Update(job);
        await _jobRepository.SaveChangesAsync(cancellationToken);
    }

    private async Task ThrowIfCancelledAsync(Guid jobId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (await IsCancellationRequestedAsync(jobId, cancellationToken))
        {
            throw new OperationCanceledException("Transfer job cancellation was requested.");
        }
    }

    private async Task<bool> IsCancellationRequestedAsync(Guid jobId, CancellationToken cancellationToken)
    {
        var job = await _jobRepository.GetByIdAsync(jobId, cancellationToken);
        return job?.CancellationRequestedAtUtc.HasValue == true;
    }

    private async Task MarkCancelledAsync(CloudTransferJob job, CancellationToken cancellationToken)
    {
        if (job.Status is CloudTransferStatus.Completed or CloudTransferStatus.Failed or CloudTransferStatus.Cancelled)
        {
            return;
        }

        job.Status = CloudTransferStatus.Cancelled;
        job.FailureReason = "جاب توسط کاربر لغو شد.";
        job.CompletedAtUtc = DateTime.UtcNow;
        job.UpdatedAtUtc = DateTime.UtcNow;
        _jobRepository.Update(job);
        await _jobRepository.SaveChangesAsync(cancellationToken);
    }

    private async Task<ConnectedCloudAccount> RequireOwnedActiveAccountAsync(
        string userId,
        Guid accountId,
        CancellationToken cancellationToken)
    {
        var account = await _credentialService.GetOwnedActiveAccountAsync(userId, accountId, cancellationToken);
        return account ?? throw new InvalidOperationException("حساب ابری مرتبط با جاب یافت نشد یا غیرفعال است.");
    }

    private async Task<string> RefreshSourceTokenAsync(TransferExecutionContext context, CancellationToken cancellationToken)
    {
        return await _credentialService.GetValidAccessTokenAsync(context.SourceAccount, cancellationToken);
    }

    private async Task<string> RefreshDestinationTokenAsync(TransferExecutionContext context, CancellationToken cancellationToken)
    {
        return await _credentialService.GetValidAccessTokenAsync(context.DestinationAccount, cancellationToken);
    }

    private async Task<CloudTransferJob> ReloadJobAsync(Guid jobId, CancellationToken cancellationToken)
    {
        return await _jobRepository.GetByIdAsync(jobId, cancellationToken)
            ?? throw new InvalidOperationException("جاب انتقال مورد نظر یافت نشد.");
    }

    private static string NormalizeParentId(string? parentId, string? rootFolderId)
    {
        if (!string.IsNullOrWhiteSpace(parentId))
        {
            return parentId;
        }

        return rootFolderId ?? string.Empty;
    }

    private sealed class TransferExecutionContext
    {
        public TransferExecutionContext(
            CloudTransferJob job,
            ICloudFileProviderClient sourceProvider,
            ICloudFileProviderClient destinationProvider,
            ConnectedCloudAccount sourceAccount,
            ConnectedCloudAccount destinationAccount)
        {
            Job = job;
            SourceProvider = sourceProvider;
            DestinationProvider = destinationProvider;
            SourceAccount = sourceAccount;
            DestinationAccount = destinationAccount;
        }

        public CloudTransferJob Job { get; }
        public ICloudFileProviderClient SourceProvider { get; }
        public ICloudFileProviderClient DestinationProvider { get; }
        public ConnectedCloudAccount SourceAccount { get; }
        public ConnectedCloudAccount DestinationAccount { get; }
    }
}
