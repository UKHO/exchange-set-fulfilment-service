using UKHO.ADDS.Configuration.Schema;

namespace ConfigurationUploader
{
    internal class Program
    {
        static async Task<int> Main(string[] args)
        {
            // TODO Use command line stuff to implement proper parameter passing

            var environmentName = args[0];
            var environment = AddsEnvironment.Parse(environmentName);

            var configFilePath = args[1]; // The config json file
            var configJson = await File.ReadAllTextAsync(configFilePath);

            // Read table storage connection string
            var configurationWriter = new ConfigurationWriter(null); // pass the table service client
            await configurationWriter.WriteConfigurationAsync(environment, configJson);

            return 0;
        }
    }
}
