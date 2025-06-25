namespace UKHO.ADDS.Configuration.Schema
{
    public sealed class AddsEnvironment : IEquatable<AddsEnvironment>
    {
        public static readonly AddsEnvironment Local = new("local");
        public static readonly AddsEnvironment Development = new("dev");
        public static readonly AddsEnvironment VNextIat = new("vnext-iat");
        public static readonly AddsEnvironment VNextE2E = new("vnext-e2e");
        public static readonly AddsEnvironment Iat = new("iat");
        public static readonly AddsEnvironment PreProd = new("preprod");
        public static readonly AddsEnvironment Live = new("live");

        private static readonly Dictionary<string, AddsEnvironment> _known = new(StringComparer.OrdinalIgnoreCase)
        {
            [Local.Value] = Local,
            [Development.Value] = Development,
            [VNextIat.Value] = VNextIat,
            [VNextE2E.Value] = VNextE2E,
            [Iat.Value] = Iat,
            [PreProd.Value] = PreProd,
            [Live.Value] = Live
        };

        private AddsEnvironment(string value) => Value = value;

        public string Value { get; }

        public bool Equals(AddsEnvironment? other) => other is not null && StringComparer.OrdinalIgnoreCase.Equals(Value, other.Value);

        public static bool TryParse(string? input, out AddsEnvironment? result)
        {
            if (input != null && _known.TryGetValue(input, out var env))
            {
                result = env;
                return true;
            }

            result = null;
            return false;
        }

        public static AddsEnvironment Parse(string input)
        {
            if (TryParse(input, out var env))
            {
                return env;
            }

            throw new ArgumentException($"Invalid AddsEnvironment: '{input}'", nameof(input));
        }

        public bool IsLocal() => this == Local;

        public override string ToString() => Value;

        public override bool Equals(object? obj) => Equals(obj as AddsEnvironment);

        public override int GetHashCode() => StringComparer.OrdinalIgnoreCase.GetHashCode(Value);

        public static bool operator ==(AddsEnvironment? left, AddsEnvironment? right) => EqualityComparer<AddsEnvironment>.Default.Equals(left, right);

        public static bool operator !=(AddsEnvironment? left, AddsEnvironment? right) => !(left == right);
    }
}
