using Azure.Storage.Blobs;
using Azure.Storage.Queues;
using Azure.Data.Tables;
using Azure.Storage.Queues.Models;
using System.Net.Http.Json;
using System.Xml.Linq;

namespace ESSFulfilmentService.Builder;

// This record is used for testing the build
public record MyData(int code, string type, string message);

public class Worker(
        IHttpClientFactory httpFactory,
        QueueServiceClient qClient,
        TableServiceClient tClient,
        ILogger<Worker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var iicApiClient = httpFactory.CreateClient("iic-comms");
        var queueClient = qClient.GetQueueClient("notify");
        var tableClient = tClient.GetTableClient("mytable");
        await queueClient.CreateIfNotExistsAsync(cancellationToken: stoppingToken);


        // Test that access to IIC is possible
        // Requests to the two IIC test endpoints enter these into the "notify" queue using azure storage explorer 
        // dev?arg=test&authkey=noauth
        // retriever/01/GB100160?authkey=noauth

        while (!stoppingToken.IsCancellationRequested)
        {
            QueueMessage[] messages = await queueClient.ReceiveMessagesAsync(maxMessages: 1, cancellationToken: stoppingToken);

            if (messages.Length > 0)
            {
                foreach (var message in messages)
                {
                    logger.LogInformation("Message from notify queue {Message}:", message.MessageText);
                    await queueClient.DeleteMessageAsync(message.MessageId, message.PopReceipt, cancellationToken: stoppingToken);

                }

                var httpClient = httpFactory.CreateClient("iic-comms");
                var instruction = messages[0].MessageText;
                var data = await httpClient.GetFromJsonAsync<MyData>(instruction);
                logger.LogInformation("Message from IIC is {Message}", data);

            }

            await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);
        }

        // Test end.

    }
}
