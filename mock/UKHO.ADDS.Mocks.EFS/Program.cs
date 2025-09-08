using UKHO.ADDS.Mocks.Configuration;
using UKHO.ADDS.Mocks.Domain.Configuration;
using UKHO.ADDS.Mocks.States;

namespace UKHO.ADDS.Mocks.EFS
{
    internal class Program
    {
        public static async Task Main(string[] args)
        {
            MockServices.AddServices();

            ServiceRegistry.AddDefinition(new ServiceDefinition("fss", "FileShare Service", []));

            ServiceRegistry.AddDefinition(new ServiceDefinition("scs", "Sales Catalogue Service", [
                new StateDefinition("get-invalidproducts", "Gets invalid products"),
                new StateDefinition("get-allinvalidproducts", "Gets all invalid products - no products returned"),
                new StateDefinition("get-cancelledproducts", "Gets cancelled products"),
                new StateDefinition("get-productswithdrawn", "Gets withdrawn products")
            ]));

            ServiceRegistry.AddDefinitionState("sample", new StateDefinition("get-jpeg", "Gets a JPEG file"));

            ServiceRegistry.AddDefinition(new ServiceDefinition("fss6357", "File Share Service (S63/S57)", []));
            ServiceRegistry.AddDefinition(new ServiceDefinition("scs6357", "Sales Catalogue Service (S63/S57)", []));

            await MockServer.RunAsync(args);
        }
    }
}
