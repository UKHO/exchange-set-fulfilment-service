using System.Text.Json;
using System.Text.Json.Nodes;
using UKHO.ADDS.Infrastructure.Serialization.Json;

namespace UKHO.ADDS.Mocks.EFS.Override.Mocks.scs.ResponseGenerator
{
    public class ScsResponseGenerator
    {
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

                // Generate the response directly as JsonObject
                var response = GenerateProductNamesResponse(requestedProducts);

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

            try
            {
                if (requestBody.TrimStart().StartsWith("["))
                {
                    var productsArray = JsonCodec.Decode<JsonElement>(requestBody);

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

        private static JsonObject GenerateProductNamesResponse(List<string> requestedProducts)
        {
            var productsArray = new JsonArray();

            foreach (var productName in requestedProducts)
            {
                productsArray.Add(GenerateProductJson(productName));
            }

            var notReturnedArray = new JsonArray();

            return new JsonObject
            {
                ["productCounts"] = new JsonObject
                {
                    ["requestedProductCount"] = requestedProducts.Count,
                    ["returnedProductCount"] = requestedProducts.Count,
                    ["requestedProductsAlreadyUpToDateCount"] = 0,
                    ["requestedProductsNotReturned"] = notReturnedArray
                },
                ["products"] = productsArray
            };
        }

        private static JsonObject GenerateProductJson(string productName)
        {
            var random = Random.Shared;
            var editionNumber = random.Next(1, 15);
            var fileSize = random.Next(2000, 15000);
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
                var currentDate = baseDate.AddDays(5);

                updateNumbersArray.Add(1);
                datesArray.Add(new JsonObject
                {
                    ["issueDate"] = currentDate.ToString("o"),
                    ["updateNumber"] = 1
                });

                var additionalUpdateCount = random.Next(0, 4);

                for (var i = 2; i <= 1 + additionalUpdateCount; i++)
                {
                    updateNumbersArray.Add(i);
                    currentDate = currentDate.AddDays(5);
                    datesArray.Add(new JsonObject
                    {
                        ["issueDate"] = currentDate.ToString("o"),
                        ["updateNumber"] = i
                    });
                }
            }

            var productObj = new JsonObject
            {
                ["editionNumber"] = editionNumber,
                ["productName"] = productName,
                ["updateNumbers"] = updateNumbersArray,
                ["dates"] = datesArray,
            };

            if (random.Next(0, 10) < 3)
            {
                var updateNumber = 0;
                if (updateNumbersArray.Count > 0)
                {
                    updateNumber = updateNumbersArray.Max(node => node.GetValue<int>());
                }

                productObj["cancellation"] = new JsonObject
                {
                    ["editionNumber"] = 0,
                    ["updateNumber"] = updateNumber
                };
            }

            productObj["fileSize"] = fileSize;

            return productObj;
        }
    }
}
