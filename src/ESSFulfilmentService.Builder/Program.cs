namespace ESSFulfilmentService.Builder;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);

        builder.AddAzureBlobClient("blobConnection");
        builder.AddAzureQueueClient("queueConnection");
        builder.AddAzureTableClient("tableConnection");

        builder.AddServiceDefaults();

        builder.Services.AddHttpClient("iic-comms", client =>
        {
            client.BaseAddress = new Uri("http://_iic-endpoint.iic/xchg-2.4/v2.4/");
        });


        builder.Services.AddHostedService<Worker>();

        var host = builder.Build();
        host.Run();
    }
}
