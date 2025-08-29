using System.Text.Json;
using System.Text.Json.Nodes;
using UKHO.ADDS.EFS.Domain.Products;
using Xunit;
using Assert = Xunit.Assert;

namespace UKHO.ADDS.EFS.Domain.UnitTests.Implementation.Serialization
{
    public sealed class ProductNameListJsonConverterTests
    {
        private static JsonSerializerOptions CreateOptionsIndented() => CreateOptions(writeIndented: true);

        private static JsonSerializerOptions CreateOptions(bool writeIndented = false)
        {
            var o = new JsonSerializerOptions
            {
                WriteIndented = writeIndented
            };

            return o;
        }

        private static ProductNameList MakeList(params string[] items)
        {
            var list = new ProductNameList();
            foreach (var s in items)
                list.Add(ProductName.From(s));
            return list;
        }

        private static string[] Names(ProductNameList list)
            => list.Names.Select(n => (string)n).ToArray();

        private sealed class Container
        {
            public ProductNameList? Names { get; set; }
        }

        private sealed class ContainerNonNullable
        {
            public ProductNameList Names { get; set; } = new();
        }

        [Fact]
        public void Serialize_NullWrapper_WritesJsonNull()
        {
            ProductNameList? list = null;
            var json = JsonSerializer.Serialize(list, CreateOptions());
            Assert.Equal("null", json);
        }

        [Fact]
        public void Deserialize_NullLiteral_ToNullableWrapper_ReturnsNull()
        {
            var json = "null";
            var result = JsonSerializer.Deserialize<ProductNameList?>(json, CreateOptions());

            Assert.Null(result);
        }

        [Fact]
        public void Serialize_EmptyList_WritesEmptyArray()
        {
            var list = new ProductNameList(); // no items
            var json = JsonSerializer.Serialize(list, CreateOptions());

            Assert.Equal("[]", json);
        }

        [Fact]
        public void Deserialize_EmptyArray_ProducesEmptyWrapper()
        {
            var list = JsonSerializer.Deserialize<ProductNameList>("[]", CreateOptions());

            Assert.NotNull(list);
            Assert.Empty(list!.Names);
        }

        [Fact]
        public void RoundTrip_TwoItems_PreservesOrder_AndValues()
        {
            var options = CreateOptions();
            var original = MakeList("prod1", "prod2");

            var json = JsonSerializer.Serialize(original, options);
            
            Assert.Equal(@"[""prod1"",""prod2""]", json);

            var roundTripped = JsonSerializer.Deserialize<ProductNameList>(json, options);

            Assert.NotNull(roundTripped);
            Assert.Equal(new[] { "prod1", "prod2" }, Names(roundTripped!));
        }

        [Fact]
        public void Deserialize_LegacyObjectShape_WithArrayProperty_StillWorks()
        {
            // The converter accepts an object and picks the first array property.
            var json = /*lang=json*/ @"{ ""names"": [""n1"", ""n2""] }";
            var list = JsonSerializer.Deserialize<ProductNameList>(json, CreateOptions());
            Assert.NotNull(list);
            Assert.Equal(new[] { "n1", "n2" }, Names(list!));
        }

        [Fact]
        public void Deserialize_ObjectWithDifferentArrayPropertyName_StillPicksFirstArray()
        {
            // Converter scans object properties and uses the first array it finds.
            var json = /*lang=json*/ @"{ ""whatever"": [""a"",""b"",""c""], ""names"": [""ignored""] }";
            var list = JsonSerializer.Deserialize<ProductNameList>(json, CreateOptions());
            Assert.NotNull(list);
            Assert.Equal(new[] { "a", "b", "c" }, Names(list!));
        }

        [Fact]
        public void Deserialize_ObjectWithNoArrayProperties_ProducesEmptyWrapper()
        {
            var json = /*lang=json*/ @"{ ""foo"": 123, ""bar"": ""baz"" }";
            var list = JsonSerializer.Deserialize<ProductNameList>(json, CreateOptions());
            Assert.NotNull(list);
            Assert.Empty(list!.Names);
        }

        [Fact]
        public void Serialize_InsideContainer_WritesArray_NotNull()
        {
            var options = CreateOptions();
            var container = new ContainerNonNullable
            {
                Names = MakeList("p1", "p2")
            };

            var json = JsonSerializer.Serialize(container, options);
            // Parse to be tolerant of formatting
            var node = JsonNode.Parse(json)!.AsObject();
            Assert.True(node.TryGetPropertyValue("Names", out var namesNode));
            Assert.Equal(JsonValueKind.Array, namesNode!.GetValue<JsonElement>().ValueKind);

            var arr = namesNode!.AsArray();
            Assert.Equal(2, arr.Count);
            Assert.Equal("p1", arr[0]!.GetValue<string>());
            Assert.Equal("p2", arr[1]!.GetValue<string>());
        }

        [Fact]
        public void Deserialize_InsideContainer_WithNullProperty_AllowsNullable()
        {
            var json = /*lang=json*/ @"{ ""Names"": null }";
            var c = JsonSerializer.Deserialize<Container>(json, CreateOptions());
            Assert.NotNull(c);
            Assert.Null(c!.Names);
        }

        [Fact]
        public void Deserialize_InsideContainer_WithArrayProperty_BindsToWrapper()
        {
            var json = /*lang=json*/ @"{ ""Names"": [""alpha"", ""beta""] }";
            var c = JsonSerializer.Deserialize<Container>(json, CreateOptions());
            Assert.NotNull(c);
            Assert.NotNull(c!.Names);
            Assert.Equal(new[] { "alpha", "beta" }, Names(c.Names!));
        }

        [Fact]
        public void Serialize_WithSpecialCharacters_EscapesCorrectly()
        {
            var options = CreateOptionsIndented();
            var list = MakeList(@"p""q", @"r\ns", @"t\u2028u");

            var json = JsonSerializer.Serialize(list, options);
            var node = JsonNode.Parse(json)!.AsArray();

            Assert.Equal(3, node.Count);
            // Expect plain JSON strings; round-trip to verify content, not escape sequences
            var back = JsonSerializer.Deserialize<ProductNameList>(json, CreateOptions())!;
            Assert.Equal(new[] { @"p""q", @"r\ns", "t\u2028u" }, Names(back));
        }
    }
}
