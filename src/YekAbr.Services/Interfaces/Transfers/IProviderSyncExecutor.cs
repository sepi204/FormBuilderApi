namespace YekAbr.Services.Interfaces.Transfers;

public interface IProviderSyncExecutor
{
    Task ExecuteAsync(Guid operationId, CancellationToken cancellationToken = default);
}
