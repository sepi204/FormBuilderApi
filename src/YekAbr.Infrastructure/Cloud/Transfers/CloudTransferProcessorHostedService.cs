using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using YekAbr.Domain.Enums;
using YekAbr.Domain.Interfaces;
using YekAbr.Services.Interfaces.Cloud;

namespace YekAbr.Infrastructure.Cloud.Transfers;

/// <summary>
/// Minimal in-process worker:
/// - recovers stuck Running jobs on startup (marks Failed)
/// - consumes the in-memory queue
/// - polls the database for Pending jobs as a fallback
/// Concurrency is limited to one job at a time.
/// </summary>
public sealed class CloudTransferProcessorHostedService : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(5);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ICloudTransferJobQueue _jobQueue;
    private readonly ILogger<CloudTransferProcessorHostedService> _logger;
    private readonly SemaphoreSlim _executionGate = new(1, 1);

    public CloudTransferProcessorHostedService(
        IServiceScopeFactory scopeFactory,
        ICloudTransferJobQueue jobQueue,
        ILogger<CloudTransferProcessorHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _jobQueue = jobQueue;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await RecoverStuckJobsAsync(stoppingToken);

        var queueTask = ProcessQueueAsync(stoppingToken);
        var pollTask = PollPendingJobsAsync(stoppingToken);
        await Task.WhenAll(queueTask, pollTask);
    }

    private async Task ProcessQueueAsync(CancellationToken stoppingToken)
    {
        await foreach (var jobId in _jobQueue.ReadAllAsync(stoppingToken))
        {
            await ProcessJobSafelyAsync(jobId, stoppingToken);
        }
    }

    private async Task PollPendingJobsAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var repository = scope.ServiceProvider.GetRequiredService<ICloudTransferJobRepository>();
                var claimed = await repository.TryClaimNextPendingJobAsync(stoppingToken);
                if (claimed is not null)
                {
                    await ProcessJobSafelyAsync(claimed.Id, stoppingToken, alreadyClaimed: true);
                    continue;
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Transfer job poller failed.");
            }

            try
            {
                await Task.Delay(PollInterval, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }

    private async Task ProcessJobSafelyAsync(Guid jobId, CancellationToken stoppingToken, bool alreadyClaimed = false)
    {
        await _executionGate.WaitAsync(stoppingToken);
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var repository = scope.ServiceProvider.GetRequiredService<ICloudTransferJobRepository>();
            var executor = scope.ServiceProvider.GetRequiredService<ICloudTransferExecutor>();

            if (!alreadyClaimed)
            {
                var job = await repository.GetByIdAsync(jobId, stoppingToken);
                if (job is null)
                {
                    return;
                }

                // Queue notifications are best-effort. If the poller already claimed/finished
                // this job, skip to avoid duplicate execution.
                if (job.Status != CloudTransferStatus.Pending)
                {
                    return;
                }
            }

            _logger.LogInformation("Executing transfer job {JobId}.", jobId);
            await executor.ExecuteAsync(jobId, stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Host is shutting down.
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Unhandled failure while processing transfer job {JobId}.", jobId);
        }
        finally
        {
            _executionGate.Release();
        }
    }

    private async Task RecoverStuckJobsAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var repository = scope.ServiceProvider.GetRequiredService<ICloudTransferJobRepository>();
            var runningJobs = await repository.GetRunningJobsAsync(cancellationToken);

            foreach (var job in runningJobs)
            {
                job.Status = CloudTransferStatus.Failed;
                job.FailureReason = "اجرای جاب به دلیل راه‌اندازی مجدد برنامه متوقف شد.";
                job.CompletedAtUtc = DateTime.UtcNow;
                job.UpdatedAtUtc = DateTime.UtcNow;
                repository.Update(job);
            }

            if (runningJobs.Count > 0)
            {
                await repository.SaveChangesAsync(cancellationToken);
                _logger.LogWarning("Marked {Count} stuck running transfer jobs as Failed after restart.", runningJobs.Count);
            }
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Failed to recover stuck transfer jobs on startup.");
        }
    }
}
