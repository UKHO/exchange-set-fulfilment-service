using System.Globalization;
using UKHO.ADDS.Mocks.Configuration.Mocks.scs.Generators;
using UKHO.ADDS.Mocks.Headers;
using UKHO.ADDS.Mocks.Markdown;
using UKHO.ADDS.Mocks.States;

namespace UKHO.ADDS.Mocks.Configuration.Mocks.scs
{
    /// <summary>
    /// Mock endpoint for Sales Catalogue Service UpdatesSince API.
    /// Simulates the /v2/products/s100/updatesSince endpoint for development and testing.
    /// </summary>
    public class UpdatesSinceEndpoint : ServiceEndpointMock
    {
        private const string DataFileName = "s100-updates-since.json";
        private const string DateTimeFormat = "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'";
        private const string LargeExchangeSetsState = "get-largeexchangesets";

        public override void RegisterSingleEndpoint(IEndpointMock endpoint) =>
            endpoint.MapGet("/v2/products/s100/updatesSince", async (string sinceDateTime, string? productIdentifier, HttpRequest request, HttpResponse response) =>
            {
                EchoHeaders(request, response, [WellKnownHeader.CorrelationId]);

                var state = GetState(request);

                return state switch
                {
                    WellKnownState.Default => await HandleUpdatesSinceRequest(productIdentifier, sinceDateTime, response),
                    LargeExchangeSetsState => await HandleUpdatesSinceRequest(productIdentifier, sinceDateTime, response, LargeExchangeSetsState),
                    WellKnownState.NotModified => HandleNotModified(response, sinceDateTime),
                    WellKnownState.BadRequest => ResponseGenerator.CreateBadRequestResponse(request, "Updates Since", "Bad Request."),
                    WellKnownState.NotFound => ResponseGenerator.CreateNotFoundResponse(request),
                    WellKnownState.UnsupportedMediaType => ResponseGenerator.CreateUnsupportedMediaTypeResponse(),
                    WellKnownState.InternalServerError => ResponseGenerator.CreateInternalServerErrorResponse(request),
                    _ => WellKnownStateHandler.HandleWellKnownState(state)
                };
            })
            .Produces<string>()
            .WithEndpointMetadata(endpoint, d =>
            {
                d.Append(new MarkdownHeader("Updates Since Endpoint", 3));
                d.Append(new MarkdownParagraph("This endpoint returns all releasable changes to S-100 maritime products since a specified date."));

                d.Append(new MarkdownHeader("Parameters", 4));
                d.Append(new MarkdownParagraph("**Required Parameter:** sinceDateTime - The date and time from which changes are requested (ISO 8601 format)."));
                d.Append(new MarkdownParagraph("**Optional Parameter:** productIdentifier - Filter by S-100 standard specification (e.g., 's101', 's102', 's104', 's111')."));

                d.Append(new MarkdownHeader("Mock Behavior", 4));
                d.Append(new MarkdownParagraph($"**Data Source:** Static data loaded from {DataFileName}"));
                d.Append(new MarkdownParagraph("**Filtering:** Supports product identifier filtering when specified"));
                d.Append(new MarkdownParagraph("**Validation:** No date range validation - accepts any sinceDateTime parameter"));

                d.Append(new MarkdownHeader("Testing States", 4));
                d.Append(new MarkdownParagraph("Supports various mock states for testing different scenarios (BadRequest, NotFound, UnsupportedMediaType, InternalServerError, NotModified)."));
                
                d.Append(new MarkdownHeader($"Try out the {LargeExchangeSetsState} state!", 3));
                d.Append(new MarkdownParagraph("The response mimics a situation where all products have large file sizes (6-10MB). This simulates the scenario where the exchange set will be large in size."));
            });

        /// <summary>
        /// Handles updates since requests by loading data from the static JSON file.
        /// </summary>
        /// <param name="productIdentifier">Optional product identifier filter</param>
        /// <param name="sinceDateTime">DateTime string to parse and use for LastModified header</param>
        /// <param name="response">HTTP response to set headers on</param>
        /// <param name="state">Optional state parameter for specialized response handling</param>
        private async Task<IResult> HandleUpdatesSinceRequest(string? productIdentifier, string sinceDateTime, HttpResponse response, string? state = null)
        {
            SetLastModifiedHeader(response, sinceDateTime, addDayOffset: true);

            var pathResult = GetFile(DataFileName);
            if (!pathResult.IsSuccess(out var file))
            {
                return Results.NotFound($"Could not find {DataFileName} file");
            }

            return state is null
                ? await ResponseGenerator.ProvideUpdatesSinceResponse(productIdentifier, file)
                : await ResponseGenerator.ProvideUpdatesSinceResponse(productIdentifier, file, state);
        }

        /// <summary>
        /// Handles the NotModified state (304 response).
        /// </summary>
        private static IResult HandleNotModified(HttpResponse response, string sinceDateTime)
        {
            SetLastModifiedHeader(response, sinceDateTime, addDayOffset: false, dayOffset: -1);
            return Results.StatusCode(304);
        }

        /// <summary>
        /// Sets the LastModified header based on the provided sinceDateTime string.
        /// </summary>
        /// <param name="response">HTTP response to set the header on</param>
        /// <param name="sinceDateTime">DateTime string to parse</param>
        /// <param name="addDayOffset">Whether to add a day offset when the parsed date is in the past</param>
        /// <param name="dayOffset">Number of days to offset (default: 1)</param>
        private static void SetLastModifiedHeader(HttpResponse response, string sinceDateTime, bool addDayOffset = false, int dayOffset = 1)
        {
            if (DateTime.TryParse(sinceDateTime, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsedDate))
            {
                var modifiedDate = parsedDate;
                if (addDayOffset && parsedDate < DateTime.UtcNow)
                {
                    modifiedDate = parsedDate.AddDays(dayOffset);
                }
                else if (!addDayOffset)
                {
                    modifiedDate = parsedDate.AddDays(dayOffset);
                }
                
                response.Headers.LastModified = modifiedDate.ToString(DateTimeFormat, CultureInfo.InvariantCulture);
            }
            else
            {
                response.Headers.LastModified = sinceDateTime;
            }
        }
    }
}
