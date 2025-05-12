using UKHO.ADDS.Infrastructure.Results;

namespace UKHO.ADDS.EFS.Builder.S100.IIC
{
    internal class ToolClient : IToolClient
    {
        private readonly HttpClient _httpClient;
        private readonly string _workSpaceId;
        private readonly string _authKey;
        private const string ApiVersion = "2.7";

        public ToolClient(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _workSpaceId = configuration["IICTool:WorkSpaceId"];
            _authKey = configuration["IICTool:AuthKey"];
        }

        public Task<Result> Ping() => Task.FromResult(Result.Success());

        public async Task AddExchangeSetAsync(string workspaceRootPath, string exchangeSetId)
        {
            string resourceLocation = Path.Combine(workspaceRootPath, "workspaces", _workSpaceId, exchangeSetId);
            if (!Directory.Exists(resourceLocation))
            {
                var path = BuildApiPath("addExchangeSet", exchangeSetId);

                using var response = await _httpClient.GetAsync(path);
                var content = await response.Content.ReadAsStringAsync();
            }
        }

        public async Task AddContentAsync(string workspaceRootPath, string exchangeSetId)
        {
            string resourceLocation = Path.Combine(workspaceRootPath, "spool/spec-wise");
            var directories = Directory.GetDirectories(resourceLocation);
            foreach (var directory in directories)
            {
                var directoryName = $"spec-wise/{Path.GetFileName(directory)}";
                var path = BuildApiPath("addContent", exchangeSetId, directoryName);

                using var response = await _httpClient.GetAsync(path);
                var content = await response.Content.ReadAsStringAsync();
            }
        }

        public async Task SignExchangeSetAsync(string workspaceRootPath, string exchangeSetId)
        {
            string resourceLocation = Path.Combine(workspaceRootPath, "workspaces", _workSpaceId, exchangeSetId);
            if (Directory.Exists(resourceLocation))
            {
                string path = BuildApiPath("signExchangeSet", exchangeSetId);

                using var response = await _httpClient.GetAsync(path);
                var content = await response.Content.ReadAsStringAsync();
            }
        }

        public async Task ExtractExchangeSetAsync(string workspaceRootPath, string exchangeSetId)
        {
            string resourceLocation = Path.Combine(workspaceRootPath, "workspaces", _workSpaceId, exchangeSetId);
            if (Directory.Exists(resourceLocation))
            {
                string path = BuildApiPath("extractExchangeSet", exchangeSetId);
                using var response = await _httpClient.GetAsync(path);
                response.EnsureSuccessStatusCode();

                var contentBytes = await response.Content.ReadAsByteArrayAsync();
                if (contentBytes != null && contentBytes.Length > 0)
                {
                    var tempFilePath = Path.Combine(AppContext.BaseDirectory, $"{exchangeSetId}.zip");
                    await File.WriteAllBytesAsync(tempFilePath, contentBytes);

                }
            }
        }

        private string BuildApiPath(string action, string exchangeSetId, string? resourceLocation = null)
        {
            var basePath = $"/xchg-{ApiVersion}/v{ApiVersion}/{action}/{_workSpaceId}/{exchangeSetId}";
            var query = $"?authkey={_authKey}";
            if (!string.IsNullOrEmpty(resourceLocation))
                query += $"&resourceLocation={resourceLocation}";
            return basePath + query;
        }
    }
}
