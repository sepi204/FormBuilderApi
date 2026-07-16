using YekAbr.Domain.Enums;
using YekAbr.Services.Common.Responses;
using YekAbr.Services.DTOs.Cloud;

namespace YekAbr.Services.Interfaces.Cloud;

public interface ICloudTransferService
{
    Task<Result<CloudTransferJobDto>> CreateAsync(
        string userId,
        CreateCloudTransferJobRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<CloudTransferJobDto>> GetByIdAsync(
        string userId,
        Guid jobId,
        CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<CloudTransferJobDto>>> ListAsync(
        string userId,
        CloudTransferStatus? status = null,
        Guid? sourceConnectedAccountId = null,
        Guid? destinationConnectedAccountId = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default);

    Task<Result<object>> CancelAsync(
        string userId,
        Guid jobId,
        CancellationToken cancellationToken = default);
}
