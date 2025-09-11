using System.Text.Json;
using System.Text.Json.Nodes;
using UKHO.ADDS.Mocks.Configuration.Mocks.scs.Helpers;
using UKHO.ADDS.Mocks.EFS.Override.Mocks.scs.Models;
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
                return (ResponseHelper.CreateBadRequestResponse(request, "Request Body", "Request body is required"), requestedProducts);

            try
            {
                using var doc = JsonDocument.Parse(requestBody);
                if (doc.RootElement.ValueKind != JsonValueKind.Array)
                    return (ResponseHelper.CreateBadRequestResponse(request, "Request Body", "Request body must be a JSON array of product names."), requestedProducts);

                foreach (var element in doc.RootElement.EnumerateArray())
                {
                    if (element.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(element.GetString()))
                        return (ResponseHelper.CreateBadRequestResponse(request, "Product Names", "All items in the array must be non-empty strings."), requestedProducts);

                    requestedProducts.Add(element.GetString()!);
                }

                if (!requestedProducts.Any())
                    return (ResponseHelper.CreateBadRequestResponse(request, "Product Names", "Empty product name is not allowed."), requestedProducts);

                return (null, requestedProducts);
            }
            catch (JsonException)
            {
                return (ResponseHelper.CreateBadRequestResponse(request, "JSON Format", "Invalid JSON format."), requestedProducts);
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
                    AddAllProductsAsNotReturned(requestedProducts, notReturnedArray, "invalidProduct");
                    break;

                case "get-invalidproducts" when productCount > 0:
                    ProcessProductsWithLastAsNotReturned(requestedProducts, productsArray, notReturnedArray, "invalidProduct");
                    break;

                case "get-productwithdrawn" when productCount > 0:
                    ProcessProductsWithLastAsNotReturned(requestedProducts, productsArray, notReturnedArray, "productWithdrawn");
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

        private static JsonObject CreateProductNotReturnedObject(string productName, string reason)
        {
            return new JsonObject
            {
                ["productName"] = productName,
                ["reason"] = reason
            };
        }
        private static JsonObject CreateProductsVersionsNotReturnedObject(ProductVersionRequest productRequest, string reason)
        {
            return new JsonObject
            {
                ["productName"] = productRequest.ProductName,
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
    }
}
