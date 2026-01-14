using UKHO.ADDS.EFS.Infrastructure.Configuration.Orchestrator;

namespace UKHO.ADDS.EFS.Infrastructure.Builders.Configuration
{
    public sealed class BuilderAddsEnvironment : IEquatable<BuilderAddsEnvironment>
    {
        public static readonly BuilderAddsEnvironment Local = new("local");
        public static readonly BuilderAddsEnvironment Development = new("dev");
        public static readonly BuilderAddsEnvironment VNextIat = new("vni");
        public static readonly BuilderAddsEnvironment VNextE2E = new("vne");
        public static readonly BuilderAddsEnvironment Iat = new("iat");
        public static readonly BuilderAddsEnvironment PreProd = new("preprod");
        public static readonly BuilderAddsEnvironment Live = new("live");

        private static readonly Dictionary<string, BuilderAddsEnvironment> _known = new(StringComparer.OrdinalIgnoreCase)
        {
            [Local.Value] = Local,
            [Development.Value] = Development,
            [VNextIat.Value] = VNextIat,
            [VNextE2E.Value] = VNextE2E,
            [Iat.Value] = Iat,
            [PreProd.Value] = PreProd,
            [Live.Value] = Live
        };

        private BuilderAddsEnvironment(string value) => Value = value;

        public string Value { get; }

        public bool Equals(BuilderAddsEnvironment? other) => other is not null && StringComparer.OrdinalIgnoreCase.Equals(Value, other.Value);

        public static bool TryParse(string? input, out BuilderAddsEnvironment? result)
        {
            if (input != null && _known.TryGetValue(input, out var env))
            {
                result = env;
                return true;
            }

            result = null;
            return false;
        }

        public static BuilderAddsEnvironment Parse(string input)
        {
            if (TryParse(input, out var env))
            {
                return env ?? throw new InvalidOperationException($"Parsed AddsEnvironment cannot be null: '{input}'");
            }

            throw new ArgumentException($"Invalid AddsEnvironment: '{input}'", nameof(input));
        }

        public static BuilderAddsEnvironment GetEnvironment()
        {
            var env = Environment.GetEnvironmentVariable(BuilderEnvironmentVariables.AddsEnvironment);

            if (string.IsNullOrEmpty(env) || !TryParse(env, out _))
            {
                throw new InvalidOperationException($"Environment variable '{BuilderEnvironmentVariables.AddsEnvironment}' is not set or invalid. Make sure the caller is registered using UKHO.ADDS.Aspire.Configuration.Hosting");
            }

            return Parse(env);
        }

        /// <summary>
        ///     The local environment.
        /// </summary>
        /// <returns></returns>
        public bool IsLocal() => this == Local;

        /// <summary>
        ///     The ADDS Azure Dev environment.
        /// </summary>
        /// <returns></returns>
        public bool IsDev() => this == Development;

        public override string ToString() => Value;

        public override bool Equals(object? obj) => Equals(obj as BuilderAddsEnvironment);

        public override int GetHashCode() => StringComparer.OrdinalIgnoreCase.GetHashCode(Value);

        public static bool operator ==(BuilderAddsEnvironment? left, BuilderAddsEnvironment? right) => EqualityComparer<BuilderAddsEnvironment>.Default.Equals(left, right);

        public static bool operator !=(BuilderAddsEnvironment? left, BuilderAddsEnvironment? right) => !(left == right);
    }
}
