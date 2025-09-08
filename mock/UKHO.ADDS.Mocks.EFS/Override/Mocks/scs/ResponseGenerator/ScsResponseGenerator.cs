using System.Text.Json;
using System.Text.Json.Nodes;
using UKHO.ADDS.Mocks.Configuration.Mocks.scs.Helpers;
using UKHO.ADDS.Mocks.Files;
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
        /// Provides a mock response for updates since a specified date using data from s100-updates-since.json file.
        /// </summary>
        public static async Task<IResult> ProvideUpdatesSinceResponse(DateTime sinceDateTime, string? productIdentifier, HttpRequest request, IMockFile file)
        {
            try
            {
                var now = DateTime.UtcNow;
                var twentyEightDaysAgo = now.AddDays(-28);

                if (sinceDateTime < twentyEightDaysAgo)
                {
                    return ResponseHelper.CreateBadRequestResponse(request, "sinceDateTime", "Date provided is more than 28 days in the past.");
                }

                if (sinceDateTime > now)
                {
                    return Results.StatusCode(304);
                }

                if (!string.IsNullOrWhiteSpace(productIdentifier))
                {
                    var validIdentifiers = new[] { "s101", "s102", "s104", "s111" };
                    if (!validIdentifiers.Contains(productIdentifier.ToLowerInvariant()))
                    {
                        return ResponseHelper.CreateNotFoundResponse(request, "Product identifier not found");
                    }
                }

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
                return (CreateBadRequestResponse(request, "Request body is required"), requestedProducts);

            try
            {
                using var doc = JsonDocument.Parse(requestBody);
                if (doc.RootElement.ValueKind != JsonValueKind.Array)
                    return (CreateBadRequestResponse(request, "Request body must be a JSON array of product names."), requestedProducts);

                foreach (var element in doc.RootElement.EnumerateArray())
                {
                    if (element.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(element.GetString()))
                        return (CreateBadRequestResponse(request, "All items in the array must be non-empty strings."), requestedProducts);

                    requestedProducts.Add(element.GetString()!);
                }

                if (!requestedProducts.Any())
                    return (CreateBadRequestResponse(request, "Empty product name is not allowed."), requestedProducts);

                return (null, requestedProducts);
            }
            catch (JsonException)
            {
                return (CreateBadRequestResponse(request, "Invalid JSON format."), requestedProducts);
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
            // Load and parse JSON data from file
            var allProducts = await LoadProductsFromFileAsync(file);

            // Filter products by identifier if specified
            var filteredProducts = FilterProductsByIdentifier(allProducts, productIdentifier);

            // Build response array
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

            return prefix is not null
                ? allProducts.Where(p => p?["productName"]?.GetValue<string>()?.StartsWith(prefix) == true).ToList()
                : new List<JsonNode>();
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

            foreach (var product in filteredProducts.Where(p => p is not null))
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

        private static IResult CreateBadRequestResponse(HttpRequest requestMessage, string description)
        {
            return ResponseHelper.CreateBadRequestResponse(requestMessage, "Product Names", description);
        }
    }
}
