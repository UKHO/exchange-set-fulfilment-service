using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Nodes;
using UKHO.ADDS.EFS.Domain.Products;
using Xunit;
using Assert = Xunit.Assert;

namespace UKHO.ADDS.EFS.Domain.UnitTests.Implementation.Serialization
{
    public sealed class JsonListConverterTests
    {
        private static JsonSerializerOptions CreateOptions(bool indented = false) => new()
        {
            WriteIndented = indented
        };

        private static ProductNameList MakeList(params string[] items)
        {
            var list = new ProductNameList();
            foreach (var s in items)
            {
                list.Add(ProductName.From(s));
            }

            return list;
        }

        private static string[] Names(ProductNameList list) => list.Names.Select(n => n.ToString()).ToArray();

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
            var result = JsonSerializer.Deserialize<ProductNameList?>("null", CreateOptions());

            Assert.Null(result);
        }

        [Fact]
        public void Serialize_EmptyList_WritesEmptyArray()
        {
            var list = new ProductNameList();
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
        public void RoundTrip_TwoItems_PreservesOrder_AndUppercases()
        {
            var opts = CreateOptions();
            var original = MakeList("101-prod1", "102-prod2"); // ctor uppercases

            var json = JsonSerializer.Serialize(original, opts);

            using var doc = JsonDocument.Parse(json);

            Assert.Equal(JsonValueKind.Array, doc.RootElement.ValueKind);
            Assert.Equal(2, doc.RootElement.GetArrayLength());
            Assert.Equal("101-PROD1", doc.RootElement[0].GetString());
            Assert.Equal("102-PROD2", doc.RootElement[1].GetString());

            var roundTripped = JsonSerializer.Deserialize<ProductNameList>(json, opts)!;

            Assert.Equal(new[]
            {
                "101-PROD1", "102-PROD2"
            }, Names(roundTripped));
        }

        [Fact]
        public void Deserialize_MixedCaseInput_Uppercases_AllItems()
        {
            var json = /*lang=json*/ @"[ ""101-alpha"", ""102-Beta"", ""101-GaMmA"" ]";
            var list = JsonSerializer.Deserialize<ProductNameList>(json, CreateOptions())!;

            Assert.Equal(new[]
            {
                "101-ALPHA", "102-BETA", "101-GAMMA"
            }, Names(list));
        }

        [Fact]
        public void Serialize_InsideContainer_NonNullable_WritesUppercaseArray()
        {
            var container = new ContainerNonNullable
            {
                Names = MakeList("101-a", "101-b", "102-c")
            };

            var json = JsonSerializer.Serialize(container, CreateOptions(true));
            var node = JsonNode.Parse(json)!.AsObject();

            Assert.True(node.TryGetPropertyValue(nameof(ContainerNonNullable.Names), out var namesNode));

            var arr = namesNode!.AsArray();

            Assert.Equal(3, arr.Count);
            Assert.Equal("101-A", arr[0]!.GetValue<string>());
            Assert.Equal("101-B", arr[1]!.GetValue<string>());
            Assert.Equal("102-C", arr[2]!.GetValue<string>());
        }

        [Fact]
        public void Deserialize_InsideContainer_Nullable_AllowsNull()
        {
            var json = /*lang=json*/ @"{ ""Names"": null }";
            var c = JsonSerializer.Deserialize<ContainerNullable>(json, CreateOptions());

            Assert.NotNull(c);
            Assert.Null(c!.Names);
        }

        [Fact]
        public void Deserialize_InsideContainer_Array_BindsAndUppercases()
        {
            var json = /*lang=json*/ @"{ ""Names"": [""101-alpha"", ""102-beta""] }";
            var c = JsonSerializer.Deserialize<ContainerNullable>(json, CreateOptions())!;

            Assert.NotNull(c.Names);
            Assert.Equal(new[]
            {
                "101-ALPHA", "102-BETA"
            }, Names(c.Names!));
        }

        [Fact]
        public void Deserialize_Array_WithDuplicates_PreservesDuplicates_AndUppercases()
        {
            // Converter calls IJsonList<T>.Add; your explicit impl adds directly (duplicates preserved).
            var json = /*lang=json*/ @"[ ""101-dup"", ""101-dup"", ""102-x"" ]";
            var list = JsonSerializer.Deserialize<ProductNameList>(json, CreateOptions())!;

            Assert.Equal(new[]
            {
                "101-DUP", "101-DUP", "102-X"
            }, Names(list));
        }

        [Fact]
        public void Deserialize_InvalidToken_NonArray_ThrowsJsonException()
        {
            Assert.ThrowsAny<JsonException>(() => JsonSerializer.Deserialize<ProductNameList>("\"not-an-array\"", CreateOptions()));

            Assert.ThrowsAny<JsonException>(() => JsonSerializer.Deserialize<ProductNameList>("123", CreateOptions()));
        }

        [Fact]
        public void Deserialize_Array_WithNullElement_Throws()
        {
            var json = /*lang=json*/ @"[ null ]";
            Assert.ThrowsAny<JsonException>(() => JsonSerializer.Deserialize<ProductNameList>(json, CreateOptions()));
        }

        [Fact]
        public void Deserialize_Array_WithInvalidItem_Throws()
        {
            // Violates rule: must start with 101 or 102
            var json = /*lang=json*/ @"[ ""999-oops"" ]";
            var ex = Record.Exception(() => JsonSerializer.Deserialize<ProductNameList>(json, CreateOptions()));

            Assert.NotNull(ex); // typically JsonException or wrapped
        }

        [Fact]
        public void Deserialize_Array_WithMixedValidInvalid_Throws()
        {
            var json = /*lang=json*/ @"[ ""101-good"", ""100-bad"", ""102-good"" ]";
            var ex = Record.Exception(() => JsonSerializer.Deserialize<ProductNameList>(json, CreateOptions()));

            Assert.NotNull(ex);
        }

        [Fact]
        public void IReadOnlyList_Surface_Behaves_As_List_AndUppercases()
        {
            var list = MakeList("101-a", "101-b", "102-c");

            Assert.Equal(3, ((IReadOnlyCollection<ProductName>)list).Count);
            Assert.Equal("101-A", list[0].ToString());
            Assert.Equal("102-C", list[2].ToString());
            Assert.Collection(list,
                p => Assert.Equal("101-A", p.ToString()),
                p => Assert.Equal("101-B", p.ToString()),
                p => Assert.Equal("102-C", p.ToString()));
        }

        private sealed class ContainerNullable
        {
            public ProductNameList? Names { get; set; }
        }

        private sealed class ContainerNonNullable
        {
            [Required] public ProductNameList Names { get; set; } = new();
        }
    }
}
