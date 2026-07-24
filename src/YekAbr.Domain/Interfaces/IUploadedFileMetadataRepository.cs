using YekAbr.Domain.Entities;

namespace YekAbr.Domain.Interfaces;

public interface IUploadedFileMetadataRepository
{
    Task AddAsync(UploadedFileMetadata entity, CancellationToken cancellationToken = default);

    void Update(UploadedFileMetadata entity);

    /// <summary>
    /// Finds metadata by provider file identity.
    /// When <paramref name="includeDeleted"/> is true, prefers an active row over soft-deleted ones.
    /// </summary>
    Task<UploadedFileMetadata?> GetByProviderFileAsync(
        string userId,
        Guid connectedCloudAccountId,
        string providerFileId,
        bool includeDeleted = false,
        CancellationToken cancellationToken = default);

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
