using YekAbr.Domain.Entities;

namespace YekAbr.Domain.Interfaces;

public interface IProviderSyncOperationRepository
{
    Task AddAsync(ProviderSyncOperation entity, CancellationToken cancellationToken = default);

    Task<ProviderSyncOperation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<ProviderSyncOperation?> GetByIdForUserAsync(
        Guid id,
        string userId,
        CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<ProviderSyncOperation> Items, int TotalCount)> GetByUserIdPagedAsync(
        string userId,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProviderSyncOperation>> GetRunningOperationsAsync(
        CancellationToken cancellationToken = default);

    Task<ProviderSyncOperation?> TryClaimNextPendingAsync(CancellationToken cancellationToken = default);

    void Update(ProviderSyncOperation entity);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
