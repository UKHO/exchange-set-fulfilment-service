using System.Text.Json;
using UKHO.ADDS.Aspire.Configuration.Emulator.Common;
using Xunit;

namespace UKHO.ADDS.Aspire.Configuration.Emulator.Tests.Common
{
    public class KeyValuePairJsonEncoderTests
    {
        // ReSharper disable once InconsistentNaming
        public static IEnumerable<object?[]> Encode_Document_KeyValuePairsAndPrefixAndSeparator_TestCases =
        [
            [
                new Dictionary<string, string?>
                {
                    {
                        "TestKey", "TestValue"
                    }
                },
                null, null, "{\"TestKey\":\"TestValue\"}"
            ],
            [
                new Dictionary<string, string?>
                {
                    {
                        "TestPrefixTestKey", "TestValue"
                    }
                },
                "TestPrefix", null, "{\"TestKey\":\"TestValue\"}"
            ],
            [
                new Dictionary<string, string?>
                {
                    {
                        "TestKey", "TestValue"
                    }
                },
                null, ".", "{\"TestKey\":\"TestValue\"}"
            ],
            [
                new Dictionary<string, string?>
                {
                    {
                        "TestPrefix.TestKey", "TestValue"
                    }
                },
                "TestPrefix", ".", "{\"TestKey\":\"TestValue\"}"
            ],
            [
                new Dictionary<string, string?>
                {
                    {
                        "TestOuterKey.TestInnerKey", "TestValue"
                    }
                },
                null, ".", "{\"TestOuterKey\":{\"TestInnerKey\":\"TestValue\"}}"
            ],
            [
                new Dictionary<string, string?>
                {
                    {
                        "TestPrefix.TestOuterKey.TestInnerKey", "TestValue"
                    }
                },
                "TestPrefix", ".", "{\"TestOuterKey\":{\"TestInnerKey\":\"TestValue\"}}"
            ],
            [
                new Dictionary<string, string?>
                {
                    {
                        "TestKey.0", "TestValue"
                    }
                },
                null, ".", "{\"TestKey\":[\"TestValue\"]}"
            ],
            [
                new Dictionary<string, string?>
                {
                    {
                        "TestPrefix.TestKey.0", "TestValue"
                    }
                },
                "TestPrefix", ".", "{\"TestKey\":[\"TestValue\"]}"
            ],
            [
                new Dictionary<string, string?>
                {
                    {
                        "TestOuterKey.0.TestInnerKey", "TestValue"
                    }
                },
                null, ".", "{\"TestOuterKey\":[{\"TestInnerKey\":\"TestValue\"}]}"
            ],
            [
                new Dictionary<string, string?>
                {
                    {
                        "TestPrefix.TestOuterKey.0.TestInnerKey", "TestValue"
                    }
                },
                "TestPrefix", ".", "{\"TestOuterKey\":[{\"TestInnerKey\":\"TestValue\"}]}"
            ]
        ];

        public KeyValuePairJsonEncoderTests() => Encoder = new KeyValuePairJsonEncoder();

        private KeyValuePairJsonEncoder Encoder { get; }

        [Theory]
        [MemberData(nameof(Encode_Document_KeyValuePairsAndPrefixAndSeparator_TestCases))]
        public void Encode_Document_KeyValuePairsAndPrefixAndSeparator(IEnumerable<KeyValuePair<string, string?>> pairs, string? prefix, string? separator, string expected)
        {
            // Act
            using var document = Encoder.Encode(pairs, prefix, separator);

            // Assert
            Assert.Equal(JsonSerializer.Serialize(document), expected);
        }
    }
}
