using System.Text.Json;
using System.Text.Json.Nodes;
using UKHO.ADDS.Mocks;
using UKHO.ADDS.Mocks.EFS.Override.Mocks.scs.Models;
using UKHO.ADDS.Mocks.Files;
using UKHO.ADDS.Mocks.Headers;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace UKHO.ADDS.Mocks.Configuration.Mocks.scs.Generators
{
    public class ResponseGenerator
    {
        private static readonly int MaxAdditionalUpdates = 4;
        private static readonly int MinEditionNumber = 1;
        private static readonly int MaxEditionNumber = 15;
        private static readonly int MinFileSize = 2000;
        private static readonly int MaxFileSize = 15000;
        private static readonly Random RandomInstance = Random.Shared;

        private static readonly string InvalidProduct = "invalidProduct";
        private static readonly string InvalidProductWithdrawn = "productWithdrawn";

        #region Response Helper Methods

        /// <summary>
        /// Safely extracts correlation ID from request headers
        /// </summary>
        /// <param name="request">The HTTP request</param>
        /// <returns>The correlation ID or empty string if not found</returns>
        public static string GetCorrelationId(HttpRequest request)
        {
            return request.Headers.ContainsKey(WellKnownHeader.CorrelationId)
                ? request.Headers[WellKnownHeader.CorrelationId].ToString()
                : string.Empty;
        }

        /// <summary>
        /// Creates a standardized 400 Bad Request response with correlation ID and errors
        /// </summary>
        /// <param name="request">The HTTP request</param>
        /// <param name="source">The source of the error</param>
        /// <param name="description">The error description</param>
        /// <returns>A 400 Bad Request IResult</returns>
        public static IResult CreateBadRequestResponse(HttpRequest request, string source, string description)
        {
            return Results.Json(new
            {
                correlationId = GetCorrelationId(request),
                errors = new[]
                {
                    new
                    {
                        source,
                        description
                    }
                }
            }, statusCode: 400);
        }

        /// <summary>
        /// Creates a standardized 404 Not Found response with correlation ID
        /// </summary>
        /// <param name="request">The HTTP request</param>
        /// <param name="detail">Optional details for the not found response</param>
        /// <returns>A 404 Not Found IResult</returns>
        public static IResult CreateNotFoundResponse(HttpRequest request, string detail = "Not Found")
        {
            return Results.Json(new
            {
                correlationId = GetCorrelationId(request),
                detail
            }, statusCode: 404);
        }

        /// <summary>
        /// Creates a standardized 415 Unsupported Media Type response
        /// </summary>
        /// <param name="typeUri">The RFC URI for the error type</param>
        /// <param name="traceId">Optional trace ID</param>
        /// <returns>A 415 Unsupported Media Type IResult</returns>
        public static IResult CreateUnsupportedMediaTypeResponse(
            string? typeUri = null,
            string? traceId = null)
        {
            const string UnsupportedMediaTypeUri = "https://tools.ietf.org/html/rfc9110#section-15.5.16";
            const int MockTraceIdLength = 11;

            return Results.Json(new
            {
                type = typeUri ?? UnsupportedMediaTypeUri,
                title = "Unsupported Media Type",
                status = 415,
                traceId = traceId ?? Guid.NewGuid().ToString("D")[..MockTraceIdLength]
            }, statusCode: 415);
        }

        /// <summary>
        /// Creates a standardized 500 Internal Server Error response with correlation ID
        /// </summary>
        /// <param name="request">The HTTP request</param>
        /// <param name="detail">Optional details for the error</param>
        /// <returns>A 500 Internal Server Error IResult</returns>
        public static IResult CreateInternalServerErrorResponse(HttpRequest request, string detail = "Internal Server Error")
        {
            return Results.Json(new
            {
                correlationId = GetCorrelationId(request),
                detail
            }, statusCode: 500);
        }

        #endregion

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

        /// <summary>
        /// Provides a mock response for updates since a specified date using data from s100-updates-since.json file.
        /// Note: As a mock endpoint, this method returns static data regardless of date parameters.
        /// </summary>
        public static async Task<IResult> ProvideUpdatesSinceResponse(string? productIdentifier, IMockFile file)
        {
            try
            {
                var response = await GenerateUpdatesSinceResponseFromFile(productIdentifier, file);
                return Results.Ok(response);
            }
            catch (Exception ex)
            {
                return Results.Problem($"Error processing UpdatesSince request: {ex.Message}");
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
                return (CreateBadRequestResponse(request, "Request Body", "Request body is required"), requestedProducts);

            try
            {
                using var doc = JsonDocument.Parse(requestBody);
                if (doc.RootElement.ValueKind != JsonValueKind.Array)
                    return (CreateBadRequestResponse(request, "Request Body", "Request body must be a JSON array of product names."), requestedProducts);

                foreach (var element in doc.RootElement.EnumerateArray())
                {
                    if (element.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(element.GetString()))
                        return (CreateBadRequestResponse(request, "Product Names", "All items in the array must be non-empty strings."), requestedProducts);

                    requestedProducts.Add(element.GetString()!);
                }

                if (!requestedProducts.Any())
                    return (CreateBadRequestResponse(request, "Product Names", "Empty product name is not allowed."), requestedProducts);

                return (null, requestedProducts);
            }
            catch (JsonException)
            {
                return (CreateBadRequestResponse(request, "JSON Format", "Invalid JSON format."), requestedProducts);
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
                    notReturnedArray.Add(CreateProductNotReturnedObject(productName, InvalidProduct));
                }
            }
            else if (state == "get-invalidproducts" && requestedProducts.Count > 0)
            {
                foreach (var productName in requestedProducts.SkipLast(1))
                    productsArray.Add(GenerateProductJson(productName));

                var lastProduct = requestedProducts.Last();
                notReturnedArray.Add(CreateProductNotReturnedObject(lastProduct, InvalidProduct));
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

        private static JsonObject GenerateProductJson(string productName)
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
            if (RandomInstance.Next(0, 10) < 3)
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

        private static async Task<JsonObject> GenerateUpdatesSinceResponseFromFile(string? productIdentifier, IMockFile file)
        {
            var allProducts = await LoadProductsFromFileAsync(file);

            var filteredProducts = FilterProductsByIdentifier(allProducts, productIdentifier);

            var productsArray = BuildProductsArray(filteredProducts);

            return CreateResponseObject(productsArray);
        }

        private static async Task<List<JsonNode>> LoadProductsFromFileAsync(IMockFile file)
        {
            try
            {
                using var stream = file.Open();
                using var reader = new StreamReader(stream);
                var jsonContent = await reader.ReadToEndAsync();
                var jsonArray = JsonNode.Parse(jsonContent);

                return jsonArray is JsonArray array
                    ? array.ToList()
                    : throw new InvalidOperationException("Invalid JSON format in s100-updates-since.json file - expected array");
            }
            catch (JsonException ex)
            {
                throw new InvalidOperationException("Failed to parse JSON content from s100-updates-since.json file", ex);
            }
        }

        private static List<JsonNode> FilterProductsByIdentifier(List<JsonNode> allProducts, string? productIdentifier)
        {
            if (string.IsNullOrWhiteSpace(productIdentifier))
                return allProducts;

            var prefix = GetProductPrefix(productIdentifier.ToLowerInvariant());

            if (prefix is null)
                return new List<JsonNode>();

            return allProducts
                .Where(p => p?["productName"]?.GetValue<string>()?.StartsWith(prefix) == true)
                .ToList();
        }

        private static string? GetProductPrefix(string identifier) => identifier switch
        {
            "s101" => "101",
            "s102" => "102",
            "s104" => "104",
            "s111" => "111",
            _ => null
        };

        private static JsonArray BuildProductsArray(List<JsonNode> filteredProducts)
        {
            var productsArray = new JsonArray();

            foreach (var product in filteredProducts)
            {
                productsArray.Add(product.DeepClone());
            }

            return productsArray;
        }

        private static JsonObject CreateResponseObject(JsonArray productsArray)
        {
            return new JsonObject
            {
                ["productCounts"] = new JsonObject
                {
                    ["requestedProductCount"] = 0,
                    ["returnedProductCount"] = productsArray.Count,
                    ["requestedProductsAlreadyUpToDateCount"] = 0,
                    ["requestedProductsNotReturned"] = new JsonArray()
                },
                ["products"] = productsArray
            };
        }

        /// <summary>
        /// Provides a mock response for product names based on the requested products.
        /// </summary>
        public static async Task<IResult> ProvideProductVersionsResponse(HttpRequest requestMessage, string state = "")
        {
            try
            {
                var requestedProducts = await ExtractProductNamesFromRequestAsync(requestMessage);

                var response = GenerateProductsVersionsResponse(requestedProducts, state);
                return Results.Ok(response);
            }
            catch (Exception ex)
            {
                return Results.Problem($"Error processing request: {ex.Message}");
            }
        }

        private static JsonObject GenerateProductsVersionsResponse(List<ProductVersionRequest> requestedProducts, string state = "")
        {
            var productsArray = new JsonArray();
            var notReturnedArray = new JsonArray();
            var requestedProductCount = requestedProducts.Count;

            ProcessProductsByState(requestedProducts, state, productsArray, notReturnedArray);

            var responseCounts = CalculateResponseCounts(requestedProductCount, productsArray.Count, notReturnedArray.Count, state);

            return new JsonObject
            {
                ["productCounts"] = new JsonObject
                {
                    ["requestedProductCount"] = responseCounts.RequestedCount,
                    ["returnedProductCount"] = responseCounts.ReturnedCount,
                    ["requestedProductsAlreadyUpToDateCount"] = responseCounts.AlreadyUpToDateCount,
                    ["requestedProductsNotReturned"] = notReturnedArray
                },
                ["products"] = productsArray
            };
        }

        private static void ProcessProductsByState(List<ProductVersionRequest> requestedProducts, string state, JsonArray productsArray, JsonArray notReturnedArray)
        {
            var productCount = requestedProducts.Count;

            switch (state)
            {
                case "get-allinvalidproducts":
                    AddAllProductsAsNotReturned(requestedProducts, notReturnedArray, InvalidProduct);
                    break;

                case "get-invalidproducts" when productCount > 0:
                    ProcessProductsWithLastAsNotReturned(requestedProducts, productsArray, notReturnedArray, InvalidProduct);
                    break;

                case "get-productwithdrawn" when productCount > 0:
                    ProcessProductsWithLastAsNotReturned(requestedProducts, productsArray, notReturnedArray, InvalidProductWithdrawn);
                    break;

                case "get-productalreadytuptodate" when productCount > 0:
                    ProcessProductsExceptLast(requestedProducts, productsArray);
                    break;

                case "get-cancelledproducts" when productCount > 0:
                    ProcessProductsWithLastCancelled(requestedProducts, productsArray);
                    break;

                default:
                    ProcessAllProducts(requestedProducts, productsArray);
                    break;
            }
        }

        private static void AddAllProductsAsNotReturned(List<ProductVersionRequest> requestedProducts, JsonArray notReturnedArray, string reason)
        {
            foreach (var product in requestedProducts)
            {
                notReturnedArray.Add(CreateProductsVersionsNotReturnedObject(product, reason));
            }
        }

        private static void ProcessProductsWithLastAsNotReturned(List<ProductVersionRequest> requestedProducts, JsonArray productsArray, JsonArray notReturnedArray, string reason)
        {
            foreach (var product in requestedProducts.SkipLast(1))
            {
                productsArray.Add(GenerateProductsVersionsJson(product));
            }
            notReturnedArray.Add(CreateProductsVersionsNotReturnedObject(requestedProducts.Last(), reason));
        }

        private static void ProcessProductsExceptLast(List<ProductVersionRequest> requestedProducts, JsonArray productsArray)
        {
            foreach (var product in requestedProducts.SkipLast(1))
            {
                productsArray.Add(GenerateProductsVersionsJson(product));
            }
        }

        private static void ProcessProductsWithLastCancelled(List<ProductVersionRequest> requestedProducts, JsonArray productsArray)
        {
            foreach (var product in requestedProducts.SkipLast(1))
            {
                productsArray.Add(GenerateProductsVersionsJson(product));
            }
            productsArray.Add(GenerateProductsVersionsJson(requestedProducts.Last(), cancelled: true));
        }

        private static void ProcessAllProducts(List<ProductVersionRequest> requestedProducts, JsonArray productsArray)
        {
            foreach (var product in requestedProducts)
            {
                productsArray.Add(GenerateProductsVersionsJson(product));
            }
        }

        private static (int RequestedCount, int ReturnedCount, int AlreadyUpToDateCount) CalculateResponseCounts(int requestedCount, int productsArrayCount, int notReturnedCount, string state)
        {
            var returnedCount = IsStateWithNotReturnedProducts(state) && requestedCount > 0
                ? requestedCount - notReturnedCount
                : productsArrayCount;

            var alreadyUpToDateCount = state == "get-productalreadytuptodate" ? 1 : 0;

            return (requestedCount, returnedCount, alreadyUpToDateCount);
        }

        private static bool IsStateWithNotReturnedProducts(string state)
        {
            return state == "get-invalidproducts" || state == "get-productwithdrawn";
        }

        private static JsonObject GenerateProductsVersionsJson(ProductVersionRequest productRequest, bool cancelled = false)
        {
            var editionNumber = productRequest.ProductName.StartsWith("101") ? productRequest.EditionNumber : productRequest.EditionNumber + 1;
            var fileSize = RandomInstance.Next(MinFileSize, MaxFileSize);
            var baseDate = DateTime.UtcNow;

            var updateNumbersArray = new JsonArray { 0 };
            var updateNumbersArrayS101 = new JsonArray { productRequest.UpdateNumber + 1 };
            var datesArray = new JsonArray
    {
        new JsonObject
        {
            ["issueDate"] = baseDate.ToString("o"),
            ["updateApplicationDate"] = baseDate.ToString("o"),
            ["updateNumber"] = productRequest.ProductName.StartsWith("101") ? productRequest.UpdateNumber +1 :0
        }
    };

            if (productRequest.ProductName.StartsWith("101"))
            {
                var additionalUpdateCount = 3;

                var updates = Enumerable.Range(productRequest.UpdateNumber + 2, additionalUpdateCount)
                    .Select(i =>
                    {
                        var currentDate = baseDate.AddDays(i * 5);
                        updateNumbersArrayS101.Add(i);
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
                ["productName"] = productRequest.ProductName,
                ["updateNumbers"] = productRequest.ProductName.StartsWith("101") ? updateNumbersArrayS101 : updateNumbersArray,
                ["dates"] = datesArray
            };

            // 30% chance to add cancellation
            if (cancelled)
            {
                var updateNumber = productRequest.ProductName.StartsWith("101") ? updateNumbersArrayS101.Max(node => node.GetValue<int>()) : updateNumbersArray.Max(node => node.GetValue<int>());

                productObj["cancellation"] = new JsonObject
                {
                    ["editionNumber"] = 0,
                    ["updateNumber"] = updateNumber
                };
            }

            productObj["fileSize"] = cancelled ? 0 : fileSize;

            return productObj;
        }

        private static async Task<List<ProductVersionRequest>> ExtractProductNamesFromRequestAsync(HttpRequest request)
        {
            request.EnableBuffering();

            using var reader = new StreamReader(request.Body, leaveOpen: true);
            var requestBody = await reader.ReadToEndAsync();
            request.Body.Position = 0;

            using var doc = JsonDocument.Parse(requestBody);
            var requestedProducts = new List<ProductVersionRequest>(doc.RootElement.GetArrayLength());

            foreach (var element in doc.RootElement.EnumerateArray())
            {
                var productRequest = new ProductVersionRequest();

                if (element.TryGetProperty("productName", out var productNameProperty) &&
                    productNameProperty.ValueKind == JsonValueKind.String)
                {
                    var productName = productNameProperty.GetString();
                    if (!string.IsNullOrEmpty(productName))
                    {
                        productRequest.ProductName = productName;
                    }
                }

                if (element.TryGetProperty("editionNumber", out var editionProperty) &&
                    editionProperty.ValueKind == JsonValueKind.Number)
                {
                    productRequest.EditionNumber = editionProperty.GetInt32();
                }

                if (element.TryGetProperty("updateNumber", out var updateNumberProperty) &&
                    updateNumberProperty.ValueKind == JsonValueKind.Number)
                {
                    productRequest.UpdateNumber = updateNumberProperty.GetInt32();
                }

                if (!string.IsNullOrEmpty(productRequest.ProductName))
                {
                    requestedProducts.Add(productRequest);
                }
            }

            return requestedProducts;
        }

        private static JsonObject CreateProductsVersionsNotReturnedObject(ProductVersionRequest productRequest, string reason)
        {
            return new JsonObject
            {
                ["productName"] = productRequest.ProductName,
                ["reason"] = reason
            };
        }
    }
}
