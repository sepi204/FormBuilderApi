namespace YekAbr.Services.DTOs.Transfers;

public sealed class PagedProviderSyncOperationsResponse
{
    public IReadOnlyList<ProviderSyncOperationDto> Items { get; set; } = Array.Empty<ProviderSyncOperationDto>();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
}
