using UKHO.ADDS.Mocks.Configuration.Mocks.scs.Helpers;
using UKHO.ADDS.Mocks.Headers;
using UKHO.ADDS.Mocks.Markdown;
using UKHO.ADDS.Mocks.States;

namespace UKHO.ADDS.Mocks.Configuration.Mocks.scs
{
    /// <summary>
    /// Base class for SCS (Sales Catalogue Service) endpoints to eliminate code duplication
    /// </summary>
    public abstract class ScsEndpointBase : ServiceEndpointMock
    {
        protected const string ApplicationJson = "application/json";

        /// <summary>
        /// Validates Content-Type header for POST requests requiring JSON
        /// </summary>
        protected static IResult? ValidateContentType(HttpRequest request)
        {
            var contentTypeHeader = request.Headers.ContentType.ToString();
            
            if (string.IsNullOrEmpty(contentTypeHeader) || 
                !contentTypeHeader.Contains(ApplicationJson, StringComparison.OrdinalIgnoreCase))
            {
                return ResponseHelper.CreateUnsupportedMediaTypeResponse();
            }

            return null;
        }

        protected static void AddCommonScsDocumentation(IMarkdownDocument documentBuilder)
        {
            documentBuilder.Append(new MarkdownHeader("Try out the get-invalidproducts state!", 3));
            documentBuilder.Append(new MarkdownParagraph("The response mimics a situation where one of the requested products is unavailable. The final item in the request is omitted from the returned list and is instead flagged as 'not returned', along with a reason like 'invalidProduct'."));

            documentBuilder.Append(new MarkdownHeader("Try out the get-allinvalidproducts state!", 3));
            documentBuilder.Append(new MarkdownParagraph("The response mimics a situation where ALL requested products are invalid. No products are returned and all requested products are flagged as 'not returned' with reason 'invalidProduct'. This simulates the scenario where an error should be logged and no exchange set should be created."));
        }

        /// <summary>
        /// Configures endpoint metadata with the specified title, description, and optional additional states
        /// </summary>
        protected static void ConfigureEndpointMetadata(
            IEndpointMock endpoint,
            IMarkdownDocument documentBuilder,
            string endpointTitle,
            string endpointDescription,
            params (string stateTitle, string stateDescription)[] additionalStates)
        {
            // Add main endpoint documentation
            documentBuilder.Append(new MarkdownHeader(endpointTitle, 3));
            documentBuilder.Append(new MarkdownParagraph(endpointDescription));

            // Add common SCS documentation
            AddCommonScsDocumentation(documentBuilder);

            // Add any additional endpoint-specific states
            foreach (var (stateTitle, stateDescription) in additionalStates)
            {
                documentBuilder.Append(new MarkdownHeader(stateTitle, 3));
                documentBuilder.Append(new MarkdownParagraph(stateDescription));
            }
        }
    }
}
