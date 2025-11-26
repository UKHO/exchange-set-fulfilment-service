using System.Text.Json.Nodes;
using UKHO.ADDS.Mocks.EFS.Override.Mocks.fss.Enums;
using UKHO.ADDS.Mocks.EFS.Override.Mocks.fss.Models;
using UKHO.ADDS.Mocks.Headers;

namespace UKHO.ADDS.Mocks.EFS.Override.Mocks.fss.ResponseGenerator
{
    public static class FssResponseGenerator
    {
        private static readonly Random _random = new Random();

        private static readonly string _template =
            """
            {
              "count": 0,
              "total": 0,
              "entries": [],
              "_links": {    
              }
            }
            """;

        public static IResult ProvideSearchFilterResponse(HttpRequest requestMessage)
        {
            try
            {
                var jsonTemplate = JsonNode.Parse(_template)?.AsObject();
                var filter = requestMessage.Query["$filter"].FirstOrDefault();
                if (string.IsNullOrEmpty(filter))
                {
                    return CreateBadRequestResponse(requestMessage, "Missing or invalid $filter parameter.");
                }

                var batchDetails = BatchQueryParser.ParseBatchQuery("$filter=" + filter);

                UpdateResponseTemplate(jsonTemplate!, batchDetails);
                return Results.Ok(jsonTemplate);
            }
            catch (Exception)
            {
                return Results.InternalServerError("Error occurred while processing Batch Search request");
            }
        }

        private static void UpdateResponseTemplate(JsonObject jsonTemplate, FSSSearchFilterDetails filterDetails)
        {
            var entries = new JsonArray();

            if (!string.IsNullOrEmpty(filterDetails.BusinessUnit) && !string.IsNullOrEmpty(filterDetails.ProductCode) && filterDetails.Products.Count == 0)
            {
                // Special case for empty products
                var batchId = Guid.NewGuid().ToString();
                entries.Add(new JsonObject
                {
                    ["batchId"] = batchId,
                    ["status"] = "Committed",
                    ["allFilesZipSize"] = null,
                    ["attributes"] = new JsonArray { CreateAttribute("Product Type", filterDetails.ProductCode), CreateAttribute("Media Type", "Zip") },
                    ["businessUnit"] = filterDetails.BusinessUnit,
                    ["batchPublishedDate"] = DateTime.UtcNow.AddDays(-1).ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                    ["expiryDate"] = null, // Empty expiryDate
                    ["isAllFilesZipAvailable"] = true,
                    ["files"] = CreateFilesArray(batchId, "", 0, 0, true) // Pass true to indicate empty products case
                });
            }

            else
            {
                foreach (var product in filterDetails.Products)
                {
                    product.UpdateNumbers?.ForEach(updateNumber =>
                    {
                        var batchId = Guid.NewGuid().ToString();
                        entries.Add(new JsonObject
                        {
                            ["batchId"] = batchId,
                            ["status"] = "Committed",
                            ["allFilesZipSize"] = null,
                            ["attributes"] = new JsonArray { CreateAttribute("Product Name", product.ProductName), CreateAttribute("Edition Number", product.EditionNumber.ToString()), CreateAttribute("Update Number", updateNumber.ToString()), CreateAttribute("Product Type", filterDetails.ProductCode) },
                            ["businessUnit"] = filterDetails.BusinessUnit,
                            ["batchPublishedDate"] = DateTime.UtcNow.AddMonths(-2).ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                            ["expiryDate"] = DateTime.UtcNow.AddMonths(2).ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                            ["isAllFilesZipAvailable"] = true,
                            ["files"] = CreateFilesArray(batchId, product.ProductName, product.EditionNumber, updateNumber, false)
                        });
                    });
                }
            }

            jsonTemplate["count"] = entries.Count;
            jsonTemplate["total"] = entries.Count;
            jsonTemplate["entries"] = entries;
            jsonTemplate["_links"] = CreateLinkObject(filterDetails.ProductCode, filterDetails.Products.FirstOrDefault());
        }

        private static JsonObject CreateAttribute(string attr, object value) =>
            new() { ["key"] = attr, ["value"] = JsonValue.Create(value) };

        private static JsonArray CreateFiles(string productName, string batchId, IEnumerable<string> extensions, int editionNumber = 0, int updateNumber = 0)
        {
            var array = new JsonArray();
            foreach (var ext in extensions)
            {
                var fileSize = _random.Next(800, 2000); // Random file size between 800 and 2000
                array.Add(CreateFileObject(productName, ext, fileSize, batchId, editionNumber, updateNumber));
            }
            return array;
        }

        private static JsonArray CreateFilesArray(string batchId, string productName, int editionNumber = 0, int updateNumber = 0, bool isEmptyProducts = false)
        {
            if (isEmptyProducts)
            {
                // Special case for empty products - include S_100ExchangeSet.zip
                var array = new JsonArray();
                var fileSize = _random.Next(800, 2000);
                array.Add(CreateFileObject("S_100ExchangeSet", ".zip", fileSize, batchId));
                return array;
            }

            // Existing logic for non-empty products
            IEnumerable<string> extensions = productName switch
            {
                var name when name.StartsWith(((int)ProductIdentifiers.s101).ToString()) => new[] { ".zip" },
                var name when name.StartsWith(((int)ProductIdentifiers.s102).ToString()) => new[] { ".zip" },
                var name when name.StartsWith(((int)ProductIdentifiers.s104).ToString()) => new[] { ".zip" },
                var name when name.StartsWith(((int)ProductIdentifiers.s111).ToString()) => new[] { ".zip" }
            };

            return CreateFiles(productName, batchId, extensions, editionNumber, updateNumber);
        }

        private static JsonObject CreateFileObject(string productName, string extension, int fileSize, string batchId, int editionNumber = 0, int updateNumber = 0)
        {
            var filename = $"{productName}_{editionNumber}_{updateNumber}{extension}";
            
            return new()
            {
                ["filename"] = filename,
                ["fileSize"] = fileSize,
                ["mimeType"] = "text/plain",
                ["hash"] = string.Empty,
                ["attributes"] = new JsonArray(),
                ["links"] = new JsonObject { ["get"] = new JsonObject { ["href"] = $"/batch/{batchId}/files/{filename}" } }
            };
        }

        private static JsonObject CreateLinkObject(string productCode, Product product)
        {
            var filterValue = !string.IsNullOrEmpty(product?.ProductName)
                ? $"$batch(Product Type) eq '{productCode}' and $batch(Product Name) eq '{product.ProductName}' and $batch(Edition Number) eq '{product.EditionNumber}' and $batch(Update Number) eq '{product.UpdateNumbers.FirstOrDefault()}'"
                : $"$batch(Product Type) eq '{productCode}'";

            var encodedFilterUrl = $"/batch?limit=10&start=0&$filter={Uri.EscapeDataString(filterValue)}";

            return new JsonObject
            {
                ["self"] = new JsonObject { ["href"] = encodedFilterUrl },
                ["first"] = new JsonObject { ["href"] = encodedFilterUrl },
                ["last"] = new JsonObject { ["href"] = encodedFilterUrl }
            };
        }

        private static IResult CreateBadRequestResponse(HttpRequest requestMessage, string description)
        {
            return Results.BadRequest(new
            {
                correlationId = requestMessage.Headers[WellKnownHeader.CorrelationId],
                errors = new[]
                {
                    new { source = "Search Batch", description }
                }
            });
        }
    }
}
