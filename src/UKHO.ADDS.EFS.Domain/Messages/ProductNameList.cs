using System.Collections;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace UKHO.ADDS.EFS.Messages;

/// <summary>
/// Represents a collection of product names.
/// </summary>
[JsonConverter(typeof(ProductNameListJsonConverter))]
public class ProductNameList : IEnumerable<ProductName>
{
    private readonly List<ProductName> _products;

    public ProductNameList()
    {
        _products = [];
    }

    public ProductNameList(IEnumerable<string> products)
    {
        _products = products?.Select(p => new ProductName(p)).ToList() ?? [];
    }

    public ProductNameList(IEnumerable<ProductName> products)
    {
        _products = products?.ToList() ?? [];
    }

    /// <summary>
    /// Gets the products as an enumerable collection.
    /// </summary>
    public IEnumerable<ProductName> Products => _products;

    /// <summary>
    /// Gets a value indicating whether the list contains any products.
    /// </summary>
    public bool HasProducts => _products.Count > 0;

    /// <summary>
    /// Gets the count of products in the list.
    /// </summary>
    public int Count => _products.Count;

    /// <summary>
    /// Returns a string representation of the product list as a comma-separated string.
    /// </summary>
    /// <returns>A comma-separated string of product names.</returns>
    public override string ToString() => string.Join(", ", _products.Select(p => p.ToString()));

    /// <summary>
    /// Implicit conversion from string array to ProductNameList.
    /// </summary>
    public static implicit operator ProductNameList(string[] products) => new(products);

    /// <summary>
    /// Implicit conversion from ProductNameList to string array.
    /// </summary>
    public static implicit operator string[](ProductNameList productNameList) => 
        productNameList._products.Select(p => p.ToString()).ToArray();

    public IEnumerator<ProductName> GetEnumerator() => _products.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

/// <summary>
/// JSON converter for ProductNameList to handle array deserialization.
/// </summary>
public class ProductNameListJsonConverter : JsonConverter<ProductNameList>
{
    public override bool HandleNull => true;

    public override ProductNameList Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        // Handle null values explicitly - return an empty ProductNameList
        if (reader.TokenType == JsonTokenType.Null)
        {
            return new ProductNameList();
        }

        if (reader.TokenType == JsonTokenType.StartArray)
        {
            var products = new List<string>();
            
            while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
            {
                if (reader.TokenType == JsonTokenType.String)
                {
                    var productName = reader.GetString();
                    if (!string.IsNullOrWhiteSpace(productName))
                    {
                        products.Add(productName);
                    }
                }
            }
            
            return new ProductNameList(products);
        }

        if (reader.TokenType == JsonTokenType.String)
        {
            // Handle comma-separated string format as fallback
            var stringValue = reader.GetString();
            if (string.IsNullOrEmpty(stringValue))
            {
                return new ProductNameList();
            }

            var productNames = stringValue.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                         .Select(p => p.Trim())
                                         .Where(p => !string.IsNullOrEmpty(p));
            return new ProductNameList(productNames);
        }

        throw new JsonException($"Unexpected token type: {reader.TokenType}");
    }

    public override void Write(Utf8JsonWriter writer, ProductNameList? value, JsonSerializerOptions options)
    {
        // Always write as an array, never as null - this ensures Scalar API UI shows []
        if (value == null || !value.HasProducts)
        {
            writer.WriteStartArray();
            writer.WriteEndArray();
            return;
        }

        writer.WriteStartArray();
        
        foreach (var product in value.Products)
        {
            writer.WriteStringValue(product.ToString());
        }
        
        writer.WriteEndArray();
    }
}
