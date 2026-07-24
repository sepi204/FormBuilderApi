using Microsoft.Extensions.Logging;
using YekAbr.Domain.Entities;
using YekAbr.Domain.Enums;
using YekAbr.Domain.Interfaces;
using YekAbr.Domain.Models;
using YekAbr.Infrastructure.Services.Cloud;
using YekAbr.Services.Common.Responses;
using YekAbr.Services.DTOs.Sync;
using YekAbr.Services.Interfaces.Cloud;
using YekAbr.Services.Interfaces.Sync;

namespace YekAbr.Infrastructure.Services.Sync;

public sealed class CloudMetadataSyncService : ICloudMetadataSyncService
{
    private readonly IConnectedCloudAccountRepository _accountRepository;
    private readonly IUploadedFileMetadataRepository _metadataRepository;
    private readonly ICloudAccountCredentialService _credentialService;
    private readonly ICloudProviderClientFactory _providerFactory;
    private readonly ILogger<CloudMetadataSyncService> _logger;

    public CloudMetadataSyncService(
        IConnectedCloudAccountRepository accountRepository,
        IUploadedFileMetadataRepository metadataRepository,
        ICloudAccountCredentialService credentialService,
        ICloudProviderClientFactory providerFactory,
        ILogger<CloudMetadataSyncService> logger)
    {
        _accountRepository = accountRepository;
        _metadataRepository = metadataRepository;
        _credentialService = credentialService;
        _providerFactory = providerFactory;
        _logger = logger;
    }

    public async Task<Result<ProviderMetadataSyncResultDto>> SyncAllConnectedProvidersAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Result<ProviderMetadataSyncResultDto>.Failed("کاربر احراز هویت نشده است.");
        }

        var accounts = (await _accountRepository.GetByUserIdAsync(userId, cancellationToken))
            .Where(x => x.IsActive)
            .ToList();

        if (accounts.Count == 0)
        {
            return Result<ProviderMetadataSyncResultDto>.Failed("هیچ حساب ابری فعالی برای همگام‌سازی یافت نشد.");
        }

        return await SyncAccountsAsync(userId, accounts, cancellationToken);
    }

    public async Task<Result<ProviderMetadataSyncResultDto>> SyncProviderAsync(
        string userId,
        CloudProviderType providerType,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Result<ProviderMetadataSyncResultDto>.Failed("کاربر احراز هویت نشده است.");
        }

        if (!_providerFactory.IsSupported(providerType))
        {
            return Result<ProviderMetadataSyncResultDto>.Failed("این ارائه‌دهنده پشتیبانی نمی‌شود.");
        }

        var accounts = (await _accountRepository.GetByUserIdAsync(userId, cancellationToken))
            .Where(x => x.IsActive && x.Provider == providerType)
            .ToList();

        if (accounts.Count == 0)
        {
            return Result<ProviderMetadataSyncResultDto>.Failed("حساب فعالی برای این ارائه‌دهنده یافت نشد.");
        }

        return await SyncAccountsAsync(userId, accounts, cancellationToken);
    }

    private async Task<Result<ProviderMetadataSyncResultDto>> SyncAccountsAsync(
        string userId,
        IReadOnlyList<ConnectedCloudAccount> accounts,
        CancellationToken cancellationToken)
    {
        var result = new ProviderMetadataSyncResultDto();
        var failedMessages = new List<string>();
        var syncedProviders = new HashSet<CloudProviderType>();

        foreach (var account in accounts)
        {
            result.ProvidersChecked++;
            try
            {
                if (string.IsNullOrWhiteSpace(account.AccessToken))
                {
                    throw new InvalidOperationException("اعتبارنامه اتصال موجود نیست. لطفاً دوباره متصل شوید.");
                }

                var accountResult = await SyncSingleAccountAsync(userId, account, cancellationToken);
                result.FilesDiscovered += accountResult.Discovered;
                result.InsertedCount += accountResult.Inserted;
                result.UpdatedCount += accountResult.Updated;
                result.SkippedCount += accountResult.Skipped;
                syncedProviders.Add(account.Provider);

                account.LastSyncedAtUtc = DateTime.UtcNow;
                account.UpdatedAtUtc = DateTime.UtcNow;
                _accountRepository.Update(account);
                await _accountRepository.SaveChangesAsync(cancellationToken);
            }
            catch (Exception exception)
            {
                result.ProvidersFailed++;
                var providerName = _providerFactory.IsSupported(account.Provider)
                    ? _providerFactory.GetProvider(account.Provider).ProviderName
                    : account.Provider.ToString();
                var detail = exception is InvalidOperationException
                    ? exception.Message
                    : "خطای غیرمنتظره رخ داد.";
                var message = $"همگام‌سازی {providerName} ({account.DisplayName}) ناموفق بود: {detail}";
                failedMessages.Add(message);
                _logger.LogWarning(exception, "Metadata sync failed for account {AccountId}.", account.Id);
            }
        }

        result.FailedProviderMessages = failedMessages;
        result.SyncedProviders = syncedProviders.ToList();

        if (result.ProvidersChecked > 0 && result.ProvidersFailed == result.ProvidersChecked)
        {
            return Result<ProviderMetadataSyncResultDto>.Failed(
                "همگام‌سازی متادیتا برای همه ارائه‌دهنده‌ها ناموفق بود.",
                new Dictionary<string, string[]>
                {
                    ["providers"] = failedMessages.ToArray()
                });
        }

        var messageText = result.ProvidersFailed > 0
            ? "همگام‌سازی متادیتا با موفقیت نسبی انجام شد."
            : "همگام‌سازی متادیتا با موفقیت انجام شد.";

        return Result<ProviderMetadataSyncResultDto>.Succeeded(result, messageText);
    }

    private async Task<(int Discovered, int Inserted, int Updated, int Skipped)> SyncSingleAccountAsync(
        string userId,
        ConnectedCloudAccount account,
        CancellationToken cancellationToken)
    {
        if (!_providerFactory.IsSupported(account.Provider))
        {
            throw new InvalidOperationException("عملیات فایل برای این ارائه‌دهنده پشتیبانی نمی‌شود.");
        }

        var provider = _providerFactory.GetFileProvider(account.Provider);
        var accessToken = await _credentialService.GetValidAccessTokenAsync(account, cancellationToken);
        var rootParentId = string.IsNullOrWhiteSpace(account.RootFolderId) ? null : account.RootFolderId;

        var files = await CloudProviderFileEnumerator.ListAllFilesRecursiveAsync(
            provider,
            accessToken,
            rootParentId,
            cancellationToken);

        var inserted = 0;
        var updated = 0;
        var skipped = 0;
        var seenProviderFileIds = new HashSet<string>(StringComparer.Ordinal);

        foreach (var file in files)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(file.Id) || !seenProviderFileIds.Add(file.Id))
            {
                skipped++;
                continue;
            }

            var outcome = await UpsertMetadataAsync(userId, account, file, cancellationToken);
            switch (outcome)
            {
                case UpsertOutcome.Inserted:
                    inserted++;
                    break;
                case UpsertOutcome.Updated:
                    updated++;
                    break;
                default:
                    skipped++;
                    break;
            }
        }

        return (files.Count, inserted, updated, skipped);
    }

    private async Task<UpsertOutcome> UpsertMetadataAsync(
        string userId,
        ConnectedCloudAccount account,
        CloudItem item,
        CancellationToken cancellationToken)
    {
        var existing = await _metadataRepository.GetByProviderFileAsync(
            userId,
            account.Id,
            item.Id,
            includeDeleted: true,
            cancellationToken);

        if (existing is null)
        {
            var created = UploadedFileMetadataMapper.CreateFromCloudItem(userId, account, item);
            await _metadataRepository.AddAsync(created, cancellationToken);
            await _metadataRepository.SaveChangesAsync(cancellationToken);
            return UpsertOutcome.Inserted;
        }

        var changed = UploadedFileMetadataMapper.ApplyCloudItemUpdates(existing, item, account);
        if (!changed)
        {
            // Still touch LastSyncedAtUtc so dashboard freshness is visible.
            existing.LastSyncedAtUtc = DateTime.UtcNow;
            _metadataRepository.Update(existing);
            await _metadataRepository.SaveChangesAsync(cancellationToken);
            return UpsertOutcome.Skipped;
        }

        _metadataRepository.Update(existing);
        await _metadataRepository.SaveChangesAsync(cancellationToken);
        return UpsertOutcome.Updated;
    }

    private enum UpsertOutcome
    {
        Inserted,
        Updated,
        Skipped
    }
}
