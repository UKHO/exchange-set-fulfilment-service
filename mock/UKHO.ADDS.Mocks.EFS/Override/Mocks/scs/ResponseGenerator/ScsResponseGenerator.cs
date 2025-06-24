using System.Text.Json;
using System.Text.Json.Nodes;
using UKHO.ADDS.Mocks.EFS.Override.Mocks.scs.Models;

namespace UKHO.ADDS.Mocks.EFS.Override.Mocks.scs.ResponseGenerator
{
    public class ScsResponseGenerator
    {
        private static readonly Random _random = new Random();

        public static IResult ProvideProductNamesResponse(HttpRequest requestMessage)
        {
            try
            {
                // Parse and validate the request
                var validationResult = ValidateRequest(requestMessage, out var requestedProducts);
                if (validationResult != null)
                {
                    return validationResult;
                }

                // Generate the response using the strongly-typed model
                var responseModel = GenerateProductNamesResponse(requestedProducts);

                // Convert to JsonObject for the API response
                var response = ConvertToJsonObject(responseModel);

                return Results.Ok(response);
            }
            catch (Exception ex)
            {
                return Results.Problem($"Error processing request: {ex.Message}");
            }
        }

        private static IResult ValidateRequest(HttpRequest request, out List<string> requestedProducts)
        {
            requestedProducts = new List<string>();

            string requestBody;
            using (var reader = new StreamReader(request.Body))
            {
                requestBody = reader.ReadToEndAsync().Result;
            }

            if (string.IsNullOrEmpty(requestBody))
            {
                return Results.BadRequest("Request body is required");
            }

            // Accepts request body as a JSON array of strings (e.g., ["101GB007645NUTS58","101GB007645NUTS57"])
            try
            {
                if (requestBody.TrimStart().StartsWith("["))
                {
                    var productsArray = JsonSerializer.Deserialize<JsonElement>(requestBody);
                    if (productsArray.ValueKind != JsonValueKind.Array)
                    {
                        return Results.BadRequest("Request body must be a JSON array of product names.");
                    }

                    foreach (var product in productsArray.EnumerateArray())
                    {
                        if (product.ValueKind == JsonValueKind.String)
                        {
                            requestedProducts.Add(product.GetString());
                        }
                        else
                        {
                            return Results.BadRequest("All items in the array must be strings.");
                        }
                    }

                    return null;
                }
            }
            catch (JsonException)
            {
                return Results.BadRequest("Invalid JSON format.");
            }

            return null;
        }

        private static ProductNamesResponse GenerateProductNamesResponse(List<string> requestedProducts)
        {
            var products = new List<Product>();

            foreach (var productName in requestedProducts)
            {
                products.Add(GenerateProduct(productName));
            }

            return new ProductNamesResponse
            {
                ProductCounts = new ProductCounts
                {
                    RequestedProductCount = requestedProducts.Count,
                    ReturnedProductCount = requestedProducts.Count,
                    RequestedProductsAlreadyUpToDateCount = 0,
                    RequestedProductsNotReturned = new List<string>()
                },
                Products = products
            };
        }

        private static Product GenerateProduct(string productName)
        {
            var editionNumber = _random.Next(1, 15);
            var fileSize = _random.Next(2000, 15000);

            // Always include base update (0)
            var updateNumbers = new List<int> { 0 };
            var dates = new List<ProductDate>
                {
                    new ProductDate
                    {
                        IssueDate = DateTime.UtcNow,
                        UpdateApplicationDate = DateTime.UtcNow,
                        UpdateNumber = 0
                    }
                };

            // Optionally add more updates
            if (_random.Next(0, 2) == 1)
            {
                var updateCount = _random.Next(1, 3);
                for (var i = 1; i <= updateCount; i++)
                {
                    updateNumbers.Add(i);
                    dates.Add(new ProductDate
                    {
                        IssueDate = DateTime.UtcNow.AddDays(-30 + i * 10),
                        UpdateNumber = i
                    });
                }
            }

            return new Product
            {
                EditionNumber = editionNumber,
                ProductName = productName,
                UpdateNumbers = updateNumbers,
                Dates = dates,
                FileSize = fileSize
            };
        }

        private static JsonObject ConvertToJsonObject(ProductNamesResponse responseModel)
        {
            var productsArray = new JsonArray();

            foreach (var product in responseModel.Products)
            {
                var datesArray = new JsonArray();
                foreach (var date in product.Dates)
                {
                    var dateObj = new JsonObject
                    {
                        ["issueDate"] = date.IssueDate.ToString("o"),
                        ["updateNumber"] = date.UpdateNumber
                    };

                    if (date.UpdateApplicationDate.HasValue)
                    {
                        dateObj["updateApplicationDate"] = date.UpdateApplicationDate.Value.ToString("o");
                    }

                    datesArray.Add(dateObj);
                }

                var productObj = new JsonObject
                {
                    ["editionNumber"] = product.EditionNumber,
                    ["productName"] = product.ProductName,
                    ["updateNumbers"] = new JsonArray(product.UpdateNumbers.Select(n => JsonValue.Create(n)).ToArray()),
                    ["dates"] = datesArray,
                    ["fileSize"] = product.FileSize
                };

                productsArray.Add(productObj);
            }

            var notReturnedArray = new JsonArray();
            foreach (var item in responseModel.ProductCounts.RequestedProductsNotReturned)
            {
                notReturnedArray.Add(item);
            }

            return new JsonObject
            {
                ["productCounts"] = new JsonObject
                {
                    ["requestedProductCount"] = responseModel.ProductCounts.RequestedProductCount,
                    ["returnedProductCount"] = responseModel.ProductCounts.ReturnedProductCount,
                    ["requestedProductsAlreadyUpToDateCount"] = responseModel.ProductCounts.RequestedProductsAlreadyUpToDateCount,
                    ["requestedProductsNotReturned"] = notReturnedArray
                },
                ["products"] = productsArray
            };
        }
    }
}
