using FluentValidation;
using Microsoft.Extensions.Logging;
using YekAbr.Domain.Entities;
using YekAbr.Domain.Enums;
using YekAbr.Domain.Interfaces;
using YekAbr.Services.Common.Responses;
using YekAbr.Services.DTOs.Transfers;
using YekAbr.Services.Interfaces.Cloud;
using YekAbr.Services.Interfaces.Transfers;
using YekAbr.Services.Services.Auth;

namespace YekAbr.Infrastructure.Services.Transfers;

public sealed class ProviderSyncService : IProviderSyncService
{
    private readonly IConnectedCloudAccountRepository _accountRepository;
    private readonly IProviderSyncOperationRepository _operationRepository;
    private readonly IProviderSyncOperationQueue _operationQueue;
    private readonly ICloudAccountCredentialService _credentialService;
    private readonly ICloudProviderClientFactory _providerFactory;
    private readonly IValidator<StartProviderSyncRequest> _validator;
    private readonly ILogger<ProviderSyncService> _logger;

    public ProviderSyncService(
        IConnectedCloudAccountRepository accountRepository,
        IProviderSyncOperationRepository operationRepository,
        IProviderSyncOperationQueue operationQueue,
        ICloudAccountCredentialService credentialService,
        ICloudProviderClientFactory providerFactory,
        IValidator<StartProviderSyncRequest> validator,
        ILogger<ProviderSyncService> logger)
    {
        _accountRepository = accountRepository;
        _operationRepository = operationRepository;
        _operationQueue = operationQueue;
        _credentialService = credentialService;
        _providerFactory = providerFactory;
        _validator = validator;
        _logger = logger;
    }

    public async Task<Result<ProviderSyncOperationDto>> StartAsync(
        string userId,
        StartProviderSyncRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Result<ProviderSyncOperationDto>.Failed("کاربر احراز هویت نشده است.");
        }

        var validationErrors = await ValidationHelper.ValidateAsync(_validator, request, cancellationToken);
        if (validationErrors is not null)
        {
            return Result<ProviderSyncOperationDto>.Failed("اعتبارسنجی ناموفق بود.", validationErrors);
        }

        if (request.SourceProvider == request.DestinationProvider)
        {
            return Result<ProviderSyncOperationDto>.Failed(
                "ارائه‌دهنده مبدأ و مقصد نباید یکسان باشند.",
                new Dictionary<string, string[]>
                {
                    [nameof(request.DestinationProvider)] = ["ارائه‌دهنده مبدأ و مقصد نباید یکسان باشند."]
                });
        }

        if (!_providerFactory.IsSupported(request.SourceProvider)
            || !_providerFactory.IsSupported(request.DestinationProvider))
        {
            return Result<ProviderSyncOperationDto>.Failed("یکی از ارائه‌دهنده‌های انتخاب‌شده پشتیبانی نمی‌شود.");
        }

        try
        {
            var sourceAccountResult = await ResolveUsableAccountAsync(
                userId,
                request.SourceProvider,
                isSource: true,
                cancellationToken);
            if (!sourceAccountResult.Success)
            {
                return Result<ProviderSyncOperationDto>.Failed(sourceAccountResult.ErrorMessage!);
            }

            var destinationAccountResult = await ResolveUsableAccountAsync(
                userId,
                request.DestinationProvider,
                isSource: false,
                cancellationToken);
            if (!destinationAccountResult.Success)
            {
                return Result<ProviderSyncOperationDto>.Failed(destinationAccountResult.ErrorMessage!);
            }

            var sourceAccount = sourceAccountResult.Account!;
            var destinationAccount = destinationAccountResult.Account!;

            if (sourceAccount.Id == destinationAccount.Id)
            {
                return Result<ProviderSyncOperationDto>.Failed("حساب مبدأ و مقصد نباید یکسان باشند.");
            }

            var now = DateTime.UtcNow;
            var operation = new ProviderSyncOperation
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                SourceProviderType = request.SourceProvider,
                DestinationProviderType = request.DestinationProvider,
                SourceConnectedCloudAccountId = sourceAccount.Id,
                DestinationConnectedCloudAccountId = destinationAccount.Id,
                Status = ProviderSyncOperationStatus.Pending,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            };

            await _operationRepository.AddAsync(operation, cancellationToken);
            await _operationRepository.SaveChangesAsync(cancellationToken);
            await _operationQueue.EnqueueAsync(operation.Id, cancellationToken);

            var created = await _operationRepository.GetByIdAsync(operation.Id, cancellationToken) ?? operation;
            return Result<ProviderSyncOperationDto>.Succeeded(
                Map(created),
                "عملیات همگام‌سازی بین ارائه‌دهنده‌ها با موفقیت ایجاد شد.");
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Failed to start provider sync for user {UserId}.", userId);
            return Result<ProviderSyncOperationDto>.Failed("ایجاد عملیات همگام‌سازی ناموفق بود.");
        }
    }

    public async Task<Result<ProviderSyncOperationDto>> GetByIdAsync(
        string userId,
        Guid operationId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Result<ProviderSyncOperationDto>.Failed("کاربر احراز هویت نشده است.");
        }

        var operation = await _operationRepository.GetByIdForUserAsync(operationId, userId, cancellationToken);
        if (operation is null)
        {
            return Result<ProviderSyncOperationDto>.Failed("عملیات همگام‌سازی یافت نشد.");
        }

        return Result<ProviderSyncOperationDto>.Succeeded(
            Map(operation),
            "جزئیات عملیات همگام‌سازی با موفقیت دریافت شد.");
    }

    public async Task<Result<PagedProviderSyncOperationsResponse>> ListAsync(
        string userId,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Result<PagedProviderSyncOperationsResponse>.Failed("کاربر احراز هویت نشده است.");
        }

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var (items, totalCount) = await _operationRepository.GetByUserIdPagedAsync(
            userId,
            page,
            pageSize,
            cancellationToken);

        return Result<PagedProviderSyncOperationsResponse>.Succeeded(
            new PagedProviderSyncOperationsResponse
            {
                Items = items.Select(Map).ToList(),
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount
            },
            "لیست عملیات‌های همگام‌سازی با موفقیت دریافت شد.");
    }

    private async Task<(bool Success, ConnectedCloudAccount? Account, string? ErrorMessage)> ResolveUsableAccountAsync(
        string userId,
        CloudProviderType providerType,
        bool isSource,
        CancellationToken cancellationToken)
    {
        var roleLabel = isSource ? "مبدأ" : "مقصد";

        var account = (await _accountRepository.GetByUserIdAsync(userId, cancellationToken))
            .Where(x => x.IsActive && x.Provider == providerType)
            .OrderByDescending(x => x.CreatedAtUtc)
            .FirstOrDefault();

        if (account is null)
        {
            return (false, null, $"حساب فعالی برای ارائه‌دهنده {roleLabel} یافت نشد.");
        }

        if (string.IsNullOrWhiteSpace(account.AccessToken))
        {
            return (false, null, $"اعتبارنامه اتصال ارائه‌دهنده {roleLabel} موجود نیست. لطفاً دوباره متصل شوید.");
        }

        try
        {
            // Validates ownership/active state and refreshes token when needed.
            await _credentialService.GetValidAccessTokenAsync(account, cancellationToken);
        }
        catch (InvalidOperationException exception)
        {
            _logger.LogWarning(
                exception,
                "Credential validation failed for {Role} provider {Provider} account {AccountId}.",
                roleLabel,
                providerType,
                account.Id);

            return (false, null,
                $"اعتبارنامه ارائه‌دهنده {roleLabel} نامعتبر یا منقضی است. لطفاً دوباره متصل شوید.");
        }

        return (true, account, null);
    }

    private ProviderSyncOperationDto Map(ProviderSyncOperation operation)
    {
        return new ProviderSyncOperationDto
        {
            Id = operation.Id,
            Status = operation.Status,
            StatusDisplayName = MapStatusDisplayName(operation.Status),
            SourceProviderType = operation.SourceProviderType,
            DestinationProviderType = operation.DestinationProviderType,
            SourceProviderName = ResolveProviderName(operation.SourceProviderType),
            DestinationProviderName = ResolveProviderName(operation.DestinationProviderType),
            SourceConnectedCloudAccountId = operation.SourceConnectedCloudAccountId,
            DestinationConnectedCloudAccountId = operation.DestinationConnectedCloudAccountId,
            SourceAccountDisplayName = operation.SourceConnectedCloudAccount?.DisplayName,
            DestinationAccountDisplayName = operation.DestinationConnectedCloudAccount?.DisplayName,
            TotalFiles = operation.TotalFiles,
            SucceededFiles = operation.SucceededFiles,
            FailedFiles = operation.FailedFiles,
            SkippedFiles = operation.SkippedFiles,
            ErrorMessage = operation.ErrorMessage,
            CreatedAtUtc = operation.CreatedAtUtc,
            StartedAtUtc = operation.StartedAtUtc,
            CompletedAtUtc = operation.CompletedAtUtc,
            UpdatedAtUtc = operation.UpdatedAtUtc
        };
    }

    private string ResolveProviderName(CloudProviderType providerType)
    {
        return _providerFactory.IsSupported(providerType)
            ? _providerFactory.GetProvider(providerType).ProviderName
            : providerType.ToString();
    }

    private static string MapStatusDisplayName(ProviderSyncOperationStatus status)
    {
        return status switch
        {
            ProviderSyncOperationStatus.Pending => "در انتظار",
            ProviderSyncOperationStatus.Running => "در حال اجرا",
            ProviderSyncOperationStatus.Completed => "تکمیل‌شده",
            ProviderSyncOperationStatus.PartiallyCompleted => "تکمیل نسبی",
            ProviderSyncOperationStatus.Failed => "ناموفق",
            _ => status.ToString()
        };
    }
}
