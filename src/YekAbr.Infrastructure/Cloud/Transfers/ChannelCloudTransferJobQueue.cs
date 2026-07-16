using System.Threading.Channels;
using YekAbr.Services.Interfaces.Cloud;

namespace YekAbr.Infrastructure.Cloud.Transfers;

public sealed class ChannelCloudTransferJobQueue : ICloudTransferJobQueue
{
    private readonly Channel<Guid> _channel = Channel.CreateUnbounded<Guid>(new UnboundedChannelOptions
    {
        SingleReader = true,
        SingleWriter = false
    });

    public ValueTask EnqueueAsync(Guid jobId, CancellationToken cancellationToken = default)
    {
        return _channel.Writer.WriteAsync(jobId, cancellationToken);
    }

    public async IAsyncEnumerable<Guid> ReadAllAsync(
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await foreach (var jobId in _channel.Reader.ReadAllAsync(cancellationToken))
        {
            yield return jobId;
        }
    }
}
