using UKHO.Clients.FileShare.Models;
using UKHO.Infrastructure.Results;
using Error = UKHO.Infrastructure.Results.Error;

namespace UKHO.Clients.FileShare.Internal
{
    internal static class ResultFactory
    {
        internal static async Task<IResult<Stream>> WithStreamData(HttpResponseMessage? response)
        {
            Stream data = default;

            if (response.IsSuccessStatusCode && response.HasContent())
            {
                data = await response.ReadAsStreamAsync();
            }

            return await CreateResultAsync(response, data);
        }

        internal static async Task<IResult<T>> WithObjectData<T>(HttpResponseMessage response)
        {
            T data = default;

            if (response.IsSuccessStatusCode && response.HasContent())
            {
                data = await response.ReadAsTypeAsync<T>();
            }

            return await CreateResultAsync(response, data);
        }

        //this is strange and exists to keep backwards compatibility for the FileShareApiClient.DownloadFileAsync method
        internal static async Task<IResult<T>> WithNullData<T>(HttpResponseMessage response) where T : class
        {
            return await CreateResultAsync<T>(response, null);
        }

        private static async Task<IResult<T>> CreateResultAsync<T>(HttpResponseMessage? response, T data)
        {
            if (response == null)
            {
                return Result.Failure<T>(new Error("No response received"));
            }

            if (response.IsSuccessStatusCode)
            {
                return Result.Success(data);
            }

            if (!response.IsSuccessStatusCode && response.HasContent())
            {
                try
                {
                    var errorResponse = await response.ReadAsTypeAsync<ErrorResponseModel>();
                    return Result.Failure<T>(new Error());
                }
                catch
                {
                    var stringContent = await response.Content.ReadAsStringAsync();
                    return Result.Failure<T>(new Error(stringContent));
                }
            }

            return Result.Failure<T>("Response has no content");
        }

        private static bool HasContent(this HttpResponseMessage? response)
        {
            if (response == null)
            {
                return false;
            }

            return response.Content.GetType().Name != "EmptyContent";
        }
    }
}
