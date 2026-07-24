using Microsoft.EntityFrameworkCore;
using YekAbr.Domain.Entities;
using YekAbr.Domain.Interfaces;
using YekAbr.Infrastructure.Persistence;

namespace YekAbr.Infrastructure.Repositories;

public sealed class UploadedFileMetadataRepository : IUploadedFileMetadataRepository
{
    private readonly ApplicationDbContext _dbContext;

    public UploadedFileMetadataRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(UploadedFileMetadata entity, CancellationToken cancellationToken = default)
    {
        await _dbContext.UploadedFileMetadata.AddAsync(entity, cancellationToken);
    }

    public async Task<(IReadOnlyList<UploadedFileMetadata> Items, int TotalCount)> GetByUserIdPagedAsync(
        string userId,
        int page,
        int pageSize,
        string sortBy,
        string sortDirection,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _dbContext.UploadedFileMetadata
            .AsNoTracking()
            .Where(x => x.UserId == userId && !x.IsDeleted);

        var totalCount = await query.CountAsync(cancellationToken);

        var descending = !string.Equals(sortDirection, "asc", StringComparison.OrdinalIgnoreCase);
        query = ApplySort(query, sortBy, descending);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task SoftDeleteByProviderFileAsync(
        string userId,
        Guid connectedCloudAccountId,
        string providerFileId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(providerFileId))
        {
            return;
        }

        var now = DateTime.UtcNow;
        var matches = await _dbContext.UploadedFileMetadata
            .Where(x =>
                x.UserId == userId
                && x.ConnectedCloudAccountId == connectedCloudAccountId
                && x.ProviderFileId == providerFileId
                && !x.IsDeleted)
            .ToListAsync(cancellationToken);

        foreach (var item in matches)
        {
            item.IsDeleted = true;
            item.LastModifiedAtUtc = now;
        }
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }

    private static IQueryable<UploadedFileMetadata> ApplySort(
        IQueryable<UploadedFileMetadata> query,
        string sortBy,
        bool descending)
    {
        var key = (sortBy ?? string.Empty).Trim().ToLowerInvariant();

        return key switch
        {
            "filename" => descending
                ? query.OrderByDescending(x => x.FileName).ThenByDescending(x => x.UploadedAtUtc)
                : query.OrderBy(x => x.FileName).ThenByDescending(x => x.UploadedAtUtc),
            "size" => descending
                ? query.OrderByDescending(x => x.Size).ThenByDescending(x => x.UploadedAtUtc)
                : query.OrderBy(x => x.Size).ThenByDescending(x => x.UploadedAtUtc),
            "providertype" => descending
                ? query.OrderByDescending(x => x.ProviderType).ThenByDescending(x => x.UploadedAtUtc)
                : query.OrderBy(x => x.ProviderType).ThenByDescending(x => x.UploadedAtUtc),
            _ => descending
                ? query.OrderByDescending(x => x.UploadedAtUtc)
                : query.OrderBy(x => x.UploadedAtUtc)
        };
    }
}
