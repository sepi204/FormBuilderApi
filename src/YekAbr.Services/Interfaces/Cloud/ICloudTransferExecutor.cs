namespace YekAbr.Services.Interfaces.Cloud;

public interface ICloudTransferExecutor
{
    Task ExecuteAsync(Guid jobId, CancellationToken cancellationToken = default);
}
