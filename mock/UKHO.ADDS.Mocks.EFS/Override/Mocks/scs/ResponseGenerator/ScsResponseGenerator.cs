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

                var response = GenerateProductsResponse(validationResult.requestedProducts, state);
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
                var requestedProducts = await ExtractProductNamesFromRequestAsync(requestMessage);

                var response = GenerateProductsResponse(requestedProducts, state);
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

        private static JsonObject GenerateProductsResponse(List<string> requestedProducts, string state = "")
        {
            var productsArray = new JsonArray();
            var notReturnedArray = new JsonArray();
            var productCount = requestedProducts.Count;

            switch (state)
            {
                case "get-allinvalidproducts":
                    foreach (var productName in requestedProducts)
                    {
                        notReturnedArray.Add(CreateProductNotReturnedObject(productName, "invalidProduct"));
                    }
                    break;

                case "get-invalidproducts" when productCount > 0:
                    foreach (var productName in requestedProducts.SkipLast(1))
                        productsArray.Add(GenerateProductJson(productName));

                    notReturnedArray.Add(CreateProductNotReturnedObject(requestedProducts.Last(), "invalidProduct"));
                    break;


                case "get-cancelledproducts" when productCount > 0:

                    foreach (var productName in requestedProducts.SkipLast(1))
                        productsArray.Add(GenerateProductJson(productName));

                    productsArray.Add(GenerateProductJson(requestedProducts.Last(), true));
                    break;

                case "get-productwithdrawn" when productCount > 0:

                    foreach (var productName in requestedProducts.SkipLast(1))
                        productsArray.Add(GenerateProductJson(productName));

                    notReturnedArray.Add(CreateProductNotReturnedObject(requestedProducts.Last(), "productWithdrawn"));
                    break;

                default:

                    foreach (var productName in requestedProducts)
                        productsArray.Add(GenerateProductJson(productName));
                    break;
            }
            var returnedProductCount = state == "get-invalidproducts" && productCount > 0
? productCount - notReturnedArray.Count
: productsArray.Count;
            return new JsonObject
            {
                ["productCounts"] = new JsonObject
                {
                    ["requestedProductCount"] = productCount,
                    ["returnedProductCount"] = returnedProductCount,
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

            productObj["fileSize"] = cancelled ? 0 : fileSize;

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

        private static async Task<List<string>> ExtractProductNamesFromRequestAsync(HttpRequest request)
        {
            request.EnableBuffering();

            using var reader = new StreamReader(request.Body, leaveOpen: true);
            var requestBody = await reader.ReadToEndAsync();
            request.Body.Position = 0;

            using var doc = JsonDocument.Parse(requestBody);
            var requestedProducts = new List<string>(doc.RootElement.GetArrayLength());

            foreach (var element in doc.RootElement.EnumerateArray())
            {
                if (element.TryGetProperty("productName", out var productNameProperty) &&
                    productNameProperty.ValueKind == JsonValueKind.String)
                {
                    var productName = productNameProperty.GetString();
                    if (!string.IsNullOrEmpty(productName))
                    {
                        requestedProducts.Add(productName);
                    }
                }
            }

            return requestedProducts;
        }
    }
}
