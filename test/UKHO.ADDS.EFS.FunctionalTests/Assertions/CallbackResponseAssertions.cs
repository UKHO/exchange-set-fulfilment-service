using System.Text.Json;
using FluentAssertions;
using UKHO.ADDS.EFS.FunctionalTests.Diagnostics;

namespace UKHO.ADDS.EFS.FunctionalTests.Assertions
{
    public class  CallbackResponseAssertions
    {
        /// <summary>
        /// Compares a JSON response from an API call with the content of a reference file,
        /// focusing on the "data" section and its key properties.
        /// Uses soft assertions to report all validation issues at once.
        /// </summary>
        /// <param name="sourceResponseFromPostApiCall">The JSON response string from the API call</param>
        /// <param name="targetTextFile">Path to the reference file containing expected structure</param>
        public static void CompareCallbackResponse(string sourceResponseFromPostApiCall, string targetTextFile)
        {
            TestOutput.WriteLine($"Source response:\n{sourceResponseFromPostApiCall}");

            // Parse the source response
            var sourceJson = JsonDocument.Parse(sourceResponseFromPostApiCall);

            // Read and parse the target file
            var targetFileContent = File.ReadAllText(targetTextFile);
            TestOutput.WriteLine($"Target file content:\n{targetFileContent}");

            var targetJson = JsonDocument.Parse(targetFileContent);

            // Extract the "data" section from the target file
            if (!targetJson.RootElement.TryGetProperty("data", out var targetData))
            {
                throw new InvalidOperationException("Target file does not contain 'data' property");
            }

            // Convert both to strings with consistent formatting for comparison
            var options = new JsonSerializerOptions { WriteIndented = true };
            var formattedSource = JsonSerializer.Serialize(sourceJson, options);
            var formattedTarget = JsonSerializer.Serialize(targetData, options);

            TestOutput.WriteLine($"Formatted source:\n{formattedSource}");
            TestOutput.WriteLine($"Formatted target data section:\n{formattedTarget}");

            JsonElement sourceData = sourceJson.RootElement;

            // Compare key properties in the root
            targetJson.RootElement.TryGetProperty("type", out var targetTypeElement);
            targetTypeElement.GetString()?.TrimEnd('$')
                .Should().Be("uk.co.admiralty.s100Data.exchangeSetCreated.v1",
                    $"Values for property type should match");

            targetJson.RootElement.TryGetProperty("source", out var targetSourceElement);
            targetSourceElement.GetString()?.TrimEnd('$')
                .Should().Be("https://exchangeset.admiralty.co.uk/s100Data",
                    $"Values for property type should match");

            // Compare key properties one by one to provide better error messages
            CompareJsonProperty(sourceData, targetData, "requestedProductCount");
            CompareJsonProperty(sourceData, targetData, "exchangeSetProductCount");
            CompareJsonProperty(sourceData, targetData, "requestedProductsAlreadyUpToDateCount");
            CompareJsonProperty(sourceData, targetData, "fssBatchId");
            CompareJsonProperty(sourceData, targetData, "exchangeSetUrlExpiryDateTime");

            // Compare the arrays (special handling for empty arrays)
            CompareJsonArrays(sourceJson.RootElement, targetData, "requestedProductsNotInExchangeSet");

            // Compare the links object structure
            CompareLinksStructure(sourceJson.RootElement, targetData);
        }

        private static void CompareJsonProperty(JsonElement source, JsonElement target, string propertyName)
        {
            if (target.TryGetProperty(propertyName, out var targetValue))
            {
                source.TryGetProperty(propertyName, out var sourceValue).Should().BeTrue(
                    $"Source should contain '{propertyName}' property");

                // Remove any trailing $ characters if present (based on your examples)
                var sourceValueString = sourceValue.ToString().TrimEnd('$');
                var targetValueString = targetValue.ToString().TrimEnd('$');

                targetValueString.Should().Be(sourceValueString,
                    $"Values for property '{propertyName}' should match");
            }
        }

        private static void CompareJsonArrays(JsonElement source, JsonElement target, string propertyName)
        {
            // First verify source has the array property we're validating
            source.TryGetProperty(propertyName, out var sourceArray).Should().BeTrue(
                $"Source should contain '{propertyName}' property");

            // Then verify target has the array property
            target.TryGetProperty(propertyName, out var targetArray).Should().BeTrue(
                $"Target should contain '{propertyName}' property");

            // Compare array length
            targetArray.GetArrayLength().Should().Be(sourceArray.GetArrayLength(),
                $"Target array '{propertyName}' should have same length as source ({sourceArray.GetArrayLength()})");

            TestOutput.WriteLine($"Array '{propertyName}' has {sourceArray.GetArrayLength()} items");

            // If not empty, compare contents item by item
            if (sourceArray.GetArrayLength() > 0)
            {
                for (int i = 0; i < sourceArray.GetArrayLength(); i++)
                {
                    var sourceItem = sourceArray[i];
                    var targetItem = targetArray[i];

                    TestOutput.WriteLine($"Comparing item {i}:");
                    TestOutput.WriteLine($"  Source: {sourceItem}");
                    TestOutput.WriteLine($"  Target: {targetItem}");

                    // Get all source item property names to verify target has all expected properties
                    var sourceProps = sourceItem.EnumerateObject().Select(p => p.Name).ToList();
                    var targetProps = targetItem.EnumerateObject().Select(p => p.Name).ToList();

                    // Verify target has all expected properties and no extras
                    targetProps.Should().BeEquivalentTo(sourceProps,
                        $"Item {i} should have the same properties as the source");

                    // Compare each property value from source to ensure it's in target with correct value
                    foreach (var prop in sourceItem.EnumerateObject())
                    {
                        targetItem.TryGetProperty(prop.Name, out var targetProp).Should().BeTrue(
                            $"Target item {i} should have '{prop.Name}' property");

                        var sourceValueString = prop.Value.ToString().TrimEnd('$');
                        var targetValueString = targetProp.ToString().TrimEnd('$');

                        targetValueString.Should().Be(sourceValueString,
                            $"Value for '{prop.Name}' in item {i} should match source value");
                    }
                }
            }
            else
            {
                TestOutput.WriteLine($"Array '{propertyName}' is empty in both source and target");
            }
        }

        private static void CompareLinksStructure(JsonElement source, JsonElement target)
        {
            // First verify source has links section to use as our reference
            source.TryGetProperty("links", out var sourceLinks).Should().BeTrue(
                "Source response should contain 'links' section");

            // Get all source link types we expect to find in the target
            var sourceLinkTypes = new List<string>();
            foreach (var sourceLink in sourceLinks.EnumerateObject())
            {
                sourceLinkTypes.Add(sourceLink.Name);
            }

            // Log source link types for debugging
            TestOutput.WriteLine($"Source contains {sourceLinkTypes.Count} link types: {string.Join(", ", sourceLinkTypes)}");

            // Then verify target has links section
            target.TryGetProperty("links", out var targetLinks).Should().BeTrue(
                "Target should contain 'links' section");

            foreach (var linkName in sourceLinkTypes)
            {
                // Verify target contains this link type
                targetLinks.TryGetProperty(linkName, out var targetLink)
                    .Should().BeTrue($"Target should contain link type '{linkName}'");

                // Get source URI for comparison
                sourceLinks.TryGetProperty(linkName, out var sourceLink).Should().BeTrue();
                sourceLink.TryGetProperty("uri", out var sourceUri).Should().BeTrue();

                // Verify target has URI property that matches source
                targetLink.TryGetProperty("uri", out var targetUri)
                    .Should().BeTrue($"Target link '{linkName}' should have 'uri' property");

                // Compare URI values (removing any trailing $ characters)
                var sourceUriValue = sourceUri.GetString()?.TrimEnd('$');
                var targetUriValue = targetUri.GetString()?.TrimEnd('$');

                targetUriValue.Should().Be(sourceUriValue,
                    $"URI for '{linkName}' should match source value");
            }

            // Also check that target doesn't have extra link types not in source
            foreach (var targetLink in targetLinks.EnumerateObject())
            {
                sourceLinkTypes.Should().Contain(targetLink.Name,
                    $"Target contains unexpected link type '{targetLink.Name}'");
            }
        }
    }
}
