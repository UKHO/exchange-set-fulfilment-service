using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using UKHO.ADDS.Aspire.Configuration.Emulator.Common;
using UKHO.ADDS.Aspire.Configuration.Emulator.ConfigurationSettings;
using Xunit;

namespace UKHO.ADDS.Aspire.Configuration.Emulator.Tests.ConfigurationSettings
{
    public class ConfigurationSettingResultTests
    {
        // ReSharper disable once InconsistentNaming
        public static IEnumerable<object?[]> ExecuteAsync_ResponseBody_ConfigurationSetting_TestCases =
        [
            [
                new ConfigurationSetting(
                    "TestEtag",
                    "TestKey",
                    DateTimeOffset.Parse("2023-10-01T00:00:00+00:00"),
                    false),
                "{\"etag\":\"TestEtag\",\"key\":\"TestKey\",\"label\":null,\"content_type\":null,\"value\":null,\"tags\":{},\"locked\":false,\"last_modified\":\"2023-10-01T00:00:00.0000000\\u002B00:00\"}"
            ],
            [
                new ConfigurationSetting(
                    "TestEtag",
                    "TestKey",
                    DateTimeOffset.Parse("2023-10-01T00:00:00+00:00"),
                    false,
                    "TestLabel"),
                "{\"etag\":\"TestEtag\",\"key\":\"TestKey\",\"label\":\"TestLabel\",\"content_type\":null,\"value\":null,\"tags\":{},\"locked\":false,\"last_modified\":\"2023-10-01T00:00:00.0000000\\u002B00:00\"}"
            ],
            [
                new ConfigurationSetting(
                    "TestEtag",
                    "TestKey",
                    DateTimeOffset.Parse("2023-10-01T00:00:00+00:00"),
                    false,
                    contentType: "TestContentType"),
                "{\"etag\":\"TestEtag\",\"key\":\"TestKey\",\"label\":null,\"content_type\":\"TestContentType\",\"value\":null,\"tags\":{},\"locked\":false,\"last_modified\":\"2023-10-01T00:00:00.0000000\\u002B00:00\"}"
            ],
            [
                new ConfigurationSetting(
                    "TestEtag",
                    "TestKey",
                    DateTimeOffset.Parse("2023-10-01T00:00:00+00:00"),
                    false,
                    value: "TestValue"),
                "{\"etag\":\"TestEtag\",\"key\":\"TestKey\",\"label\":null,\"content_type\":null,\"value\":\"TestValue\",\"tags\":{},\"locked\":false,\"last_modified\":\"2023-10-01T00:00:00.0000000\\u002B00:00\"}"
            ],
            [
                new ConfigurationSetting(
                    "TestEtag",
                    "TestKey",
                    DateTimeOffset.Parse("2023-10-01T00:00:00+00:00"),
                    false,
                    tags: new Dictionary<string, string>
                    {
                        {
                            "TestKey", "TestValue"
                        }
                    }),
                "{\"etag\":\"TestEtag\",\"key\":\"TestKey\",\"label\":null,\"content_type\":null,\"value\":null,\"tags\":{\"TestKey\":\"TestValue\"},\"locked\":false,\"last_modified\":\"2023-10-01T00:00:00.0000000\\u002B00:00\"}"
            ],
            [
                new ConfigurationSetting(
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
                    }),
                "{\"etag\":\"TestEtag\",\"key\":\"TestKey\",\"label\":\"TestLabel\",\"content_type\":\"TestContentType\",\"value\":\"TestValue\",\"tags\":{\"TestKey\":\"TestValue\"},\"locked\":false,\"last_modified\":\"2023-10-01T00:00:00.0000000\\u002B00:00\"}"
            ]
        ];

        // ReSharper disable once InconsistentNaming
        public static IEnumerable<object?[]> ExecuteAsync_ResponseBody_Select_TestCases =
        [
            [
                new ConfigurationSetting(
                    "TestEtag",
                    "TestKey",
                    DateTimeOffset.Parse("2023-10-01T00:00:00+00:00"),
                    false),
                "key", "{\"key\":\"TestKey\"}"
            ],
            [
                new ConfigurationSetting(
                    "TestEtag",
                    "TestKey",
                    DateTimeOffset.Parse("2023-10-01T00:00:00+00:00"),
                    false),
                "key,value", "{\"key\":\"TestKey\",\"value\":null}"
            ],
            [
                new ConfigurationSetting(
                    "TestEtag",
                    "TestKey",
                    DateTimeOffset.Parse("2023-10-01T00:00:00+00:00"),
                    false),
                "", "{\"etag\":\"TestEtag\",\"key\":\"TestKey\",\"label\":null,\"content_type\":null,\"value\":null,\"tags\":{},\"locked\":false,\"last_modified\":\"2023-10-01T00:00:00.0000000\\u002B00:00\"}"
            ],
            [
                new ConfigurationSetting(
                    "TestEtag",
                    "TestKey",
                    DateTimeOffset.Parse("2023-10-01T00:00:00+00:00"),
                    false),
                null, "{\"etag\":\"TestEtag\",\"key\":\"TestKey\",\"label\":null,\"content_type\":null,\"value\":null,\"tags\":{},\"locked\":false,\"last_modified\":\"2023-10-01T00:00:00.0000000\\u002B00:00\"}"
            ]
        ];

        public ConfigurationSettingResultTests() => HttpContext = new DefaultHttpContext();

        private HttpContext HttpContext { get; }

        [Fact]
        public async Task ExecuteAsync_ContentTypeResponseHeader_ConfigurationSetting()
        {
            // Arrange
            var setting = new ConfigurationSetting("TestEtag", "TestKey", DateTimeOffset.UtcNow, false);

            // Act
            await new ConfigurationSettingResult(setting).ExecuteAsync(HttpContext);

            // Assert
            Assert.Equal(HttpContext.Response.Headers.ContentType, MediaType.ConfigurationSetting);
        }

        [Fact]
        public async Task ExecuteAsync_EtagResponseHeader_ConfigurationSetting()
        {
            // Arrange
            const string etag = "TestEtag";
            var setting = new ConfigurationSetting(etag, "TestKey", DateTimeOffset.UtcNow, false);

            // Act
            await new ConfigurationSettingResult(setting).ExecuteAsync(HttpContext);

            // Assert
            Assert.Equal(HttpContext.Response.Headers.ETag, etag);
        }

        [Fact]
        public async Task ExecuteAsync_LastModifiedResponseHeader_ConfigurationSetting()
        {
            // Arrange
            var lastModified = DateTimeOffset.UtcNow;
            var setting = new ConfigurationSetting("TestEtag", "TestKey", lastModified, false);

            // Act
            await new ConfigurationSettingResult(setting).ExecuteAsync(HttpContext);

            // Assert
            Assert.Equal(HttpContext.Response.Headers.LastModified, lastModified.ToString("R"));
        }

        [Fact]
        public async Task ExecuteAsync_MementoDatetimeResponseHeader_MementoDatetime()
        {
            // Arrange
            var setting = new ConfigurationSetting("TestEtag", "TestKey", DateTimeOffset.UtcNow, false);
            var mementoDatetime = DateTimeOffset.UtcNow;

            // Act
            await new ConfigurationSettingResult(setting, mementoDatetime).ExecuteAsync(HttpContext);

            // Assert
            Assert.Equal(HttpContext.Response.Headers["Memento-Datetime"], mementoDatetime.ToString("R"));
        }

        [Theory]
        [MemberData(nameof(ExecuteAsync_ResponseBody_ConfigurationSetting_TestCases))]
        public async Task ExecuteAsync_ResponseBody_ConfigurationSetting(ConfigurationSetting setting, string expected)
        {
            // Arrange
            using var stream = new MemoryStream();
            var feature = new StreamResponseBodyFeature(stream);
            HttpContext.Features.Set<IHttpResponseBodyFeature>(feature);

            // Act
            await new ConfigurationSettingResult(setting).ExecuteAsync(HttpContext);

            // Assert
            stream.Seek(0, SeekOrigin.Begin);
            using var reader = new StreamReader(stream);
            var actual = await reader.ReadToEndAsync();
            Assert.Equal(actual, expected);
        }

        [Theory]
        [MemberData(nameof(ExecuteAsync_ResponseBody_Select_TestCases))]
        public async Task ExecuteAsync_ResponseBody_Select(ConfigurationSetting setting, string? select, string expected)
        {
            // Arrange
            using var stream = new MemoryStream();
            var feature = new StreamResponseBodyFeature(stream);
            HttpContext.Features.Set<IHttpResponseBodyFeature>(feature);

            // Act
            await new ConfigurationSettingResult(setting, select: select).ExecuteAsync(HttpContext);

            // Assert
            stream.Seek(0, SeekOrigin.Begin);
            using var reader = new StreamReader(stream);
            var actual = await reader.ReadToEndAsync();
            Assert.Equal(actual, expected);
        }

        [Fact]
        public async Task ExecuteAsync_ResponseStatusCode_ConfigurationSetting()
        {
            // Arrange
            var setting = new ConfigurationSetting("TestEtag", "TestKey", DateTimeOffset.UtcNow, false);

            // Act
            await new ConfigurationSettingResult(setting).ExecuteAsync(HttpContext);

            // Assert
            Assert.Equal(HttpContext.Response.StatusCode, StatusCodes.Status200OK);
        }
    }
}
