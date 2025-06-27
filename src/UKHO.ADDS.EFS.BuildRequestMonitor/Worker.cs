using Azure.Storage.Queues;
using Docker.DotNet.Models;
using UKHO.ADDS.EFS.BuildRequestMonitor.Services;
using UKHO.ADDS.EFS.Configuration.Namespaces;

namespace UKHO.ADDS.EFS.BuildRequestMonitor;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly QueueServiceClient _queueClient;
    private readonly ProcessRequestService _requestService;


    public Worker(ILogger<Worker> logger, QueueServiceClient qClient,ProcessRequestService process)
    {
        _logger = logger;
        _queueClient = qClient ?? throw new ArgumentNullException(nameof(qClient), "QueueServiceClient cannot be null");
        _requestService = process ?? throw new ArgumentNullException(nameof(process), "ProcessRequestService cannot be null");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var requestQueue = _queueClient.GetQueueClient(StorageConfiguration.S100RequestsQueueName);
        await requestQueue.CreateIfNotExistsAsync(cancellationToken: stoppingToken);

        var responseQueue = _queueClient.GetQueueClient(StorageConfiguration.S100ResponsesQueueName);
        await responseQueue.CreateIfNotExistsAsync(cancellationToken: stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var messages = await requestQueue.ReceiveMessagesAsync(maxMessages: 10, visibilityTimeout: TimeSpan.FromMinutes(5), cancellationToken: stoppingToken);
                foreach (var message in messages.Value)
                {
                    _logger.LogInformation("Processing build request message: {MessageId}", message.MessageId);

                    // Create container using image here
                    await _requestService.ProcessRequestAsync(message.MessageText, cancellationToken:stoppingToken);
                    // Delete the message after processing
                    await requestQueue.DeleteMessageAsync(message.MessageId, message.PopReceipt, cancellationToken: stoppingToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while processing messages from the queue.");
            }
            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken); // Wait before checking for new messages
        }
    }
}
