using Microsoft.Extensions.Logging;
using YekAbr.Domain.Entities;
using YekAbr.Domain.Enums;
using YekAbr.Domain.Interfaces;
using YekAbr.Domain.Models;
using YekAbr.Infrastructure.Services.Cloud;
using YekAbr.Services.DTOs.Cloud;
using YekAbr.Services.Interfaces.Cloud;
using YekAbr.Services.Interfaces.Transfers;

namespace YekAbr.Infrastructure.Services.Transfers;

public sealed class ProviderSyncExecutor : IProviderSyncExecutor
{
    private readonly IProviderSyncOperationRepository _operationRepository;
    private readonly ICloudAccountCredentialService _credentialService;
    private readonly ICloudProviderClientFactory _providerFactory;
    private readonly IUploadedFileMetadataRepository _metadataRepository;
    private readonly IConnectedCloudAccountRepository _accountRepository;
    private readonly ILogger<ProviderSyncExecutor> _logger;

    public ProviderSyncExecutor(
        IProviderSyncOperationRepository operationRepository,
        ICloudAccountCredentialService credentialService,
        ICloudProviderClientFactory providerFactory,
        IUploadedFileMetadataRepository metadataRepository,
        IConnectedCloudAccountRepository accountRepository,
        ILogger<ProviderSyncExecutor> logger)
    {
        _operationRepository = operationRepository;
        _credentialService = credentialService;
        _providerFactory = providerFactory;
        _metadataRepository = metadataRepository;
        _accountRepository = accountRepository;
        _logger = logger;
    }

    public async Task ExecuteAsync(Guid operationId, CancellationToken cancellationToken = default)
    {
        var operation = await _operationRepository.GetByIdAsync(operationId, cancellationToken);
        if (operation is null)
        {
            _logger.LogWarning("Provider sync operation {OperationId} was not found.", operationId);
            return;
        }

        if (operation.Status == ProviderSyncOperationStatus.Pending)
        {
            operation.Status = ProviderSyncOperationStatus.Running;
            operation.StartedAtUtc ??= DateTime.UtcNow;
            operation.UpdatedAtUtc = DateTime.UtcNow;
            _operationRepository.Update(operation);
            await _operationRepository.SaveChangesAsync(cancellationToken);
        }

        if (operation.Status != ProviderSyncOperationStatus.Running)
        {
            return;
        }

        try
        {
            var sourceAccount = operation.SourceConnectedCloudAccount
                ?? await RequireOwnedActiveAccountAsync(operation.UserId, operation.SourceConnectedCloudAccountId, cancellationToken);
            var destinationAccount = operation.DestinationConnectedCloudAccount
                ?? await RequireOwnedActiveAccountAsync(operation.UserId, operation.DestinationConnectedCloudAccountId, cancellationToken);

            var sourceProvider = _providerFactory.GetFileProvider(sourceAccount.Provider);
            var destinationProvider = _providerFactory.GetFileProvider(destinationAccount.Provider);

            var sourceToken = await _credentialService.GetValidAccessTokenAsync(sourceAccount, cancellationToken);
            var destinationToken = await _credentialService.GetValidAccessTokenAsync(destinationAccount, cancellationToken);

            var rootParentId = string.IsNullOrWhiteSpace(sourceAccount.RootFolderId) ? null : sourceAccount.RootFolderId;
            var sourceFiles = await CloudProviderFileEnumerator.ListAllFilesRecursiveAsync(
                sourceProvider,
                sourceToken,
                rootParentId,
                cancellationToken);

            operation.TotalFiles = sourceFiles.Count;
            operation.SucceededFiles = 0;
            operation.FailedFiles = 0;
            operation.SkippedFiles = 0;
            operation.UpdatedAtUtc = DateTime.UtcNow;
            _operationRepository.Update(operation);
            await _operationRepository.SaveChangesAsync(cancellationToken);

            var destinationRoot = NormalizeParentId(destinationAccount.RootFolderId);

            foreach (var sourceFile in sourceFiles)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    await TransferSingleFileAsync(
                        operation,
                        sourceProvider,
                        destinationProvider,
                        sourceToken,
                        destinationToken,
                        sourceAccount,
                        destinationAccount,
                        sourceFile,
                        destinationRoot,
                        cancellationToken);
                }
                catch (Exception fileException)
                {
                    operation.FailedFiles++;
                    operation.UpdatedAtUtc = DateTime.UtcNow;
                    _operationRepository.Update(operation);
                    await _operationRepository.SaveChangesAsync(cancellationToken);
                    _logger.LogWarning(
                        fileException,
                        "Provider sync file transfer failed. Operation {OperationId}, SourceFile {SourceFileId}.",
                        operation.Id,
                        sourceFile.Id);
                }
            }

            operation = await _operationRepository.GetByIdAsync(operationId, cancellationToken) ?? operation;
            operation.CompletedAtUtc = DateTime.UtcNow;
            operation.UpdatedAtUtc = DateTime.UtcNow;

            if (operation.FailedFiles == 0 && operation.SucceededFiles + operation.SkippedFiles >= operation.TotalFiles)
            {
                operation.Status = ProviderSyncOperationStatus.Completed;
                operation.ErrorMessage = null;
            }
            else if (operation.SucceededFiles > 0)
            {
                operation.Status = ProviderSyncOperationStatus.PartiallyCompleted;
                operation.ErrorMessage = "برخی از فایل‌ها با خطا مواجه شدند.";
            }
            else
            {
                operation.Status = ProviderSyncOperationStatus.Failed;
                operation.ErrorMessage ??= "انتقال فایل‌ها ناموفق بود.";
            }

            _operationRepository.Update(operation);
            await _operationRepository.SaveChangesAsync(cancellationToken);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Provider sync operation {OperationId} failed.", operationId);
            operation = await _operationRepository.GetByIdAsync(operationId, cancellationToken) ?? operation;
            operation.Status = ProviderSyncOperationStatus.Failed;
            operation.ErrorMessage = exception is InvalidOperationException
                ? exception.Message
                : "اجرای همگام‌سازی بین ارائه‌دهنده‌ها ناموفق بود.";
            operation.CompletedAtUtc = DateTime.UtcNow;
            operation.UpdatedAtUtc = DateTime.UtcNow;
            _operationRepository.Update(operation);
            await _operationRepository.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task TransferSingleFileAsync(
        ProviderSyncOperation operation,
        ICloudFileProviderClient sourceProvider,
        ICloudFileProviderClient destinationProvider,
        string sourceToken,
        string destinationToken,
        ConnectedCloudAccount sourceAccount,
        ConnectedCloudAccount destinationAccount,
        CloudItem sourceFile,
        string destinationParentId,
        CancellationToken cancellationToken)
    {
        await using var download = await sourceProvider.DownloadFileAsync(
            sourceToken,
            sourceFile.Id,
            cancellationToken);

        var uploaded = await destinationProvider.UploadFileAsync(
            destinationToken,
            new UploadCloudFileRequest
            {
                Content = download.Content,
                FileName = download.FileName,
                ContentType = download.ContentType,
                ParentFolderId = destinationParentId,
                ContentLength = download.ContentLength ?? sourceFile.Size
            },
            cancellationToken);

        await UpsertDestinationMetadataAsync(
            operation.UserId,
            destinationAccount,
            uploaded,
            download.FileName,
            download.ContentType,
            cancellationToken);

        operation.SucceededFiles++;
        operation.UpdatedAtUtc = DateTime.UtcNow;
        _operationRepository.Update(operation);
        await _operationRepository.SaveChangesAsync(cancellationToken);

        // Source metadata remains as-is; destination side is what dashboard needs for the copied file.
        _ = sourceAccount;
    }

    private async Task UpsertDestinationMetadataAsync(
        string userId,
        ConnectedCloudAccount destinationAccount,
        CloudItem uploaded,
        string originalFileName,
        string? contentType,
        CancellationToken cancellationToken)
    {
        var existing = await _metadataRepository.GetByProviderFileAsync(
            userId,
            destinationAccount.Id,
            uploaded.Id,
            includeDeleted: true,
            cancellationToken);

        if (existing is null)
        {
            var created = UploadedFileMetadataMapper.CreateFromCloudItem(
                userId,
                destinationAccount,
                uploaded,
                originalFileName,
                contentType);
            await _metadataRepository.AddAsync(created, cancellationToken);
        }
        else
        {
            UploadedFileMetadataMapper.ApplyCloudItemUpdates(existing, uploaded, destinationAccount);
            if (string.IsNullOrWhiteSpace(existing.OriginalFileName))
            {
                existing.OriginalFileName = originalFileName;
            }

            if (!string.IsNullOrWhiteSpace(contentType))
            {
                existing.ContentType = contentType;
            }

            _metadataRepository.Update(existing);
        }

        await _metadataRepository.SaveChangesAsync(cancellationToken);
    }

    private async Task<ConnectedCloudAccount> RequireOwnedActiveAccountAsync(
        string userId,
        Guid accountId,
        CancellationToken cancellationToken)
    {
        var account = await _credentialService.GetOwnedActiveAccountAsync(userId, accountId, cancellationToken);
        if (account is null)
        {
            // Fallback read for inactive check messaging
            var raw = await _accountRepository.GetByIdAsync(accountId, cancellationToken);
            if (raw is null || raw.UserId != userId)
            {
                throw new InvalidOperationException("حساب ابری مورد نظر یافت نشد.");
            }

            throw new InvalidOperationException("یکی از حساب‌های مبدأ یا مقصد غیرفعال است.");
        }

        return account;
    }

    private static string NormalizeParentId(string? rootFolderId)
    {
        return rootFolderId ?? string.Empty;
    }
}
