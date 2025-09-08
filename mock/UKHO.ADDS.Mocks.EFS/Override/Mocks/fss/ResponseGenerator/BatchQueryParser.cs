using System.Text.RegularExpressions;
using UKHO.ADDS.Mocks.EFS.Override.Mocks.fss.Models;

namespace UKHO.ADDS.Mocks.EFS.Override.Mocks.fss.ResponseGenerator
{
    public class BatchQueryParser
    {
        private const string BatchPattern = @"\$batch\((?<Property>\w+)\) eq '(?<Value>[^']*)'";
        private static readonly Regex _businessUnitRegex = new(@"BusinessUnit\s*eq\s*'([^']*)'", RegexOptions.Compiled);
        private static readonly Regex _productTypeRegex = new Regex(@"\$batch\(Product Code\) eq '(?<Value>[^']*)'", RegexOptions.Compiled);

        public static FSSSearchFilterDetails ParseBatchQuery(string odataQuery)
        {
            var filterDetails = new FSSSearchFilterDetails();

            var filterMatch = Regex.Match(odataQuery, @"\$filter=(.*)");
            if (!filterMatch.Success)
            {
                return filterDetails;
            }

            var filter = filterMatch.Groups[1].Value;
            ParseFilterExpression(filter, filterDetails);

            return filterDetails;
        }

        private static void ParseFilterExpression(string filter, FSSSearchFilterDetails filterDetails)
        {
            var businessUnitMatch = _businessUnitRegex.Match(filter);
            filterDetails.BusinessUnit = businessUnitMatch.Success ? businessUnitMatch.Groups[1].Value : string.Empty;

            var productTypeMatch = _productTypeRegex.Match(filter);
            filterDetails.ProductType = productTypeMatch.Success ? productTypeMatch.Groups[1].Value : string.Empty;

            var conditions = filter.Split(")))", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            foreach (var condition in conditions)
            {
                var product = ParseFilterProductProperties(condition);
                if (product != null)
                {
                    filterDetails.Products.Add(product);
                }
            }
        }

        private static Product? ParseFilterProductProperties(string filter)
        {
            var matches = Regex.Matches(filter, BatchPattern);

            if (matches.Count == 0)
            {
                return null;
            }

            var product = new Product();


            foreach (Match match in matches)
            {
                var property = match.Groups["Property"].Value;
                var value = match.Groups["Value"].Value;

                if (string.IsNullOrEmpty(property) || string.IsNullOrEmpty(value))
                {
                    continue;
                }

                switch (property)
                {
                    case "Product Name":
                        product.ProductName = value;
                        break;
                    case "Edition Number" when int.TryParse(value, out var editionNumber):
                        product.EditionNumber = editionNumber;
                        break;
                    case "Update Number" when int.TryParse(value, out var updateNumber):
                        product.UpdateNumbers.Add(updateNumber);
                        break;
                }
            }

            return product.UpdateNumbers.Count == 0 && string.IsNullOrEmpty(product.ProductName) 
                ? null
                : product;
        }
    }
}
