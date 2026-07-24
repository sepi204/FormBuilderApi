using Microsoft.EntityFrameworkCore;
using YekAbr.Domain.Entities;
using YekAbr.Domain.Enums;
using YekAbr.Domain.Interfaces;
using YekAbr.Infrastructure.Persistence;

namespace YekAbr.Infrastructure.Repositories;

public sealed class ProviderSyncOperationRepository : IProviderSyncOperationRepository
{
    private readonly ApplicationDbContext _dbContext;

    public ProviderSyncOperationRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(ProviderSyncOperation entity, CancellationToken cancellationToken = default)
    {
        await _dbContext.ProviderSyncOperations.AddAsync(entity, cancellationToken);
    }

    public Task<ProviderSyncOperation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _dbContext.ProviderSyncOperations
            .Include(x => x.SourceConnectedCloudAccount)
            .Include(x => x.DestinationConnectedCloudAccount)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public Task<ProviderSyncOperation?> GetByIdForUserAsync(
        Guid id,
        string userId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.ProviderSyncOperations
            .Include(x => x.SourceConnectedCloudAccount)
            .Include(x => x.DestinationConnectedCloudAccount)
            .FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId, cancellationToken);
    }

    public async Task<(IReadOnlyList<ProviderSyncOperation> Items, int TotalCount)> GetByUserIdPagedAsync(
        string userId,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _dbContext.ProviderSyncOperations
            .AsNoTracking()
            .Include(x => x.SourceConnectedCloudAccount)
            .Include(x => x.DestinationConnectedCloudAccount)
            .Where(x => x.UserId == userId);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(x => x.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<IReadOnlyList<ProviderSyncOperation>> GetRunningOperationsAsync(
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.ProviderSyncOperations
            .Where(x => x.Status == ProviderSyncOperationStatus.Running)
            .ToListAsync(cancellationToken);
    }

    public async Task<ProviderSyncOperation?> TryClaimNextPendingAsync(CancellationToken cancellationToken = default)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        var operation = await _dbContext.ProviderSyncOperations
            .Where(x => x.Status == ProviderSyncOperationStatus.Pending)
            .OrderBy(x => x.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        if (operation is null)
        {
            await transaction.CommitAsync(cancellationToken);
            return null;
        }

        var now = DateTime.UtcNow;
        operation.Status = ProviderSyncOperationStatus.Running;
        operation.StartedAtUtc ??= now;
        operation.UpdatedAtUtc = now;
        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return await GetByIdAsync(operation.Id, cancellationToken);
    }

    public void Update(ProviderSyncOperation entity)
    {
        _dbContext.ProviderSyncOperations.Update(entity);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}
