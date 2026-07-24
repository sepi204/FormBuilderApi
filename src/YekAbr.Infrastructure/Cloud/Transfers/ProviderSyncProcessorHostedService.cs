using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using YekAbr.Domain.Enums;
using YekAbr.Domain.Interfaces;
using YekAbr.Services.Interfaces.Transfers;

namespace YekAbr.Infrastructure.Cloud.Transfers;

/// <summary>
/// Background worker for bulk provider-to-provider sync operations.
/// </summary>
public sealed class ProviderSyncProcessorHostedService : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(5);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IProviderSyncOperationQueue _operationQueue;
    private readonly ILogger<ProviderSyncProcessorHostedService> _logger;
    private readonly SemaphoreSlim _executionGate = new(1, 1);

    public ProviderSyncProcessorHostedService(
        IServiceScopeFactory scopeFactory,
        IProviderSyncOperationQueue operationQueue,
        ILogger<ProviderSyncProcessorHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _operationQueue = operationQueue;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await RecoverStuckOperationsAsync(stoppingToken);

        var queueTask = ProcessQueueAsync(stoppingToken);
        var pollTask = PollPendingOperationsAsync(stoppingToken);
        await Task.WhenAll(queueTask, pollTask);
    }

    private async Task ProcessQueueAsync(CancellationToken stoppingToken)
    {
        await foreach (var operationId in _operationQueue.ReadAllAsync(stoppingToken))
        {
            await ProcessOperationSafelyAsync(operationId, stoppingToken);
        }
    }

    private async Task PollPendingOperationsAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var repository = scope.ServiceProvider.GetRequiredService<IProviderSyncOperationRepository>();
                var claimed = await repository.TryClaimNextPendingAsync(stoppingToken);
                if (claimed is not null)
                {
                    await ProcessOperationSafelyAsync(claimed.Id, stoppingToken, alreadyClaimed: true);
                    continue;
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Provider sync operation poller failed.");
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

    private async Task ProcessOperationSafelyAsync(
        Guid operationId,
        CancellationToken stoppingToken,
        bool alreadyClaimed = false)
    {
        await _executionGate.WaitAsync(stoppingToken);
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var repository = scope.ServiceProvider.GetRequiredService<IProviderSyncOperationRepository>();
            var executor = scope.ServiceProvider.GetRequiredService<IProviderSyncExecutor>();

            if (!alreadyClaimed)
            {
                var operation = await repository.GetByIdAsync(operationId, stoppingToken);
                if (operation is null || operation.Status != ProviderSyncOperationStatus.Pending)
                {
                    return;
                }
            }

            _logger.LogInformation("Executing provider sync operation {OperationId}.", operationId);
            await executor.ExecuteAsync(operationId, stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Host is shutting down.
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Unhandled failure while processing provider sync operation {OperationId}.", operationId);
        }
        finally
        {
            _executionGate.Release();
        }
    }

    private async Task RecoverStuckOperationsAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var repository = scope.ServiceProvider.GetRequiredService<IProviderSyncOperationRepository>();
            var running = await repository.GetRunningOperationsAsync(cancellationToken);

            foreach (var operation in running)
            {
                operation.Status = ProviderSyncOperationStatus.Failed;
                operation.ErrorMessage = "اجرای عملیات به دلیل راه‌اندازی مجدد برنامه متوقف شد.";
                operation.CompletedAtUtc = DateTime.UtcNow;
                operation.UpdatedAtUtc = DateTime.UtcNow;
                repository.Update(operation);
            }

            if (running.Count > 0)
            {
                await repository.SaveChangesAsync(cancellationToken);
                _logger.LogWarning(
                    "Marked {Count} stuck running provider sync operations as Failed after restart.",
                    running.Count);
            }
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Failed to recover stuck provider sync operations on startup.");
        }
    }
}
