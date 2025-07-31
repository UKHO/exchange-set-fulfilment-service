using System.Reflection;
using Microsoft.AspNetCore.Http.Metadata;

namespace UKHO.ADDS.Configuration.AACEmulator.Common
{
    public class PreconditionFailedResult :
        IResult,
        IEndpointMetadataProvider,
        IStatusCodeHttpResult
    {
        public static void PopulateMetadata(MethodInfo method, EndpointBuilder builder)
        {
            ArgumentNullException.ThrowIfNull(method);
            ArgumentNullException.ThrowIfNull(builder);

            builder.Metadata.Add(new ProducesResponseTypeMetadata(StatusCodes.Status412PreconditionFailed, typeof(void)));
        }

        public Task ExecuteAsync(HttpContext httpContext)
        {
            if (StatusCode.HasValue)
            {
                httpContext.Response.StatusCode = StatusCode.Value;
            }

            return Task.CompletedTask;
        }

        public int? StatusCode => StatusCodes.Status412PreconditionFailed;
    }
}
