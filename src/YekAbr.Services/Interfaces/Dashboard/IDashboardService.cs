using YekAbr.Services.Common.Responses;
using YekAbr.Services.DTOs.Dashboard;

namespace YekAbr.Services.Interfaces.Dashboard;

public interface IDashboardService
{
    Task<Result<PagedDashboardFilesResponse>> GetUserFilesAsync(
        string userId,
        GetUserFilesRequest request,
        CancellationToken cancellationToken = default);
}
