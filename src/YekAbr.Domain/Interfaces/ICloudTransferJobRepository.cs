using YekAbr.Domain.Entities;
using YekAbr.Domain.Enums;

namespace YekAbr.Domain.Interfaces;

public interface ICloudTransferJobRepository
{
    Task AddAsync(CloudTransferJob entity, CancellationToken cancellationToken = default);

    Task<CloudTransferJob?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<CloudTransferJob?> GetByIdForUserAsync(Guid id, string userId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CloudTransferJob>> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CloudTransferJob>> ListAsync(
        string userId,
        CloudTransferStatus? status = null,
        Guid? sourceConnectedAccountId = null,
        Guid? destinationConnectedAccountId = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default);

    Task<CloudTransferJob?> TryClaimNextPendingJobAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CloudTransferJob>> GetRunningJobsAsync(CancellationToken cancellationToken = default);

    void Update(CloudTransferJob entity);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
