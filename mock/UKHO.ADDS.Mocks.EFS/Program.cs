using UKHO.ADDS.Mocks.Configuration;
using UKHO.ADDS.Mocks.States;

namespace UKHO.ADDS.Mocks.EFS
{
    internal class Program
    {
        public static async Task Main(string[] args)
        {
            MockServices.AddServices();
            ServiceRegistry.AddDefinitionState("sample", new StateDefinition("get-jpeg", "Gets a JPEG file"));

            await MockServer.RunAsync(args);
        }
    }
}
