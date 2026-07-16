using Microsoft.EntityFrameworkCore;
using YekAbr.Domain.Entities;
using YekAbr.Domain.Enums;
using YekAbr.Domain.Interfaces;
using YekAbr.Infrastructure.Persistence;

namespace YekAbr.Infrastructure.Repositories;

public sealed class CloudTransferJobRepository : ICloudTransferJobRepository
{
    private readonly ApplicationDbContext _dbContext;

    public CloudTransferJobRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(CloudTransferJob entity, CancellationToken cancellationToken = default)
    {
        await _dbContext.CloudTransferJobs.AddAsync(entity, cancellationToken);
    }

    public Task<CloudTransferJob?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _dbContext.CloudTransferJobs
            .Include(x => x.SourceConnectedCloudAccount)
            .Include(x => x.DestinationConnectedCloudAccount)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public Task<CloudTransferJob?> GetByIdForUserAsync(
        Guid id,
        string userId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.CloudTransferJobs
            .Include(x => x.SourceConnectedCloudAccount)
            .Include(x => x.DestinationConnectedCloudAccount)
            .FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId, cancellationToken);
    }

    public async Task<IReadOnlyList<CloudTransferJob>> GetByUserIdAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        return await ListAsync(userId, cancellationToken: cancellationToken);
    }

    public async Task<IReadOnlyList<CloudTransferJob>> ListAsync(
        string userId,
        CloudTransferStatus? status = null,
        Guid? sourceConnectedAccountId = null,
        Guid? destinationConnectedAccountId = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _dbContext.CloudTransferJobs
            .AsNoTracking()
            .Include(x => x.SourceConnectedCloudAccount)
            .Include(x => x.DestinationConnectedCloudAccount)
            .Where(x => x.UserId == userId);

        if (status.HasValue)
        {
            query = query.Where(x => x.Status == status.Value);
        }

        if (sourceConnectedAccountId.HasValue)
        {
            query = query.Where(x => x.SourceConnectedCloudAccountId == sourceConnectedAccountId.Value);
        }

        if (destinationConnectedAccountId.HasValue)
        {
            query = query.Where(x => x.DestinationConnectedCloudAccountId == destinationConnectedAccountId.Value);
        }

        return await query
            .OrderByDescending(x => x.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<CloudTransferJob?> TryClaimNextPendingJobAsync(CancellationToken cancellationToken = default)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        var job = await _dbContext.CloudTransferJobs
            .Where(x => x.Status == CloudTransferStatus.Pending)
            .OrderBy(x => x.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        if (job is null)
        {
            await transaction.CommitAsync(cancellationToken);
            return null;
        }

        var now = DateTime.UtcNow;
        job.Status = CloudTransferStatus.Running;
        job.StartedAtUtc ??= now;
        job.UpdatedAtUtc = now;
        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return await GetByIdAsync(job.Id, cancellationToken);
    }

    public async Task<IReadOnlyList<CloudTransferJob>> GetRunningJobsAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.CloudTransferJobs
            .Where(x => x.Status == CloudTransferStatus.Running)
            .ToListAsync(cancellationToken);
    }

    public void Update(CloudTransferJob entity)
    {
        _dbContext.CloudTransferJobs.Update(entity);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}
