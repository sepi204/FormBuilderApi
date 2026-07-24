namespace YekAbr.Services.DTOs.Dashboard;

public sealed class PagedDashboardFilesResponse
{
    public IReadOnlyList<DashboardFileItemResponse> Items { get; set; } = Array.Empty<DashboardFileItemResponse>();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
}
