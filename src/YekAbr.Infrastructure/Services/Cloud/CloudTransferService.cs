using FluentValidation;
using Microsoft.Extensions.Logging;
using YekAbr.Domain.Entities;
using YekAbr.Domain.Enums;
using YekAbr.Domain.Interfaces;
using YekAbr.Services.Common.Responses;
using YekAbr.Services.DTOs.Cloud;
using YekAbr.Services.Interfaces.Cloud;
using YekAbr.Services.Services.Auth;

namespace YekAbr.Infrastructure.Services.Cloud;

public sealed class CloudTransferService : ICloudTransferService
{
    private readonly ICloudTransferJobRepository _jobRepository;
    private readonly ICloudAccountCredentialService _credentialService;
    private readonly ICloudProviderClientFactory _providerFactory;
    private readonly ICloudTransferJobQueue _jobQueue;
    private readonly IValidator<CreateCloudTransferJobRequest> _createValidator;
    private readonly ILogger<CloudTransferService> _logger;

    public CloudTransferService(
        ICloudTransferJobRepository jobRepository,
        ICloudAccountCredentialService credentialService,
        ICloudProviderClientFactory providerFactory,
        ICloudTransferJobQueue jobQueue,
        IValidator<CreateCloudTransferJobRequest> createValidator,
        ILogger<CloudTransferService> logger)
    {
        _jobRepository = jobRepository;
        _credentialService = credentialService;
        _providerFactory = providerFactory;
        _jobQueue = jobQueue;
        _createValidator = createValidator;
        _logger = logger;
    }

    public async Task<Result<CloudTransferJobDto>> CreateAsync(
        string userId,
        CreateCloudTransferJobRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Result<CloudTransferJobDto>.Failed("کاربر احراز هویت نشده است.");
        }

        var validationErrors = await ValidationHelper.ValidateAsync(_createValidator, request, cancellationToken);
        if (validationErrors is not null)
        {
            return Result<CloudTransferJobDto>.Failed("اعتبارسنجی ناموفق بود.", validationErrors);
        }

        if (request.SourceConnectedAccountId == request.DestinationConnectedAccountId
            && !string.IsNullOrWhiteSpace(request.DestinationParentFolderId)
            && string.Equals(request.SourceItemId, request.DestinationParentFolderId, StringComparison.Ordinal))
        {
            return Result<CloudTransferJobDto>.Failed("نمی‌توان یک پوشه را به داخل خودش کپی کرد.");
        }

        var sourceAccount = await _credentialService.GetOwnedActiveAccountAsync(
            userId,
            request.SourceConnectedAccountId,
            cancellationToken);
        if (sourceAccount is null)
        {
            return Result<CloudTransferJobDto>.Failed("حساب ابری مبدأ یافت نشد یا غیرفعال است.");
        }

        var destinationAccount = await _credentialService.GetOwnedActiveAccountAsync(
            userId,
            request.DestinationConnectedAccountId,
            cancellationToken);
        if (destinationAccount is null)
        {
            return Result<CloudTransferJobDto>.Failed("حساب ابری مقصد یافت نشد یا غیرفعال است.");
        }

        if (!_providerFactory.IsSupported(sourceAccount.Provider)
            || !_providerFactory.IsSupported(destinationAccount.Provider))
        {
            return Result<CloudTransferJobDto>.Failed("یکی از ارائه‌دهندگان مبدأ یا مقصد هنوز پشتیبانی نمی‌شود.");
        }

        try
        {
            var sourceProvider = _providerFactory.GetFileProvider(sourceAccount.Provider);
            var destinationProvider = _providerFactory.GetFileProvider(destinationAccount.Provider);

            var sourceToken = await _credentialService.GetValidAccessTokenAsync(sourceAccount, cancellationToken);
            var destinationToken = await _credentialService.GetValidAccessTokenAsync(destinationAccount, cancellationToken);

            var sourceItem = await sourceProvider.GetItemAsync(sourceToken, request.SourceItemId, cancellationToken);

            var destinationParentId = NormalizeParentId(request.DestinationParentFolderId, destinationAccount.RootFolderId);
            if (!string.IsNullOrEmpty(destinationParentId)
                && !string.Equals(destinationParentId, "root", StringComparison.OrdinalIgnoreCase))
            {
                var destinationParent = await destinationProvider.GetItemAsync(
                    destinationToken,
                    destinationParentId,
                    cancellationToken);

                if (destinationParent.ItemType != CloudItemType.Folder)
                {
                    return Result<CloudTransferJobDto>.Failed("مقصد باید یک پوشه باشد.");
                }
            }

            var now = DateTime.UtcNow;
            var job = new CloudTransferJob
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                SourceConnectedCloudAccountId = sourceAccount.Id,
                DestinationConnectedCloudAccountId = destinationAccount.Id,
                SourceItemId = sourceItem.Id,
                SourceItemName = sourceItem.Name,
                SourceItemType = sourceItem.ItemType,
                DestinationParentFolderId = destinationParentId,
                Status = CloudTransferStatus.Pending,
                ProgressPercentage = 0,
                TotalItems = 0,
                ProcessedItems = 0,
                TotalBytes = sourceItem.Size,
                TransferredBytes = 0,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            };

            await _jobRepository.AddAsync(job, cancellationToken);
            await _jobRepository.SaveChangesAsync(cancellationToken);

            // Reload with navigation for response mapping.
            job = await _jobRepository.GetByIdForUserAsync(job.Id, userId, cancellationToken) ?? job;

            await _jobQueue.EnqueueAsync(job.Id, cancellationToken);

            return Result<CloudTransferJobDto>.Succeeded(
                CloudTransferJobMapper.Map(job, _providerFactory),
                "جاب انتقال با موفقیت ایجاد شد و در صف اجرا قرار گرفت.");
        }
        catch (InvalidOperationException exception)
        {
            _logger.LogWarning(exception, "Failed to create transfer job for user {UserId}.", userId);
            return Result<CloudTransferJobDto>.Failed(exception.Message);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Unexpected failure creating transfer job for user {UserId}.", userId);
            return Result<CloudTransferJobDto>.Failed("ایجاد جاب انتقال ناموفق بود.");
        }
    }

    public async Task<Result<CloudTransferJobDto>> GetByIdAsync(
        string userId,
        Guid jobId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Result<CloudTransferJobDto>.Failed("کاربر احراز هویت نشده است.");
        }

        var job = await _jobRepository.GetByIdForUserAsync(jobId, userId, cancellationToken);
        if (job is null)
        {
            return Result<CloudTransferJobDto>.Failed("جاب انتقال مورد نظر یافت نشد.");
        }

        return Result<CloudTransferJobDto>.Succeeded(
            CloudTransferJobMapper.Map(job, _providerFactory),
            "جزئیات جاب انتقال با موفقیت دریافت شد.");
    }

    public async Task<Result<IReadOnlyList<CloudTransferJobDto>>> ListAsync(
        string userId,
        CloudTransferStatus? status = null,
        Guid? sourceConnectedAccountId = null,
        Guid? destinationConnectedAccountId = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Result<IReadOnlyList<CloudTransferJobDto>>.Failed("کاربر احراز هویت نشده است.");
        }

        var jobs = await _jobRepository.ListAsync(
            userId,
            status,
            sourceConnectedAccountId,
            destinationConnectedAccountId,
            page,
            pageSize,
            cancellationToken);

        var dtoList = jobs.Select(x => CloudTransferJobMapper.Map(x, _providerFactory)).ToList();
        return Result<IReadOnlyList<CloudTransferJobDto>>.Succeeded(
            dtoList,
            "لیست جاب‌های انتقال با موفقیت دریافت شد.");
    }

    public async Task<Result<object>> CancelAsync(
        string userId,
        Guid jobId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Result<object>.Failed("کاربر احراز هویت نشده است.");
        }

        var job = await _jobRepository.GetByIdForUserAsync(jobId, userId, cancellationToken);
        if (job is null)
        {
            return Result<object>.Failed("جاب انتقال مورد نظر یافت نشد.");
        }

        if (job.Status is not (CloudTransferStatus.Pending or CloudTransferStatus.Running))
        {
            return Result<object>.Failed("فقط جاب‌های در انتظار یا در حال اجرا قابل لغو هستند.");
        }

        var now = DateTime.UtcNow;
        job.CancellationRequestedAtUtc ??= now;
        job.UpdatedAtUtc = now;

        if (job.Status == CloudTransferStatus.Pending)
        {
            job.Status = CloudTransferStatus.Cancelled;
            job.CompletedAtUtc = now;
            job.FailureReason = "جاب توسط کاربر لغو شد.";
        }

        _jobRepository.Update(job);
        await _jobRepository.SaveChangesAsync(cancellationToken);

        return Result<object>.Succeeded(
            new { },
            job.Status == CloudTransferStatus.Cancelled
                ? "جاب انتقال با موفقیت لغو شد."
                : "درخواست لغو ثبت شد و در اولین فرصت ایمن اعمال می‌شود.");
    }

    private static string NormalizeParentId(string? parentId, string? rootFolderId)
    {
        if (!string.IsNullOrWhiteSpace(parentId))
        {
            return parentId;
        }

        return rootFolderId ?? string.Empty;
    }
}
