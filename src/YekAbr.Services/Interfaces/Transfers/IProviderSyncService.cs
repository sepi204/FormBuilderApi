using YekAbr.Services.Common.Responses;
using YekAbr.Services.DTOs.Transfers;

namespace YekAbr.Services.Interfaces.Transfers;

public interface IProviderSyncService
{
    Task<Result<ProviderSyncOperationDto>> StartAsync(
        string userId,
        StartProviderSyncRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<ProviderSyncOperationDto>> GetByIdAsync(
        string userId,
        Guid operationId,
        CancellationToken cancellationToken = default);

    Task<Result<PagedProviderSyncOperationsResponse>> ListAsync(
        string userId,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default);
}
