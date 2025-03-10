using Azure.Storage.Blobs;
using Azure.Storage.Queues;
using Azure.Data.Tables;

namespace ESSFulfilmentService.Builder;

public class Worker(
        IHttpClientFactory httpFactory,
        QueueServiceClient qClient,
        TableServiceClient tClient,
        ILogger<Worker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var iicApiClient = httpFactory.CreateClient("iic-comms");
        var queueClient = qClient.GetQueueClient("myqueue");
        var tableClient = tClient.GetTableClient("mytable");
        await queueClient.CreateIfNotExistsAsync(cancellationToken: stoppingToken);


        while (!stoppingToken.IsCancellationRequested)
        {
            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
            }
            await Task.Delay(1000, stoppingToken);
        }
    }
}
