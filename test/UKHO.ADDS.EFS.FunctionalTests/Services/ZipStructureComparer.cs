using System.IO.Compression;
using System.Net;
using Aspire.Hosting;
using Microsoft.VisualStudio.TestPlatform.Utilities;
using UKHO.ADDS.EFS.Infrastructure.Configuration.Namespaces;
using Xunit.Abstractions;

namespace UKHO.ADDS.EFS.FunctionalTests.Services
{
    public class ZipStructureComparer
    {
        /*
         PSEUDOCODE (Improvements for DownloadExchangeSetAsZipAsync)
         - Add optional CancellationToken parameter (non-breaking default)
         - Define retryable status codes (include 404 + transient/server errors + 429)
         - Track deadline based on maxSeconds
         - Loop:
             attempt++
             try GET with ResponseHeadersRead for quicker streaming
             if success:
                 validate (optional) content-type contains "zip" (warn if not)
                 build destination path, ensure directory exists
                 stream copy (no explicit Flush needed; disposal suffices)
                 return path
             else if non-retryable status -> throw immediately with details
             else log and compute backoff with exponential growth + jitter (cap)
         - Catch HttpRequestException: treat as transient -> retry (log)
         - Respect cancellation and deadline
         - After loop: throw with last status / exception summary
         - Keep original signature shape but add CancellationToken as last optional param
        */

        private static readonly HashSet<HttpStatusCode> RetryableStatusCodes =
        [
            HttpStatusCode.NotFound,              // eventual consistency
            HttpStatusCode.RequestTimeout,
            HttpStatusCode.TooManyRequests,
            HttpStatusCode.InternalServerError,
            HttpStatusCode.BadGateway,
            HttpStatusCode.ServiceUnavailable,
            HttpStatusCode.GatewayTimeout
        ];

        /// <summary>
        /// Downloads a zip file for the given jobId from the mock service and saves it to the out directory with improved retry + backoff.
        /// </summary>
        public static async Task<string> DownloadExchangeSetAsZipAsync(
            string jobId,
            HttpClient httpClientMock,
            ITestOutputHelper? output = null,
            int maxSeconds = 90,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(httpClientMock);
            if (string.IsNullOrWhiteSpace(TestBase.ProjectDirectory))
                throw new InvalidOperationException("TestBase.ProjectDirectory not initialized.");

            var deadline = DateTime.UtcNow.AddSeconds(maxSeconds);
            HttpResponseMessage? lastResponse = null;
            Exception? lastException = null;
            int attempt = 0;

            var fileName = $"V01X01_{jobId}.zip";
            var relativePath = $"/_admin/files/FSS/S100-ExchangeSets/{fileName}";
            var destinationFilePath = Path.Combine(TestBase.ProjectDirectory!, "out", fileName);

            const int initialDelayMs = 500;
            const int maxDelayMs = 6000;

            while (DateTime.UtcNow < deadline)
            {
                cancellationToken.ThrowIfCancellationRequested();
                attempt++;

                try
                {
                    lastResponse = await httpClientMock.GetAsync(
                        relativePath,
                        HttpCompletionOption.ResponseHeadersRead,
                        cancellationToken);

                    if (lastResponse.IsSuccessStatusCode)
                    {
                        // (Soft) content-type validation
                        if (lastResponse.Content.Headers.ContentType is { MediaType: var mt } &&
                            !string.IsNullOrEmpty(mt) &&
                            !mt.Contains("zip", StringComparison.OrdinalIgnoreCase))
                        {
                            output?.WriteLine($"Warning: Content-Type '{mt}' does not look like a ZIP.");
                        }

                        await using var zipStream = await lastResponse.Content.ReadAsStreamAsync(cancellationToken);

                        Directory.CreateDirectory(Path.GetDirectoryName(destinationFilePath)!);

                        // Overwrite if exists
                        await using (var fileStream = new FileStream(
                            destinationFilePath,
                            FileMode.Create,
                            FileAccess.Write,
                            FileShare.None,
                            bufferSize: 64 * 1024,
                            FileOptions.Asynchronous | FileOptions.SequentialScan))
                        {
                            await zipStream.CopyToAsync(fileStream, cancellationToken);
                        }

                        output?.WriteLine($"Download succeeded attempt #{attempt}: {relativePath}");
                        return destinationFilePath;
                    }

                    var statusCode = lastResponse.StatusCode;

                    // Non-retryable? (e.g., 400, 401, 403, 410 etc.)
                    if (!RetryableStatusCodes.Contains(statusCode))
                    {
                        var msg = $"Non-retryable status {(int)statusCode} {statusCode} on attempt {attempt} for {relativePath}";
                        output?.WriteLine(msg);
                        lastResponse.EnsureSuccessStatusCode(); // will throw
                    }

                    output?.WriteLine($"Retryable status {(int)statusCode} {statusCode} attempt {attempt} for {relativePath}");
                }
                catch (HttpRequestException hre)
                {
                    lastException = hre;
                    output?.WriteLine($"Transient network error attempt {attempt}: {hre.Message}");
                }

                // Backoff with exponential growth + jitter
                var exponential = initialDelayMs * Math.Pow(2, attempt - 1);
                var capped = (int)Math.Min(exponential, maxDelayMs);
                var jitter = Random.Shared.Next(0, 250);
                var delay = capped + jitter;

                var remaining = deadline - DateTime.UtcNow;
                if (remaining <= TimeSpan.Zero)
                    break;

                var actualDelay = TimeSpan.FromMilliseconds(Math.Min(delay, remaining.TotalMilliseconds));
                try
                {
                    await Task.Delay(actualDelay, cancellationToken);
                }
                catch (TaskCanceledException)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                }
            }

            var statusInfo = lastResponse != null
                ? $"{(int)lastResponse.StatusCode} {lastResponse.StatusCode}"
                : "no response";

            var extra = lastException != null ? $" NetworkError: {lastException.GetType().Name} {lastException.Message}" : string.Empty;

            throw new Xunit.Sdk.XunitException(
                $"Artifact not available after {maxSeconds}s (attempts={attempt}). Last status: {statusInfo}.{extra} Path: {relativePath}");
        }

        /// <summary>
        /// Compares two ZIP files to ensure their directory structures match exactly.
        /// Optionally verifies that specified product files are present in the source ZIP.
        /// </summary>
        public static void CompareZipFilesExactMatch(string sourceZipPath, string targetZipPath, string[]? products = null)
        {
            using var sourceArchive = ZipFile.OpenRead(sourceZipPath);
            using var targetArchive = ZipFile.OpenRead(targetZipPath);

            static string? GetDirectoryPath(string fullName)
            {
                var idx = fullName.LastIndexOf('/');
                return idx > 0 ? fullName[..idx] : fullName;
            }

            var sourceDirectories = sourceArchive.Entries
                .Select(e => GetDirectoryPath(e.FullName))
                .Distinct()
                .OrderBy(e => e)
                .ToList();

            var targetDirectories = targetArchive.Entries
                .Select(e => GetDirectoryPath(e.FullName))
                .Distinct()
                .OrderBy(e => e)
                .ToList();

            Assert.Equal(sourceDirectories, targetDirectories);

            if (products != null)
            {
                var expectedProductPaths = new List<string>();
                foreach (var productName in products)
                {
                    var productIdentifier = productName[..3];
                    var folderName = productName[3..7];
                    if (productIdentifier == "101")
                    {
                        expectedProductPaths.Add($"S100_ROOT/S-{productIdentifier}/SUPPORT_FILES/{productName}");
                    }
                    expectedProductPaths.Add($"S100_ROOT/S-{productIdentifier}/DATASET_FILES/{folderName}/{productName}");
                }
                expectedProductPaths.Add("S100_ROOT/CATALOG");

                var actualProductPaths = targetArchive.Entries
                    .Where(e => e.FullName.Contains('.'))
                    .Select(e => e.FullName[..e.FullName.IndexOf('.')])
                    .Distinct()
                    .OrderBy(p => p)
                    .ToList();

                expectedProductPaths.Sort();
                Assert.Equal(expectedProductPaths, actualProductPaths!);
            }
        }
    }
}
