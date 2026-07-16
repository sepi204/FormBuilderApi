namespace YekAbr.Services.DTOs.Cloud;

public sealed class CreateCloudTransferJobRequest
{
    public Guid SourceConnectedAccountId { get; set; }
    public Guid DestinationConnectedAccountId { get; set; }
    public string SourceItemId { get; set; } = string.Empty;
    public string? DestinationParentFolderId { get; set; }
}
