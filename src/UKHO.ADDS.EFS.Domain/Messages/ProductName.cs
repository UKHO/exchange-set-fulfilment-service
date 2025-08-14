namespace UKHO.ADDS.EFS.Messages;

/// <summary>
/// Represents a product name as a value object.
/// </summary>
public readonly struct ProductName : IEquatable<ProductName>
{
    private readonly string _value;

    public ProductName(string value)
    {
        _value = value ?? throw new ArgumentNullException(nameof(value));
    }

    /// <summary>
    /// Implicit conversion from string to ProductName.
    /// </summary>
    public static implicit operator ProductName(string value) => new(value);

    /// <summary>
    /// Implicit conversion from ProductName to string.
    /// </summary>
    public static implicit operator string(ProductName productName) => productName._value;

    public override string ToString() => _value ?? string.Empty;

    public bool Equals(ProductName other) => string.Equals(_value, other._value, StringComparison.Ordinal);

    public override bool Equals(object? obj) => obj is ProductName other && Equals(other);

    public override int GetHashCode() => _value?.GetHashCode() ?? 0;

    public static bool operator ==(ProductName left, ProductName right) => left.Equals(right);

    public static bool operator !=(ProductName left, ProductName right) => !left.Equals(right);
}
