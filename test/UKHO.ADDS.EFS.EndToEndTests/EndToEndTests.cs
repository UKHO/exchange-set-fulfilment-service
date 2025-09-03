using System.IO.Compression;
using System.Text;
using System.Text.Json;
using Aspire.Hosting;
using UKHO.ADDS.EFS.Infrastructure.Configuration.Namespaces;
using Xunit.Abstractions;

namespace UKHO.ADDS.EFS.EndToEndTests
{
    public class EndToEndTests : IAsyncLifetime
    {
        private DistributedApplication _app;

        private readonly string _projectDirectory;
        public EndToEndTests()
        {
            _projectDirectory = Directory.GetParent(AppContext.BaseDirectory)!.Parent!.Parent!.Parent!.FullName;
        }


        public async Task InitializeAsync()
        {
            var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.UKHO_ADDS_EFS_LocalHost>();
            appHost.Services.ConfigureHttpClientDefaults(clientBuilder =>
            {
                clientBuilder.AddStandardResilienceHandler();
            });
            _app = await appHost.BuildAsync();

            var resourceNotificationService = _app.Services.GetRequiredService<ResourceNotificationService>();
            await _app.StartAsync();
            await resourceNotificationService.WaitForResourceAsync(ProcessNames.OrchestratorService, KnownResourceStates.Running).WaitAsync(TimeSpan.FromSeconds(30));

        }

        public async Task DisposeAsync()
        {
            if (_app != null)
            {
                await _app.StopAsync();
                await _app.DisposeAsync();
            }

            //Clean up temporary files and directories
            var outDir = Path.Combine(_projectDirectory, "out");

            if (Directory.Exists(outDir))
                Array.ForEach(Directory.GetFiles(outDir, "*.zip"), File.Delete);

        }


        [Fact]
        public async Task S100EndToEnd()
        {
            var httpClient = _app.CreateHttpClient(ProcessNames.OrchestratorService);

            // 1.Prepare a job submission request and confirm that it was submitted successfully.
            var content = new StringContent(
                """
                {
                    "version": 1,
                    "dataStandard": "s100",
                    "products": "",
                    "filter": ""
                }
                """,
                Encoding.UTF8, "application/json");
            var requestId = Guid.NewGuid().ToString();
            content.Headers.Add("x-correlation-id", $"job-0001-{requestId}");


            var jobSubmitResponse = await httpClient.PostAsync("/jobs", content);
            Assert.True(jobSubmitResponse.IsSuccessStatusCode, "Expected success status code but got: " + jobSubmitResponse.StatusCode);
            var responseContent = await jobSubmitResponse.Content.ReadAsStringAsync();
            var responseJson = JsonDocument.Parse(responseContent);
            responseJson.RootElement.TryGetProperty("jobId", out var jobId);
            responseJson.RootElement.TryGetProperty("jobStatus", out var jobStatus);
            responseJson.RootElement.TryGetProperty("buildStatus", out var buildStatus);

            Assert.Equal("submitted", jobStatus.GetString());
            Assert.Equal("scheduled", buildStatus.GetString());

            // 2.Check for notification that the job has been picked up by the builder and completed successfully. 
            string currentJobState;
            string currentBuildState;
            double elapsedMinutes = 0;
            var waitDuration = 2000; // 2 seconds
            var maxTimeToWait = 2; // 2 minutes
            TimeOnly startTime = TimeOnly.FromDateTime(DateTime.Now);
            do
            {
                var jobStateResponse = await httpClient.GetAsync($"/jobs/{jobId}");
                responseContent = await jobStateResponse.Content.ReadAsStringAsync();
                responseJson = JsonDocument.Parse(responseContent);
                responseJson.RootElement.TryGetProperty("jobState", out var jobState);
                responseJson.RootElement.TryGetProperty("buildState", out var buildState);
                currentJobState = jobState.GetString() ?? string.Empty;
                currentBuildState = buildState.GetString() ?? string.Empty;
                await Task.Delay(waitDuration);
                elapsedMinutes = (TimeOnly.FromDateTime(DateTime.Now) - startTime).TotalMinutes;
            } while (currentJobState == "submitted" && elapsedMinutes < maxTimeToWait);

            Assert.Equal("completed", currentJobState);
            Assert.Equal("succeeded", currentBuildState);

            // 3.Check the builder has returned build status and it has been successfully processed by orchestrator.
            var jobCompletedResponse = await httpClient.GetAsync($"/jobs/{jobId}/build");
            Assert.True(jobCompletedResponse.IsSuccessStatusCode, "Expected success status code but got: " + jobCompletedResponse.StatusCode);

            // and that the builder exit code is 'success' although success is not necessary
            // the fact that a response was returned is sufficient to indicate that all components in the process
            // are working together.
            responseContent = await jobCompletedResponse.Content.ReadAsStringAsync();
            responseJson = JsonDocument.Parse(responseContent);
            responseJson.RootElement.TryGetProperty("builderExitCode", out var builderExitCode);
            Assert.Equal("success", builderExitCode.GetString());

            // 4.Download Exchange Set, call to the Admin API for downloading the exchange set
            var exchangeSetDownloadPath = await DownloadExchangeSetAsZipAsync(jobId.ToString());

            var sourceZipPath = Path.Combine(_projectDirectory, "TestData/exchangeSet-25Products.testzip");

            // 5. Compare the folder structure of the source and target zip files
            CompareZipFolderStructure(sourceZipPath, exchangeSetDownloadPath);
        }

        [Fact]
        public async Task TestMultipleRequests()
        {
            var httpClient = _app.CreateHttpClient(ProcessNames.OrchestratorService);

            StringContent content;
            var jobs = new List<string>();
            var completedJobs = new List<string>();
            double elapsedMinutes = 0;
            var numberOfJobs = 8; // Number of jobs to submit

            // 1.Submit multiple job requests and confirm that they were all submitted successfully.
            var requestId = Guid.NewGuid().ToString();
            for (int i = 0; i < numberOfJobs; i++)
            {
                string jobNumber = i.ToString("D4");

                content = new StringContent(
                """
                {
                    "version": 1,
                    "dataStandard": "s100",
                    "products": "",
                    "filter": ""
                }
                """,
                Encoding.UTF8, "application/json");
                content.Headers.Add("x-correlation-id", $"job-{jobNumber}-{requestId}");

                var jobSubmitResponse = await httpClient.PostAsync("/jobs", content);
                Assert.True(jobSubmitResponse.IsSuccessStatusCode, "Expected success status code but got: " + jobSubmitResponse.StatusCode);

                var responseContent = await jobSubmitResponse.Content.ReadAsStringAsync();
                var responseJson = JsonDocument.Parse(responseContent);
                responseJson.RootElement.TryGetProperty("jobId", out var jobId);
                var jobIdValue = jobId.GetString();
                if (!string.IsNullOrEmpty(jobIdValue))
                {
                    jobs.Add(jobIdValue);
                }
            }
            Assert.Equal(numberOfJobs, jobs.Count);


            // 2.Check for notification that the jobs have been picked up by the builder and completed successfully.
            var waitDuration = 2000; // 2 seconds
            var maxTimeToWait = 3; // 3 minutes
            TimeOnly startTime = TimeOnly.FromDateTime(DateTime.Now);
            do
            {
                foreach (var jobId in jobs)
                {
                    if (completedJobs.Contains(jobId)) continue; // Skip if job already completed
                    var jobStateResponse = await httpClient.GetAsync($"/jobs/{jobId}");
                    Assert.True(jobStateResponse.IsSuccessStatusCode, "Expected success status code but got: " + jobStateResponse.StatusCode);

                    var responseContent = await jobStateResponse.Content.ReadAsStringAsync();
                    var responseJson = JsonDocument.Parse(responseContent);
                    responseJson.RootElement.TryGetProperty("jobState", out var jobState);
                    responseJson.RootElement.TryGetProperty("buildState", out var buildState);
                    if (jobState.GetString() == "completed" && buildState.GetString() == "succeeded")
                    {
                        completedJobs.Add(jobId);
                    }
                }
                await Task.Delay(waitDuration);
                elapsedMinutes = (TimeOnly.FromDateTime(DateTime.Now) - startTime).TotalMinutes;
            } while (completedJobs.Count < jobs.Count && elapsedMinutes < maxTimeToWait);

            Assert.Equal(jobs.Count, completedJobs.Count);


            // 3.Check the builder has successfully returned build status for each completed job
            foreach (var jobId in completedJobs)
            {
                var jobCompletedResponse = await httpClient.GetAsync($"/jobs/{jobId}/build");
                Assert.True(jobCompletedResponse.IsSuccessStatusCode, "Expected success status code but got: " + jobCompletedResponse.StatusCode);
                var responseContent = await jobCompletedResponse.Content.ReadAsStringAsync();
                var responseJson = JsonDocument.Parse(responseContent);
                responseJson.RootElement.TryGetProperty("builderExitCode", out var builderExitCode);
                Assert.Equal("success", builderExitCode.GetString());
            }


        }

        public async Task<string> DownloadExchangeSetAsZipAsync(string jobId)
        {
            var httpClientMock = _app.CreateHttpClient(ProcessNames.MockService);
            var mockResponse = await httpClientMock.GetAsync($"/_admin/files/FSS/S100-ExchangeSets/V01X01_{jobId}.zip");
            mockResponse.EnsureSuccessStatusCode();

            var zipResponse = await mockResponse.Content.ReadAsStringAsync();

            await using var zipStream = await mockResponse.Content.ReadAsStreamAsync();

            var destinationFilePath = Path.Combine(_projectDirectory, "out", $"V01X01_{jobId}.zip");

            // Ensure the directory exists
            var destinationDirectory = Path.GetDirectoryName(destinationFilePath);
            if (!Directory.Exists(destinationDirectory))
            {
                Directory.CreateDirectory(destinationDirectory!);
            }

            await using var fileStream = new FileStream(destinationFilePath, FileMode.Create, FileAccess.Write, FileShare.None);
            await zipStream.CopyToAsync(fileStream);
            await fileStream.FlushAsync();
            return destinationFilePath;
        }

        public (HashSet<string> Folders, HashSet<string> Files) GetZipStructure(string zipPath)
        {
            var folders = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var files = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            using var archive = ZipFile.OpenRead(zipPath);
            foreach (var entry in archive.Entries)
            {
                // Normalize path separators
                var entryPath = entry.FullName.Replace('\\', '/').TrimEnd('/');

                if (string.IsNullOrEmpty(entryPath))
                    continue;

                if (entry.FullName.EndsWith("/"))
                {
                    // It's a directory entry
                    folders.Add(entryPath);
                }
                else
                {
                    // It's a file entry
                    files.Add(entryPath);

                    // Add all parent folders
                    var lastSlash = entryPath.LastIndexOf('/');
                    while (lastSlash > 0)
                    {
                        var folder = entryPath.Substring(0, lastSlash);
                        folders.Add(folder);
                        lastSlash = folder.LastIndexOf('/');
                    }
                }
            }
            return (folders, files);
        }
        private void CompareZipFolderStructure(string sourceZipPath, string targetZipPath)
        {
            var (sourceFolders, sourceFiles) = GetZipStructure(sourceZipPath);
            var (targetFolders, targetFiles) = GetZipStructure(targetZipPath);

            // Find non-matching folders
            var foldersOnlyInSource = sourceFolders.Except(targetFolders).ToList();
            var foldersOnlyInTarget = targetFolders.Except(sourceFolders).ToList();

            // Assert: Folder and file structures match, with details
            Assert.True(foldersOnlyInSource.Count == 0 && foldersOnlyInTarget.Count == 0,
                $"Folder structures do not match.\n" +
                (foldersOnlyInSource.Count > 0 ? $"Folders only in source: {string.Join(", ", foldersOnlyInSource)}\n" : "") +
                (foldersOnlyInTarget.Count > 0 ? $"Folders only in target: {string.Join(", ", foldersOnlyInTarget)}\n" : ""));

        }


    }
}
