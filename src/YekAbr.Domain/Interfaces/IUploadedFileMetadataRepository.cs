using YekAbr.Domain.Entities;

namespace YekAbr.Domain.Interfaces;

public interface IUploadedFileMetadataRepository
{
    Task AddAsync(UploadedFileMetadata entity, CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<UploadedFileMetadata> Items, int TotalCount)> GetByUserIdPagedAsync(
        string userId,
        int page,
        int pageSize,
        string sortBy,
        string sortDirection,
        CancellationToken cancellationToken = default);

    Task SoftDeleteByProviderFileAsync(
        string userId,
        Guid connectedCloudAccountId,
        string providerFileId,
        CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
