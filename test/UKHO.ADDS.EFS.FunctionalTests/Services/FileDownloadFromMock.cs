using System;
using System.IO;
using System.Net.Http;
using System.Threading;

namespace UKHO.ADDS.EFS.FunctionalTests.Services
{
    internal static class FileDownloadFromMock
    {
        public static async Task<string> DownloadFileAsync(string urlEndPoint, string fileName, CancellationToken cancellationToken = default)
        {
            var httpClientMock = AspireResourceSingleton.httpClientMock!;

            // Initial delay to allow the mock to materialize the file
            await Task.Delay(10000, cancellationToken);

            const int maxRetries = 10;
            var retryCount = 0;
            var delay = TimeSpan.FromSeconds(10);
            HttpResponseMessage? mockResponse = null;
            Exception? lastException = null;

            // Build a robust relative URI ensuring a single slash separator
            var requestUri = new Uri($"{urlEndPoint.TrimEnd('/')}/{fileName}", UriKind.Relative);

            while (retryCount < maxRetries)
            {
                try
                {
                    var response = await httpClientMock.GetAsync(requestUri, cancellationToken);

                    try
                    {
                        response.EnsureSuccessStatusCode();
                        mockResponse = response; // success; keep for later stream copy and dispose after
                        break; // Success - exit the retry loop
                    }
                    catch (HttpRequestException ex)
                    {
                        // Dispose the response if EnsureSuccessStatusCode throws
                        response.Dispose();
                        lastException = ex;
                        throw; // rethrow to be handled below
                    }
                }
                catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound && retryCount < maxRetries - 1)
                {
                    // Only retry for 404 errors as the httpClient already has resilience for other error types
                    // StatusCode can be null for some failures; in that case, this filter won't match and will not retry here)
                    TestOutputContext.WriteLine($"File not found (attempt {retryCount + 1}/{maxRetries}). Waiting {delay.TotalSeconds} seconds before retry ... {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
                    await Task.Delay(delay, cancellationToken);

                    // Exponential backoff with a cap
                    delay = TimeSpan.FromSeconds(Math.Min(30, delay.TotalSeconds * 2));

                    retryCount++;
                    continue;
                }
                // If we get here with a non-404 error or on final attempt, let the exception propagate with context
                catch (Exception ex) when (retryCount >= maxRetries - 1)
                {
                    lastException = lastException ?? ex;
                    TestOutputContext.WriteLine($"Failed as file not found after (attempt {retryCount + 1}/{maxRetries}) ... {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
                    throw new HttpRequestException(
                        $"Failed to download exchange set after {maxRetries} attempts ... {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}",
                        lastException,
                        (lastException as HttpRequestException)?.StatusCode);
                }
            }

            // Check if we exited the loop without success
            if (mockResponse == null)
            {
                TestOutputContext.WriteLine($">>Failed as file not found after (attempt {retryCount + 1}/{maxRetries}) ... {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
                throw new HttpRequestException(
                    $">>Failed to download exchange set after {maxRetries} attempts ... {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}",
                    lastException,
                    (lastException as HttpRequestException)?.StatusCode);
            }

            using (mockResponse)
            {
                await using var fileStream = await mockResponse.Content.ReadAsStreamAsync(cancellationToken);
                var destinationFilePath = Path.Combine(AspireResourceSingleton.ProjectDirectory!, "out", fileName);

                // Ensure the directory exists
                var destinationDirectory = Path.GetDirectoryName(destinationFilePath);
                if (!Directory.Exists(destinationDirectory))
                {
                    Directory.CreateDirectory(destinationDirectory!);
                }
                await using var newFileStream = new FileStream(destinationFilePath, FileMode.Create, FileAccess.Write, FileShare.None);
                await fileStream.CopyToAsync(newFileStream, cancellationToken);
                await newFileStream.FlushAsync(cancellationToken);
                return destinationFilePath;
            }
        }

        public static Task<string> DownloadExchangeSetAsZipAsync(string jobId, CancellationToken cancellationToken = default)
        {
            return DownloadFileAsync("/_admin/files/FSS/S100-ExchangeSets/", $"V01X01_{jobId}.zip", cancellationToken);
        }

        public static Task<string> DownloadCallbackTxtAsync(string batchId, CancellationToken cancellationToken = default)
        {
            return DownloadFileAsync("/_admin/files/callback/callback-responses/", $"callback-response-{batchId}.txt", cancellationToken);
        }
    }
}
