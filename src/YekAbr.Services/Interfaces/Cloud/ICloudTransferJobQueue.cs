namespace YekAbr.Services.Interfaces.Cloud;

/// <summary>
/// In-process queue for notifying the transfer worker about newly created jobs.
/// Persistence remains the source of truth; this queue is an optimization.
/// </summary>
public interface ICloudTransferJobQueue
{
    ValueTask EnqueueAsync(Guid jobId, CancellationToken cancellationToken = default);

    IAsyncEnumerable<Guid> ReadAllAsync(CancellationToken cancellationToken);
}
