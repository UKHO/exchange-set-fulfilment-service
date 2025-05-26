using System.Text.Json.Nodes;
using UKHO.ADDS.Mocks.Configuration.Mocks.fss.ResponseGenerator;
using UKHO.ADDS.Mocks.EFS.Override.Mocks.fss.Enums;
using UKHO.ADDS.Mocks.SampleService.Override.Mocks.fss.Models;

namespace UKHO.ADDS.Mocks.SampleService.Override.Mocks.fss.ResponseGenerator
{
    public static class FssResponseGenerator
    {
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
                    return Results.BadRequest("Missing or invalid $filter parameter.");
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
                        ["attributes"] = new JsonArray { CreateAttribute("ProductName", product.ProductName), CreateAttribute("EditionNumber", product.EditionNumber.ToString()), CreateAttribute("UpdateNumber", updateNumber.ToString()), CreateAttribute("ProductType", filterDetails.ProductType) },
                        ["businessUnit"] = filterDetails.BusinessUnit,
                        ["batchPublishedDate"] = DateTime.UtcNow.AddMonths(-2).ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                        ["expiryDate"] = DateTime.UtcNow.AddMonths(2).ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                        ["isAllFilesZipAvailable"] = true,
                        ["files"] = CreateFilesArray(product.ProductName, batchId, updateNumber.ToString())
                    });
                });
            }

            jsonTemplate["count"] = entries.Count;
            jsonTemplate["total"] = entries.Count;
            jsonTemplate["entries"] = entries;
            jsonTemplate["_links"] = CreateLinkObject(filterDetails.ProductType, filterDetails.Products.FirstOrDefault());
        }

        private static JsonObject CreateAttribute(string attr, object value) =>
            new() { ["key"] = attr, ["value"] = JsonValue.Create(value) };      

        private static JsonArray CreateFiles(string productName, string batchId, IEnumerable<string> extensions)
        {
            var array = new JsonArray();
            var random = new Random();
            foreach (var ext in extensions)
            {
                int fileSize = random.Next(800, 2000); // Random file size between 800 and 2000
                array.Add(CreateFileObject(productName, ext, fileSize, batchId));
            }
            return array;
        }

        private static JsonArray CreateFilesArray(string productName, string batchId, string updateNo)
        {           
            IEnumerable<string> extensions = productName switch
            {
                var name when name.StartsWith(((int)ProductCodes.P101).ToString()) => new[] { $".{updateNo.PadLeft(3, '0')}", ".TXT", ".TIF", ".IMG" },
                var name when name.StartsWith(((int)ProductCodes.P102).ToString()) => new[] { ".h5", ".TXT", ".TIF", ".IMG" },
                var name when name.StartsWith(((int)ProductCodes.P104).ToString()) => new[] { ".h5", ".TXT", ".TIF", ".IMG" },
                var name when name.StartsWith(((int)ProductCodes.P111).ToString()) => new[] { ".h5", ".TXT", ".TIF", ".TIFF", ".IMG" }               
            };

            return CreateFiles(productName, batchId, extensions);
        }

        private static JsonObject CreateFileObject(string productName, string extension, int fileSize, string batchId) =>
            new()
            {
                ["filename"] = $"{productName}{extension}",
                ["fileSize"] = fileSize,
                ["mimeType"] = "text/plain",
                ["hash"] = string.Empty,
                ["attributes"] = new JsonArray(),
                ["links"] = new JsonObject { ["get"] = new JsonObject { ["href"] = $"/batch/{batchId}/files/{productName}{extension}" } }
            };

        private static JsonObject CreateLinkObject(string productType, Product product)
        {
            var filterValue = !string.IsNullOrEmpty(product?.ProductName)
                ? $"$batch(ProductType) eq '{productType}' and $batch(ProductName) eq '{product.ProductName}' and $batch(EditionNumber) eq '{product.EditionNumber}' and $batch(UpdateNumber) eq '{product.UpdateNumbers.FirstOrDefault()}'"
                : $"$batch(ProductType) eq '{productType}'";

            var encodedFilterUrl = $"/batch?limit=10&start=0&$filter={Uri.EscapeDataString(filterValue)}";

            return new JsonObject
            {
                ["self"] = new JsonObject { ["href"] = encodedFilterUrl },
                ["first"] = new JsonObject { ["href"] = encodedFilterUrl },
                ["last"] = new JsonObject { ["href"] = encodedFilterUrl }
            };
        }
    }
}
