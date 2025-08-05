using System.Text.Json;
using UKHO.ADDS.Aspire.Configuration.Emulator.Common;
using Xunit;

namespace UKHO.ADDS.Aspire.Configuration.Emulator.Tests.Common
{
    public class KeyValuePairJsonDecoderTests
    {
        // ReSharper disable once InconsistentNaming
        public static IEnumerable<object?[]> Decode_KeyValuePairs_DocumentAndPrefixAndSeparator_TestCases =
        [
            [
                "{\"TestKey\":\"TestValue\"}", null, null, new Dictionary<string, string?>
                {
                    {
                        "TestKey", "TestValue"
                    }
                }
            ],
            [
                "{\"TestKey\":\"TestValue\"}", "TestPrefix", null, new Dictionary<string, string?>
                {
                    {
                        "TestPrefixTestKey", "TestValue"
                    }
                }
            ],
            [
                "{\"TestKey\":\"TestValue\"}", null, ".", new Dictionary<string, string?>
                {
                    {
                        "TestKey", "TestValue"
                    }
                }
            ],
            [
                "{\"TestKey\":\"TestValue\"}", "TestPrefix", ".", new Dictionary<string, string?>
                {
                    {
                        "TestPrefix.TestKey", "TestValue"
                    }
                }
            ],
            [
                "{\"TestOuterKey\":{\"TestInnerKey\":\"TestValue\"}}", null, null, new Dictionary<string, string?>
                {
                    {
                        "TestOuterKeyTestInnerKey", "TestValue"
                    }
                }
            ],
            [
                "{\"TestOuterKey\":{\"TestInnerKey\":\"TestValue\"}}", "TestPrefix", null, new Dictionary<string, string?>
                {
                    {
                        "TestPrefixTestOuterKeyTestInnerKey", "TestValue"
                    }
                }
            ],
            [
                "{\"TestOuterKey\":{\"TestInnerKey\":\"TestValue\"}}", null, ".", new Dictionary<string, string?>
                {
                    {
                        "TestOuterKey.TestInnerKey", "TestValue"
                    }
                }
            ],
            [
                "{\"TestOuterKey\":{\"TestInnerKey\":\"TestValue\"}}", "TestPrefix", ".", new Dictionary<string, string?>
                {
                    {
                        "TestPrefix.TestOuterKey.TestInnerKey", "TestValue"
                    }
                }
            ],
            [
                "{\"TestKey\":[\"TestValue\"]}", null, null, new Dictionary<string, string?>
                {
                    {
                        "TestKey0", "TestValue"
                    }
                }
            ],
            [
                "{\"TestKey\":[\"TestValue\"]}", "TestPrefix", null, new Dictionary<string, string?>
                {
                    {
                        "TestPrefixTestKey0", "TestValue"
                    }
                }
            ],
            [
                "{\"TestKey\":[\"TestValue\"]}", null, ".", new Dictionary<string, string?>
                {
                    {
                        "TestKey.0", "TestValue"
                    }
                }
            ],
            [
                "{\"TestKey\":[\"TestValue\"]}", "TestPrefix", ".", new Dictionary<string, string?>
                {
                    {
                        "TestPrefix.TestKey.0", "TestValue"
                    }
                }
            ],
            [
                "{\"TestOuterKey\":[{\"TestInnerKey\":\"TestValue\"}]}", null, null, new Dictionary<string, string?>
                {
                    {
                        "TestOuterKey0TestInnerKey", "TestValue"
                    }
                }
            ],
            [
                "{\"TestOuterKey\":[{\"TestInnerKey\":\"TestValue\"}]}", "TestPrefix", null, new Dictionary<string, string?>
                {
                    {
                        "TestPrefixTestOuterKey0TestInnerKey", "TestValue"
                    }
                }
            ],
            [
                "{\"TestOuterKey\":[{\"TestInnerKey\":\"TestValue\"}]}", null, ".", new Dictionary<string, string?>
                {
                    {
                        "TestOuterKey.0.TestInnerKey", "TestValue"
                    }
                }
            ],
            [
                "{\"TestOuterKey\":[{\"TestInnerKey\":\"TestValue\"}]}", "TestPrefix", ".", new Dictionary<string, string?>
                {
                    {
                        "TestPrefix.TestOuterKey.0.TestInnerKey", "TestValue"
                    }
                }
            ]
        ];

        public KeyValuePairJsonDecoderTests() => Decoder = new KeyValuePairJsonDecoder();

        private KeyValuePairJsonDecoder Decoder { get; }

        [Theory]
        [MemberData(nameof(Decode_KeyValuePairs_DocumentAndPrefixAndSeparator_TestCases))]
        public void Decode_KeyValuePairs_DocumentAndPrefixAndSeparator(string json, string? prefix, string? separator, IEnumerable<KeyValuePair<string, string?>> expected)
        {
            // Arrange
            using var document = JsonDocument.Parse(json);

            // Act
            var settings = Decoder.Decode(document, prefix, separator);

            // Assert
            Assert.Equal(settings, expected);
        }
    }
}
