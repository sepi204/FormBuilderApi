namespace YekAbr.Services.Interfaces.Transfers;

public interface IProviderSyncOperationQueue
{
    ValueTask EnqueueAsync(Guid operationId, CancellationToken cancellationToken = default);

    IAsyncEnumerable<Guid> ReadAllAsync(CancellationToken cancellationToken = default);
}
