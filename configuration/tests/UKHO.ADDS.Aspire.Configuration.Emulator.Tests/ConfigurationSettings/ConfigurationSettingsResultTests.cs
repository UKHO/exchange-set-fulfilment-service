using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using UKHO.ADDS.Aspire.Configuration.Emulator.Common;
using UKHO.ADDS.Aspire.Configuration.Emulator.ConfigurationSettings;
using Xunit;

namespace UKHO.ADDS.Aspire.Configuration.Emulator.Tests.ConfigurationSettings
{
    public class ConfigurationSettingsResultTests
    {
        // ReSharper disable once InconsistentNaming
        public static IEnumerable<object?[]> ExecuteAsync_ResponseBody_ConfigurationSettings_TestCases =
        [
            [
                new ConfigurationSetting[]
                {
                    new(
                        "TestEtag",
                        "TestKey",
                        DateTimeOffset.Parse("2023-10-01T00:00:00+00:00"),
                        false)
                },
                "{\"items\":[{\"etag\":\"TestEtag\",\"key\":\"TestKey\",\"label\":null,\"content_type\":null,\"value\":null,\"tags\":{},\"locked\":false,\"last_modified\":\"2023-10-01T00:00:00.0000000\\u002B00:00\"}]}"
            ],
            [
                new ConfigurationSetting[]
                {
                    new(
                        "TestEtag",
                        "TestKey",
                        DateTimeOffset.Parse("2023-10-01T00:00:00+00:00"),
                        false,
                        "TestLabel")
                },
                "{\"items\":[{\"etag\":\"TestEtag\",\"key\":\"TestKey\",\"label\":\"TestLabel\",\"content_type\":null,\"value\":null,\"tags\":{},\"locked\":false,\"last_modified\":\"2023-10-01T00:00:00.0000000\\u002B00:00\"}]}"
            ],
            [
                new ConfigurationSetting[]
                {
                    new(
                        "TestEtag",
                        "TestKey",
                        DateTimeOffset.Parse("2023-10-01T00:00:00+00:00"),
                        false,
                        contentType: "TestContentType")
                },
                "{\"items\":[{\"etag\":\"TestEtag\",\"key\":\"TestKey\",\"label\":null,\"content_type\":\"TestContentType\",\"value\":null,\"tags\":{},\"locked\":false,\"last_modified\":\"2023-10-01T00:00:00.0000000\\u002B00:00\"}]}"
            ],
            [
                new ConfigurationSetting[]
                {
                    new(
                        "TestEtag",
                        "TestKey",
                        DateTimeOffset.Parse("2023-10-01T00:00:00+00:00"),
                        false,
                        value: "TestValue")
                },
                "{\"items\":[{\"etag\":\"TestEtag\",\"key\":\"TestKey\",\"label\":null,\"content_type\":null,\"value\":\"TestValue\",\"tags\":{},\"locked\":false,\"last_modified\":\"2023-10-01T00:00:00.0000000\\u002B00:00\"}]}"
            ],
            [
                new ConfigurationSetting[]
                {
                    new(
                        "TestEtag",
                        "TestKey",
                        DateTimeOffset.Parse("2023-10-01T00:00:00+00:00"),
                        false,
                        tags: new Dictionary<string, string>
                        {
                            {
                                "TestKey", "TestValue"
                            }
                        })
                },
                "{\"items\":[{\"etag\":\"TestEtag\",\"key\":\"TestKey\",\"label\":null,\"content_type\":null,\"value\":null,\"tags\":{\"TestKey\":\"TestValue\"},\"locked\":false,\"last_modified\":\"2023-10-01T00:00:00.0000000\\u002B00:00\"}]}"
            ],
            [
                new ConfigurationSetting[]
                {
                    new(
                        "TestEtag",
                        "TestKey",
                        DateTimeOffset.Parse("2023-10-01T00:00:00+00:00"),
                        false,
                        "TestLabel",
                        "TestContentType",
                        "TestValue",
                        new Dictionary<string, string>
                        {
                            {
                                "TestKey", "TestValue"
                            }
                        })
                },
                "{\"items\":[{\"etag\":\"TestEtag\",\"key\":\"TestKey\",\"label\":\"TestLabel\",\"content_type\":\"TestContentType\",\"value\":\"TestValue\",\"tags\":{\"TestKey\":\"TestValue\"},\"locked\":false,\"last_modified\":\"2023-10-01T00:00:00.0000000\\u002B00:00\"}]}"
            ],
            [
                new ConfigurationSetting[]
                {
                    new(
                        "TestEtag",
                        "TestKey1",
                        DateTimeOffset.Parse("2023-10-01T00:00:00+00:00"),
                        false),
                    new(
                        "TestEtag",
                        "TestKey2",
                        DateTimeOffset.Parse("2023-10-01T00:00:00+00:00"),
                        false)
                },
                "{\"items\":[{\"etag\":\"TestEtag\",\"key\":\"TestKey1\",\"label\":null,\"content_type\":null,\"value\":null,\"tags\":{},\"locked\":false,\"last_modified\":\"2023-10-01T00:00:00.0000000\\u002B00:00\"},{\"etag\":\"TestEtag\",\"key\":\"TestKey2\",\"label\":null,\"content_type\":null,\"value\":null,\"tags\":{},\"locked\":false,\"last_modified\":\"2023-10-01T00:00:00.0000000\\u002B00:00\"}]}"
            ],
            [
                new ConfigurationSetting[]
                {
                    new(
                        "TestEtag",
                        "TestKey1",
                        DateTimeOffset.Parse("2023-10-01T00:00:00+00:00"),
                        false,
                        "TestLabel"),
                    new(
                        "TestEtag",
                        "TestKey2",
                        DateTimeOffset.Parse("2023-10-01T00:00:00+00:00"),
                        false,
                        "TestLabel")
                },
                "{\"items\":[{\"etag\":\"TestEtag\",\"key\":\"TestKey1\",\"label\":\"TestLabel\",\"content_type\":null,\"value\":null,\"tags\":{},\"locked\":false,\"last_modified\":\"2023-10-01T00:00:00.0000000\\u002B00:00\"},{\"etag\":\"TestEtag\",\"key\":\"TestKey2\",\"label\":\"TestLabel\",\"content_type\":null,\"value\":null,\"tags\":{},\"locked\":false,\"last_modified\":\"2023-10-01T00:00:00.0000000\\u002B00:00\"}]}"
            ],
            [
                new ConfigurationSetting[]
                {
                    new(
                        "TestEtag",
                        "TestKey1",
                        DateTimeOffset.Parse("2023-10-01T00:00:00+00:00"),
                        false,
                        contentType: "TestContentType"),
                    new(
                        "TestEtag",
                        "TestKey2",
                        DateTimeOffset.Parse("2023-10-01T00:00:00+00:00"),
                        false,
                        contentType: "TestContentType")
                },
                "{\"items\":[{\"etag\":\"TestEtag\",\"key\":\"TestKey1\",\"label\":null,\"content_type\":\"TestContentType\",\"value\":null,\"tags\":{},\"locked\":false,\"last_modified\":\"2023-10-01T00:00:00.0000000\\u002B00:00\"},{\"etag\":\"TestEtag\",\"key\":\"TestKey2\",\"label\":null,\"content_type\":\"TestContentType\",\"value\":null,\"tags\":{},\"locked\":false,\"last_modified\":\"2023-10-01T00:00:00.0000000\\u002B00:00\"}]}"
            ],
            [
                new ConfigurationSetting[]
                {
                    new(
                        "TestEtag",
                        "TestKey1",
                        DateTimeOffset.Parse("2023-10-01T00:00:00+00:00"),
                        false,
                        value: "TestValue"),
                    new(
                        "TestEtag",
                        "TestKey2",
                        DateTimeOffset.Parse("2023-10-01T00:00:00+00:00"),
                        false,
                        value: "TestValue")
                },
                "{\"items\":[{\"etag\":\"TestEtag\",\"key\":\"TestKey1\",\"label\":null,\"content_type\":null,\"value\":\"TestValue\",\"tags\":{},\"locked\":false,\"last_modified\":\"2023-10-01T00:00:00.0000000\\u002B00:00\"},{\"etag\":\"TestEtag\",\"key\":\"TestKey2\",\"label\":null,\"content_type\":null,\"value\":\"TestValue\",\"tags\":{},\"locked\":false,\"last_modified\":\"2023-10-01T00:00:00.0000000\\u002B00:00\"}]}"
            ],
            [
                new ConfigurationSetting[]
                {
                    new(
                        "TestEtag",
                        "TestKey1",
                        DateTimeOffset.Parse("2023-10-01T00:00:00+00:00"),
                        false,
                        tags: new Dictionary<string, string>
                        {
                            {
                                "TestKey", "TestValue"
                            }
                        }),
                    new(
                        "TestEtag",
                        "TestKey2",
                        DateTimeOffset.Parse("2023-10-01T00:00:00+00:00"),
                        false,
                        tags: new Dictionary<string, string>
                        {
                            {
                                "TestKey", "TestValue"
                            }
                        })
                },
                "{\"items\":[{\"etag\":\"TestEtag\",\"key\":\"TestKey1\",\"label\":null,\"content_type\":null,\"value\":null,\"tags\":{\"TestKey\":\"TestValue\"},\"locked\":false,\"last_modified\":\"2023-10-01T00:00:00.0000000\\u002B00:00\"},{\"etag\":\"TestEtag\",\"key\":\"TestKey2\",\"label\":null,\"content_type\":null,\"value\":null,\"tags\":{\"TestKey\":\"TestValue\"},\"locked\":false,\"last_modified\":\"2023-10-01T00:00:00.0000000\\u002B00:00\"}]}"
            ],
            [
                new ConfigurationSetting[]
                {
                    new(
                        "TestEtag",
                        "TestKey1",
                        DateTimeOffset.Parse("2023-10-01T00:00:00+00:00"),
                        false,
                        "TestLabel",
                        "TestContentType",
                        "TestValue",
                        new Dictionary<string, string>
                        {
                            {
                                "TestKey", "TestValue"
                            }
                        }),
                    new(
                        "TestEtag",
                        "TestKey2",
                        DateTimeOffset.Parse("2023-10-01T00:00:00+00:00"),
                        false,
                        "TestLabel",
                        "TestContentType",
                        "TestValue",
                        new Dictionary<string, string>
                        {
                            {
                                "TestKey", "TestValue"
                            }
                        })
                },
                "{\"items\":[{\"etag\":\"TestEtag\",\"key\":\"TestKey1\",\"label\":\"TestLabel\",\"content_type\":\"TestContentType\",\"value\":\"TestValue\",\"tags\":{\"TestKey\":\"TestValue\"},\"locked\":false,\"last_modified\":\"2023-10-01T00:00:00.0000000\\u002B00:00\"},{\"etag\":\"TestEtag\",\"key\":\"TestKey2\",\"label\":\"TestLabel\",\"content_type\":\"TestContentType\",\"value\":\"TestValue\",\"tags\":{\"TestKey\":\"TestValue\"},\"locked\":false,\"last_modified\":\"2023-10-01T00:00:00.0000000\\u002B00:00\"}]}"
            ]
        ];

        // ReSharper disable once InconsistentNaming
        public static IEnumerable<object?[]> ExecuteAsync_ResponseBody_Select_TestCases =
        [
            [
                new ConfigurationSetting[]
                {
                    new(
                        "TestEtag",
                        "TestKey",
                        DateTimeOffset.Parse("2023-10-01T00:00:00+00:00"),
                        false)
                },
                "key", "{\"items\":[{\"key\":\"TestKey\"}]}"
            ],
            [
                new ConfigurationSetting[]
                {
                    new(
                        "TestEtag",
                        "TestKey",
                        DateTimeOffset.Parse("2023-10-01T00:00:00+00:00"),
                        false)
                },
                "key,value", "{\"items\":[{\"key\":\"TestKey\",\"value\":null}]}"
            ],
            [
                new ConfigurationSetting[]
                {
                    new(
                        "TestEtag",
                        "TestKey",
                        DateTimeOffset.Parse("2023-10-01T00:00:00+00:00"),
                        false)
                },
                "", "{\"items\":[{\"etag\":\"TestEtag\",\"key\":\"TestKey\",\"label\":null,\"content_type\":null,\"value\":null,\"tags\":{},\"locked\":false,\"last_modified\":\"2023-10-01T00:00:00.0000000\\u002B00:00\"}]}"
            ],
            [
                new ConfigurationSetting[]
                {
                    new(
                        "TestEtag",
                        "TestKey",
                        DateTimeOffset.Parse("2023-10-01T00:00:00+00:00"),
                        false)
                },
                null, "{\"items\":[{\"etag\":\"TestEtag\",\"key\":\"TestKey\",\"label\":null,\"content_type\":null,\"value\":null,\"tags\":{},\"locked\":false,\"last_modified\":\"2023-10-01T00:00:00.0000000\\u002B00:00\"}]}"
            ]
        ];

        public ConfigurationSettingsResultTests() => HttpContext = new DefaultHttpContext();

        private HttpContext HttpContext { get; }

        [Fact]
        public async Task ExecuteAsync_ContentTypeResponseHeader_ConfigurationSettings()
        {
            // Arrange
            var settings = new List<ConfigurationSetting>
            {
                new("TestEtag", "TestKey", DateTimeOffset.UtcNow, false)
            };

            // Act
            await new ConfigurationSettingsResult(settings).ExecuteAsync(HttpContext);

            // Assert
            Assert.Equal(HttpContext.Response.Headers.ContentType, MediaType.ConfigurationSettings);
        }

        [Fact]
        public async Task ExecuteAsync_MementoDatetimeResponseHeader_MementoDatetime()
        {
            // Arrange
            var settings = new List<ConfigurationSetting>
            {
                new("TestEtag", "TestKey", DateTimeOffset.UtcNow, false)
            };
            var mementoDatetime = DateTimeOffset.UtcNow;

            // Act
            await new ConfigurationSettingsResult(settings, mementoDatetime).ExecuteAsync(HttpContext);

            // Assert
            Assert.Equal(HttpContext.Response.Headers["Memento-Datetime"], mementoDatetime.ToString("R"));
        }

        [Theory]
        [MemberData(nameof(ExecuteAsync_ResponseBody_ConfigurationSettings_TestCases))]
        public async Task ExecuteAsync_ResponseBody_ConfigurationSettings(ConfigurationSetting[] settings, string expected)
        {
            // Arrange
            using var stream = new MemoryStream();
            var feature = new StreamResponseBodyFeature(stream);
            HttpContext.Features.Set<IHttpResponseBodyFeature>(feature);

            // Act
            await new ConfigurationSettingsResult(settings).ExecuteAsync(HttpContext);

            // Assert
            stream.Seek(0, SeekOrigin.Begin);
            using var reader = new StreamReader(stream);
            var actual = await reader.ReadToEndAsync();
            Assert.Equal(actual, expected);
        }

        [Theory]
        [MemberData(nameof(ExecuteAsync_ResponseBody_Select_TestCases))]
        public async Task ExecuteAsync_ResponseBody_Select(ConfigurationSetting[] settings, string? select, string expected)
        {
            // Arrange
            using var stream = new MemoryStream();
            var feature = new StreamResponseBodyFeature(stream);
            HttpContext.Features.Set<IHttpResponseBodyFeature>(feature);

            // Act
            await new ConfigurationSettingsResult(settings, select: select).ExecuteAsync(HttpContext);

            // Assert
            stream.Seek(0, SeekOrigin.Begin);
            using var reader = new StreamReader(stream);
            var actual = await reader.ReadToEndAsync();
            Assert.Equal(actual, expected);
        }

        [Fact]
        public async Task ExecuteAsync_ResponseStatusCode_ConfigurationSettings()
        {
            // Arrange
            var settings = new List<ConfigurationSetting>
            {
                new("TestEtag", "TestKey", DateTimeOffset.UtcNow, false)
            };

            // Act
            await new ConfigurationSettingsResult(settings).ExecuteAsync(HttpContext);

            // Assert
            Assert.Equal(HttpContext.Response.StatusCode, StatusCodes.Status200OK);
        }
    }
}
