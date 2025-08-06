using System.Text.Json;
using UKHO.ADDS.Aspire.Configuration.Seeder.Json;
using Xunit;

namespace UKHO.ADDS.Aspire.Configuration.Seeder.Tests
{
    public class ExternakServiceDefinitionParserTests
    {
        private const string EnvVarKey = "services__adds-mocks-efs__http__0";
        private const string EnvVarValue = "http://resolved-host:8080";

        public ExternakServiceDefinitionParserTests()
        {
            Environment.SetEnvironmentVariable(EnvVarKey, EnvVarValue);
        }
        [Fact]
        public async Task InvalidJson_ThrowsJsonException()
        {
            var json = "{ invalid json ";

            var exception = await Record.ExceptionAsync(() => ExternalServiceDefinitionParser.ParseAndResolveAsync(AddsEnvironment.Local, json));

            Assert.NotNull(exception);
            Assert.IsAssignableFrom<JsonException>(exception);
        }

        [Fact]
        public async Task MissingEnvironmentSection_ThrowsInvalidDataException()
        {
            var json = "{\"dev\": {}}";
            var ex = await Assert.ThrowsAsync<InvalidDataException>(() => ExternalServiceDefinitionParser.ParseAndResolveAsync(AddsEnvironment.Local, json));

            Assert.Contains("Missing 'local' section", ex.Message);
        }

        [Fact]
        public async Task ServiceMissingEndpoints_ThrowsInvalidDataException()
        {
            var json = "{\"local\": { \"FileShare\": {\"clientId\": \"mock\"} }}";
            var ex = await Assert.ThrowsAsync<InvalidDataException>(() => ExternalServiceDefinitionParser.ParseAndResolveAsync(AddsEnvironment.Local, json));
            Assert.Contains("must have at least one endpoint", ex.Message);
        }

        [Fact]
        public async Task ServiceWithEmptyEndpoints_ThrowsInvalidDataException()
        {
            var json = "{\"local\": { \"FileShare\": {\"endpoints\": [], \"clientId\": \"mock\"} }}";
            var ex = await Assert.ThrowsAsync<InvalidDataException>(() => ExternalServiceDefinitionParser.ParseAndResolveAsync(AddsEnvironment.Local, json));

            Assert.Contains("must have at least one endpoint", ex.Message);
        }

        [Fact]
        public async Task ServiceMissingClientId_ThrowsInvalidDataException()
        {
            var json = "{\"local\": { \"FileShare\": {\"endpoints\": [{\"url\": \"http://localhost\", \"tag\": \"\"}] } }}";
            var ex = await Assert.ThrowsAsync<InvalidDataException>(() => ExternalServiceDefinitionParser.ParseAndResolveAsync(AddsEnvironment.Local, json));

            Assert.Contains("missing required 'clientId'", ex.Message);
        }

        [Fact]
        public async Task EndpointMissingUrl_ThrowsInvalidDataException()
        {
            var json = "{\"local\": { \"FileShare\": {\"endpoints\": [{\"tag\": \"\"}], \"clientId\": \"mock\"} }}";
            await Assert.ThrowsAsync<KeyNotFoundException>(() => ExternalServiceDefinitionParser.ParseAndResolveAsync(AddsEnvironment.Local, json));
        }

        [Fact]
        public async Task EndpointWithInvalidScheme_ThrowsInvalidDataException()
        {
            var json = "{\"local\": { \"FileShare\": {\"endpoints\": [{\"url\": \"ftp://localhost\", \"tag\": \"\"}], \"clientId\": \"mock\"} }}";

            await Assert.ThrowsAsync<InvalidDataException>(() => ExternalServiceDefinitionParser.ParseAndResolveAsync(AddsEnvironment.Local, json));
        }

        [Fact]
        public async Task MissingDefaultEndpointTag_ThrowsInvalidDataException()
        {
            var json = "{\"local\": { \"FileShare\": {\"endpoints\": [ {\"url\": \"http://localhost\", \"tag\": \"legacy\"} ], \"clientId\": \"mock\"} }}";
            var ex = await Assert.ThrowsAsync<InvalidDataException>(() => ExternalServiceDefinitionParser.ParseAndResolveAsync(AddsEnvironment.Local, json));

            Assert.Contains("must have at least one endpoint with tag=\"\"", ex.Message);
        }

        [Fact]
        public async Task LocalEnvironment_MultiplePlaceholders_ThrowsInvalidDataException()
        {
            var json = "{\"local\": { \"FileShare\": {\"endpoints\": [ {\"url\": \"http://{{one}}/{{two}}\", \"tag\": \"\"} ], \"clientId\": \"mock\"} }}";

            await Assert.ThrowsAsync<InvalidDataException>(() => ExternalServiceDefinitionParser.ParseAndResolveAsync(AddsEnvironment.Local, json));
        }

        [Fact]
        public async Task LocalEnvironment_SinglePlaceholder_ResolvesCorrectly()
        {
            var json = "{\"local\": { \"FileShare\": {\"endpoints\": [ {\"url\": \"http://{{adds-mocks-efs}}/fss/\", \"tag\": \"\"} ], \"clientId\": \"mock\"} }}";
            var result = await ExternalServiceDefinitionParser.ParseAndResolveAsync(AddsEnvironment.Local, json);

            Assert.Single(result);

            var endpoint = result[0].Endpoints[0];

            Assert.Equal("http://resolved-host:8080/fss/", endpoint.ResolvedUrl);
        }

        [Fact]
        public async Task NonLocalEnvironment_IgnoresPlaceholders()
        {
            var json = "{\"dev\": { \"FileShare\": {\"endpoints\": [ {\"url\": \"https://adds-mocks-efs.{{cae_domain}}/fss/\", \"tag\": \"\"} ], \"clientId\": \"mock\"} }}";
            var result = await ExternalServiceDefinitionParser.ParseAndResolveAsync(AddsEnvironment.Development, json);
            var endpoint = result[0].Endpoints[0];

            Assert.Equal("https://adds-mocks-efs.{{cae_domain}}/fss/", endpoint.ResolvedUrl);
        }

        [Fact]
        public async Task PlaceholderEnvVarMissing_ThrowsInvalidOperationException()
        {
            Environment.SetEnvironmentVariable("services__missing__http__0", null);
            var json = "{\"local\": { \"MissingService\": {\"endpoints\": [ {\"url\": \"http://{{missing}}/fss/\", \"tag\": \"\"} ], \"clientId\": \"mock\"} }}";
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => ExternalServiceDefinitionParser.ParseAndResolveAsync(AddsEnvironment.Local, json));

            Assert.Contains("Cannot find reference for service", ex.Message);
        }

        [Fact]
        public async Task MultipleEndpoints_ParsesSuccessfully()
        {
            var json = "{\"local\": { \"FileShare\": {\"endpoints\": [ {\"url\": \"http://{{adds-mocks-efs}}/fss/\", \"tag\": \"\"}, {\"url\": \"http://{{adds-mocks-efs}}/fss6357/\", \"tag\": \"legacy\"} ], \"clientId\": \"mock\"} }}";
            var result = await ExternalServiceDefinitionParser.ParseAndResolveAsync(AddsEnvironment.Local, json);

            Assert.Equal(2, result[0].Endpoints.Count);
            Assert.Contains(result[0].Endpoints, e => e.Tag == "");
            Assert.Contains(result[0].Endpoints, e => e.Tag == "legacy");
        }
    }
}
