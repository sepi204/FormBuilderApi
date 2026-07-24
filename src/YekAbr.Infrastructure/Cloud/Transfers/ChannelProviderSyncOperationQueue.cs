using System.Threading.Channels;
using YekAbr.Services.Interfaces.Transfers;

namespace YekAbr.Infrastructure.Cloud.Transfers;

public sealed class ChannelProviderSyncOperationQueue : IProviderSyncOperationQueue
{
    private readonly Channel<Guid> _channel = Channel.CreateUnbounded<Guid>(new UnboundedChannelOptions
    {
        SingleReader = true,
        SingleWriter = false
    });

    public ValueTask EnqueueAsync(Guid operationId, CancellationToken cancellationToken = default)
    {
        return _channel.Writer.WriteAsync(operationId, cancellationToken);
    }

    public IAsyncEnumerable<Guid> ReadAllAsync(CancellationToken cancellationToken = default)
    {
        return _channel.Reader.ReadAllAsync(cancellationToken);
    }
}
