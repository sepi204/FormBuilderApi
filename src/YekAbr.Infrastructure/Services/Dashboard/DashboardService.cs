using FluentValidation;
using Microsoft.Extensions.Logging;
using YekAbr.Domain.Interfaces;
using YekAbr.Services.Common.Responses;
using YekAbr.Services.DTOs.Dashboard;
using YekAbr.Services.Interfaces.Cloud;
using YekAbr.Services.Interfaces.Dashboard;
using YekAbr.Services.Interfaces.Profile;
using YekAbr.Services.Services.Auth;

namespace YekAbr.Infrastructure.Services.Dashboard;

public sealed class DashboardService : IDashboardService
{
    private readonly IUploadedFileMetadataRepository _fileMetadataRepository;
    private readonly ICloudProviderClientFactory _providerFactory;
    private readonly IPublicUrlBuilder _publicUrlBuilder;
    private readonly IValidator<GetUserFilesRequest> _validator;
    private readonly ILogger<DashboardService> _logger;

    public DashboardService(
        IUploadedFileMetadataRepository fileMetadataRepository,
        ICloudProviderClientFactory providerFactory,
        IPublicUrlBuilder publicUrlBuilder,
        IValidator<GetUserFilesRequest> validator,
        ILogger<DashboardService> logger)
    {
        _fileMetadataRepository = fileMetadataRepository;
        _providerFactory = providerFactory;
        _publicUrlBuilder = publicUrlBuilder;
        _validator = validator;
        _logger = logger;
    }

    public async Task<Result<PagedDashboardFilesResponse>> GetUserFilesAsync(
        string userId,
        GetUserFilesRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Result<PagedDashboardFilesResponse>.Failed("کاربر احراز هویت نشده است.");
        }

        request ??= new GetUserFilesRequest();
        if (string.IsNullOrWhiteSpace(request.SortBy))
        {
            request.SortBy = "uploadedAt";
        }

        if (string.IsNullOrWhiteSpace(request.SortDirection))
        {
            request.SortDirection = "desc";
        }

        var validationErrors = await ValidationHelper.ValidateAsync(_validator, request, cancellationToken);
        if (validationErrors is not null)
        {
            return Result<PagedDashboardFilesResponse>.Failed("اعتبارسنجی ناموفق بود.", validationErrors);
        }

        try
        {
            var (items, totalCount) = await _fileMetadataRepository.GetByUserIdPagedAsync(
                userId,
                request.Page,
                request.PageSize,
                request.SortBy,
                request.SortDirection,
                cancellationToken);

            var mapped = items.Select(MapItem).ToList();

            return Result<PagedDashboardFilesResponse>.Succeeded(
                new PagedDashboardFilesResponse
                {
                    Items = mapped,
                    Page = Math.Max(1, request.Page),
                    PageSize = Math.Clamp(request.PageSize, 1, 100),
                    TotalCount = totalCount
                },
                "لیست فایل‌های آپلودشده با موفقیت دریافت شد.");
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Failed to list dashboard files for user {UserId}.", userId);
            return Result<PagedDashboardFilesResponse>.Failed("دریافت لیست فایل‌های داشبورد ناموفق بود.");
        }
    }

    private DashboardFileItemResponse MapItem(Domain.Entities.UploadedFileMetadata entity)
    {
        var providerName = _providerFactory.IsSupported(entity.ProviderType)
            ? _providerFactory.GetProvider(entity.ProviderType).ProviderName
            : entity.ProviderType.ToString();

        return new DashboardFileItemResponse
        {
            Id = entity.Id,
            FileName = entity.FileName,
            OriginalFileName = entity.OriginalFileName,
            Extension = entity.Extension,
            ContentType = entity.ContentType,
            Size = entity.Size,
            ProviderType = entity.ProviderType,
            ProviderTypeName = providerName,
            ConnectedCloudAccountId = entity.ConnectedCloudAccountId,
            ProviderFileId = entity.ProviderFileId,
            ProviderPath = entity.ProviderPath,
            DownloadUrl = _publicUrlBuilder.ToAbsoluteUrl(entity.DownloadUrl),
            ThumbnailUrl = _publicUrlBuilder.ToAbsoluteUrl(entity.ThumbnailUrl),
            UploadedAt = entity.UploadedAtUtc,
            LastModifiedAt = entity.LastModifiedAtUtc
        };
    }
}
