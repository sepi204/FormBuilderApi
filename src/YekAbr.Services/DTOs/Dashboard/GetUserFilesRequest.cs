namespace YekAbr.Services.DTOs.Dashboard;

public sealed class GetUserFilesRequest
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;

    /// <summary>
    /// Supported values: uploadedAt (default), fileName, size, providerType.
    /// </summary>
    public string SortBy { get; set; } = "uploadedAt";

    /// <summary>
    /// Supported values: desc (default), asc.
    /// </summary>
    public string SortDirection { get; set; } = "desc";
}
