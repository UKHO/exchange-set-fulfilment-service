using System.Text.Json;
using System.Text.Json.Nodes;
using UKHO.ADDS.Mocks.Headers;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace UKHO.ADDS.Mocks.Configuration.Mocks.scs.ResponseGenerator
{
    public class ScsResponseGenerator
    {
        private static readonly int MaxAdditionalUpdates = 4;
        private static readonly int MinEditionNumber = 1;
        private static readonly int MaxEditionNumber = 15;
        private static readonly int MinFileSize = 2000;
        private static readonly int MaxFileSize = 15000;
        private static readonly Random RandomInstance = Random.Shared;
        private static readonly string ProductNamesEndpointSource = "Product Names";
        private static readonly string ProductVersionsEndpointSource = "Product Versions";

        /// <summary>
        /// Provides a mock response for product names based on the requested products.
        /// </summary>
        public static async Task<IResult> ProvideProductNamesResponse(HttpRequest requestMessage, string state = "")
        {
            try
            {
                var validationResult = await ValidateRequestAsync(requestMessage);
                if (validationResult.errorResult != null)
                    return validationResult.errorResult;

                var response = GenerateProductNamesResponse(validationResult.requestedProducts, state);
                return Results.Ok(response);
            }
            catch (Exception ex)
            {
                return Results.Problem($"Error processing request: {ex.Message}");
            }
        }

        public static async Task<IResult> ProvideProductVersionsResponse(HttpRequest requestMessage, string state = "")
        {
            try
            {
                var validationResult = await ValidateProductVersionsRequestAsync(requestMessage);
                if (validationResult.errorResult != null)
                    return validationResult.errorResult;

                var response = GenerateProductNamesResponse(validationResult.requestedProducts, state);
                return Results.Ok(response);
            }
            catch (Exception ex)
            {
                return Results.Problem($"Error processing request: {ex.Message}");
            }
        }

        private static async Task<(IResult? errorResult, List<string> requestedProducts)> ValidateRequestAsync(HttpRequest request)
        {
            var requestedProducts = new List<string>();

            request.EnableBuffering();
            string requestBody;
            using (var reader = new StreamReader(request.Body, leaveOpen: true))
            {
                requestBody = await reader.ReadToEndAsync();
                request.Body.Position = 0;
            }

            if (string.IsNullOrWhiteSpace(requestBody))
                return (CreateBadRequestResponse(request, "Request body is required", ProductNamesEndpointSource), requestedProducts);

            try
            {
                using var doc = JsonDocument.Parse(requestBody);
                if (doc.RootElement.ValueKind != JsonValueKind.Array)
                    return (CreateBadRequestResponse(request, "Request body must be a JSON array of product names.", ProductNamesEndpointSource), requestedProducts);

                foreach (var element in doc.RootElement.EnumerateArray())
                {
                    if (element.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(element.GetString()))
                        return (CreateBadRequestResponse(request, "All items in the array must be non-empty strings.", ProductNamesEndpointSource), requestedProducts);

                    requestedProducts.Add(element.GetString()!);
                }

                if (!requestedProducts.Any())
                    return (CreateBadRequestResponse(request, "Empty product name is not allowed.", ProductNamesEndpointSource), requestedProducts);

                return (null, requestedProducts);
            }
            catch (JsonException)
            {
                return (CreateBadRequestResponse(request, "Invalid JSON format.", ProductNamesEndpointSource), requestedProducts);
            }
        }

        private static JsonObject GenerateProductNamesResponse(List<string> requestedProducts, string state = "")
        {
            var productsArray = new JsonArray();
            var notReturnedArray = new JsonArray();

            if (state == "get-allinvalidproducts")
            {
                foreach (var productName in requestedProducts)
                {
                    notReturnedArray.Add(CreateProductNotReturnedObject(productName, "invalidProduct"));
                }
            }
            else if (state == "get-invalidproducts" && requestedProducts.Count > 0)
            {
                foreach (var productName in requestedProducts.SkipLast(1))
                    productsArray.Add(GenerateProductJson(productName));

                var lastProduct = requestedProducts.Last();
                notReturnedArray.Add(CreateProductNotReturnedObject(lastProduct, "invalidProduct"));
            }
            else if (state == "get-cancelledproducts" && requestedProducts.Count > 0)
            {
                foreach (var productName in requestedProducts.SkipLast(1))
                    productsArray.Add(GenerateProductJson(productName));

                var lastProduct = requestedProducts.Last();
                productsArray.Add(GenerateProductJson(lastProduct, true));
            }
            else if (state == "get-productswithdrawn" && requestedProducts.Count > 0)
            {
                foreach (var productName in requestedProducts.SkipLast(1))
                    productsArray.Add(GenerateProductJson(productName));

                var lastProduct = requestedProducts.Last();
                notReturnedArray.Add(CreateProductNotReturnedObject(lastProduct, "productWithdrawn"));
            }
            else
            {
                foreach (var productName in requestedProducts)
                    productsArray.Add(GenerateProductJson(productName));
            }

            return new JsonObject
            {
                ["productCounts"] = new JsonObject
                {
                    ["requestedProductCount"] = requestedProducts.Count,
                    ["returnedProductCount"] = (state == "get-invalidproducts" && requestedProducts.Count > 0) ? requestedProducts.Count - notReturnedArray.Count : productsArray.Count,
                    ["requestedProductsAlreadyUpToDateCount"] = 0,
                    ["requestedProductsNotReturned"] = notReturnedArray
                },
                ["products"] = productsArray
            };
        }

        private static JsonObject CreateProductNotReturnedObject(string productName, string reason)
        {
            return new JsonObject
            {
                ["productName"] = productName,
                ["reason"] = reason
            };
        }

        private static JsonObject GenerateProductJson(string productName, bool cancelled = false)
        {
            var editionNumber = RandomInstance.Next(MinEditionNumber, MaxEditionNumber);
            var fileSize = RandomInstance.Next(MinFileSize, MaxFileSize);
            var baseDate = DateTime.UtcNow;

            var updateNumbersArray = new JsonArray { 0 };
            var datesArray = new JsonArray
            {
                new JsonObject
                {
                    ["issueDate"] = baseDate.ToString("o"),
                    ["updateApplicationDate"] = baseDate.ToString("o"),
                    ["updateNumber"] = 0
                }
            };

            if (productName.StartsWith("101"))
            {
                var additionalUpdateCount = RandomInstance.Next(0, MaxAdditionalUpdates);

                var updates = Enumerable.Range(1, 1 + additionalUpdateCount)
                    .Select(i =>
                    {
                        var currentDate = baseDate.AddDays(i * 5);
                        updateNumbersArray.Add(i);
                        return new JsonObject
                        {
                            ["issueDate"] = currentDate.ToString("o"),
                            ["updateNumber"] = i
                        };
                    }).ToList();

                foreach (var update in updates)
                    datesArray.Add(update);
            }

            var productObj = new JsonObject
            {
                ["editionNumber"] = editionNumber,
                ["productName"] = productName,
                ["updateNumbers"] = updateNumbersArray,
                ["dates"] = datesArray
            };

            // 30% chance to add cancellation
            if (cancelled)
            {
                var updateNumber = updateNumbersArray.Count > 0
                    ? updateNumbersArray.Max(node => node.GetValue<int>())
                    : 0;

                productObj["cancellation"] = new JsonObject
                {
                    ["editionNumber"] = 0,
                    ["updateNumber"] = updateNumber
                };
            }

            productObj["fileSize"] = fileSize;

            return productObj;
        }

        private static IResult CreateBadRequestResponse(HttpRequest requestMessage, string description, string endpointSource)
        {
            var correlationId = requestMessage.Headers.ContainsKey(WellKnownHeader.CorrelationId)
                ? requestMessage.Headers[WellKnownHeader.CorrelationId].ToString()
                : string.Empty;

            return Results.BadRequest(new
            {
                correlationId,
                errors = new[]
                {
                    new { source = endpointSource, description }
                }
            });
        }

        private static async Task<(IResult? errorResult, List<string> requestedProducts)> ValidateProductVersionsRequestAsync(HttpRequest request)
        {
            var requestedProducts = new List<string>();

            request.EnableBuffering();
            string requestBody;
            using (var reader = new StreamReader(request.Body, leaveOpen: true))
            {
                requestBody = await reader.ReadToEndAsync();
                request.Body.Position = 0;
            }

            if (string.IsNullOrWhiteSpace(requestBody))
                return (CreateBadRequestResponse(request, "Request body is required", ProductVersionsEndpointSource), requestedProducts);

            try
            {
                using var doc = JsonDocument.Parse(requestBody);
                if (doc.RootElement.ValueKind != JsonValueKind.Array)
                    return (CreateBadRequestResponse(request, "Request body must be a JSON array of productVersions objects.", ProductVersionsEndpointSource), requestedProducts);

                foreach (var element in doc.RootElement.EnumerateArray())
                {
                    if (element.ValueKind != JsonValueKind.Object)
                        return (CreateBadRequestResponse(request, "All items in the array must be productVersions objects with productName, editionNumber, and updateNumber properties.", ProductVersionsEndpointSource), requestedProducts);

                    // Validate productName property
                    if (!element.TryGetProperty("productName", out var productNameProperty) ||
                        productNameProperty.ValueKind != JsonValueKind.String ||
                        string.IsNullOrWhiteSpace(productNameProperty.GetString()))
                        return (CreateBadRequestResponse(request, "Each productVersions object must have a non-empty 'productName' string property.", ProductVersionsEndpointSource), requestedProducts);

                    // Validate editionNumber property
                    if (!element.TryGetProperty("editionNumber", out var editionNumberProperty) ||
                        editionNumberProperty.ValueKind != JsonValueKind.Number ||
                        !editionNumberProperty.TryGetInt32(out var editionNumber) ||
                        editionNumber < 1)
                        return (CreateBadRequestResponse(request, "Each productVersions object must have an 'editionNumber' integer property with a value greater than 0.", ProductVersionsEndpointSource), requestedProducts);

                    // Validate updateNumber property
                    if (!element.TryGetProperty("updateNumber", out var updateNumberProperty) ||
                        updateNumberProperty.ValueKind != JsonValueKind.Number ||
                        !updateNumberProperty.TryGetInt32(out var updateNumber) ||
                        updateNumber < 0)
                        return (CreateBadRequestResponse(request, "Each productVersions object must have an 'updateNumber' integer property with a value greater than or equal to 0.", ProductVersionsEndpointSource), requestedProducts);

                    requestedProducts.Add(productNameProperty.GetString()!);
                }

                if (!requestedProducts.Any())
                    return (CreateBadRequestResponse(request, "At least one product must be provided.", ProductVersionsEndpointSource), requestedProducts);

                return (null, requestedProducts);
            }
            catch (JsonException)
            {
                return (CreateBadRequestResponse(request, "Invalid JSON format.", ProductVersionsEndpointSource), requestedProducts);
            }
        }
    }
}
