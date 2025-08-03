using Microsoft.Data.Sqlite;
using UKHO.ADDS.Aspire.Configuration.Emulator.ConfigurationSettings;
using UKHO.ADDS.Aspire.Configuration.Emulator.Data;
using Xunit;

namespace UKHO.ADDS.Aspire.Configuration.Emulator.Tests.Data
{
    public class SqliteDbParameterFactoryTests
    {
        // ReSharper disable once InconsistentNaming
        public static IEnumerable<object?[]> Create_DbParameter_ConfigurationSettingContentType_TestCases =
        [
            [
                "TestContentType", "TestContentType"
            ],
            [
                null, DBNull.Value
            ]
        ];

        // ReSharper disable once InconsistentNaming
        public static IEnumerable<object?[]> Create_DbParameter_ConfigurationSettingLabel_TestCases =
        [
            [
                "TestLabel", "TestLabel"
            ],
            [
                null, DBNull.Value
            ]
        ];

        // ReSharper disable once InconsistentNaming
        public static IEnumerable<object?[]> Create_DbParameter_ConfigurationSettingTags_TestCases =
        [
            [
                new Dictionary<string, string>
                {
                    {
                        "TestKey", "TestValue"
                    }
                },
                "{\"TestKey\":\"TestValue\"}"
            ],
            [
                null, DBNull.Value
            ]
        ];

        // ReSharper disable once InconsistentNaming
        public static IEnumerable<object?[]> Create_DbParameter_ConfigurationSettingValue_TestCases =
        [
            [
                "TestValue", "TestValue"
            ],
            [
                null, DBNull.Value
            ]
        ];

        // ReSharper disable once InconsistentNaming
        public static IEnumerable<object?[]> Create_DbParameterType_NameAndValue_TestCases =
        [
            [
                true, SqliteType.Integer
            ],
            [
                false, SqliteType.Integer
            ],
            [
                DateTime.Parse("2023-10-01T00:00:00+00:00"), SqliteType.Text
            ],
            [
                DateTime.Parse("2023-10-01T12:00:00+12:00"), SqliteType.Text
            ],
            [
                DateTimeOffset.Parse("2023-10-01T00:00:00+00:00"), SqliteType.Text
            ],
            [
                DateTimeOffset.Parse("2023-10-01T12:00:00+12:00"), SqliteType.Text
            ],
            [
                new Dictionary<string, object?>
                {
                    {
                        "TestKey", "TestValue"
                    }
                },
                SqliteType.Text
            ],
            [
                new Dictionary<string, object?>
                {
                    {
                        "TestKey", null
                    }
                },
                SqliteType.Text
            ],
            [
                0, SqliteType.Integer
            ],
            [
                null, SqliteType.Text
            ],
            [
                "Hello World", SqliteType.Text
            ]
        ];

        // ReSharper disable once InconsistentNaming
        public static IEnumerable<object?[]> Create_DbParameterValue_NameAndValue_TestCases =
        [
            [
                true, 1
            ],
            [
                false, 0
            ],
            [
                DateTime.Parse("2023-10-01T00:00:00+00:00"), "2023-10-01 00:00:00"
            ],
            [
                DateTime.Parse("2023-10-01T12:00:00+12:00"), "2023-10-01 00:00:00"
            ],
            [
                DateTimeOffset.Parse("2023-10-01T00:00:00+00:00"), "2023-10-01 00:00:00"
            ],
            [
                DateTimeOffset.Parse("2023-10-01T12:00:00+12:00"), "2023-10-01 00:00:00"
            ],
            [
                new Dictionary<string, object?>
                {
                    {
                        "TestKey", "TestValue"
                    }
                },
                "{\"TestKey\":\"TestValue\"}"
            ],
            [
                new Dictionary<string, object?>
                {
                    {
                        "TestKey", null
                    }
                },
                "{\"TestKey\":null}"
            ],
            [
                0, 0
            ],
            [
                null, DBNull.Value
            ],
            [
                "Hello World", "Hello World"
            ]
        ];

        [Theory]
        [MemberData(nameof(Create_DbParameter_ConfigurationSettingContentType_TestCases))]
        public void Create_DbParameter_ConfigurationSettingContentType(string? contentType, object expected)
        {
            // Arrange
            var factory = new SqliteDbParameterFactory();
            var setting = new ConfigurationSetting("TestEtag", "TestKey", DateTimeOffset.UtcNow, false, contentType: contentType);

            // Act
            var parameter = (SqliteParameter)factory.Create("TestName", setting.ContentType);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.Equal(parameter.SqliteType, SqliteType.Text);
                Assert.Equal(parameter.Value, expected);
            });
        }

        [Fact]
        public void Create_DbParameter_ConfigurationSettingEtag()
        {
            // Arrange
            var factory = new SqliteDbParameterFactory();
            const string etag = "TestEtag";
            var setting = new ConfigurationSetting(etag, "TestKey", DateTimeOffset.UtcNow, false);

            // Act
            var parameter = (SqliteParameter)factory.Create("TestName", setting.Etag);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.Equal(parameter.SqliteType, SqliteType.Text);
                Assert.Equal(parameter.Value, etag);
            });
        }

        [Fact]
        public void Create_DbParameter_ConfigurationSettingKey()
        {
            // Arrange
            var factory = new SqliteDbParameterFactory();
            const string key = "TestKey";
            var setting = new ConfigurationSetting("TestEtag", key, DateTimeOffset.UtcNow, false);

            // Act
            var parameter = (SqliteParameter)factory.Create("TestName", setting.Key);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.Equal(parameter.SqliteType, SqliteType.Text);
                Assert.Equal(parameter.Value, key);
            });
        }

        [Theory]
        [MemberData(nameof(Create_DbParameter_ConfigurationSettingLabel_TestCases))]
        public void Create_DbParameter_ConfigurationSettingLabel(string? label, object expected)
        {
            // Arrange
            var factory = new SqliteDbParameterFactory();
            var setting = new ConfigurationSetting("TestEtag", "TestKey", DateTimeOffset.UtcNow, false, label);

            // Act
            var parameter = (SqliteParameter)factory.Create("TestName", setting.Label);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.Equal(parameter.SqliteType, SqliteType.Text);
                Assert.Equal(parameter.Value, expected);
            });
        }

        [Fact]
        public void Create_DbParameter_ConfigurationSettingLastModified()
        {
            // Arrange
            var factory = new SqliteDbParameterFactory();
            var lastModified = DateTimeOffset.UtcNow;
            var setting = new ConfigurationSetting("TestEtag", "TestKey", lastModified, false);

            // Act
            var parameter = (SqliteParameter)factory.Create("TestName", setting.LastModified);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.Equal(parameter.SqliteType, SqliteType.Text);
                Assert.Equal(parameter.Value, lastModified.UtcDateTime.ToString("yyyy-MM-dd HH:mm:ss"));
            });
        }

        [Theory]
        [InlineData(0, false)]
        [InlineData(1, true)]
        public void Create_DbParameter_ConfigurationSettingLocked(int expected, bool locked)
        {
            // Arrange
            var factory = new SqliteDbParameterFactory();
            var setting = new ConfigurationSetting("TestEtag", "TestKey", DateTimeOffset.UtcNow, locked);

            // Act
            var parameter = (SqliteParameter)factory.Create("TestName", setting.Locked);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.Equal(parameter.SqliteType, SqliteType.Integer);
                Assert.Equal(parameter.Value, expected);
            });
        }

        [Theory]
        [MemberData(nameof(Create_DbParameter_ConfigurationSettingTags_TestCases))]
        public void Create_DbParameter_ConfigurationSettingTags(IDictionary<string, string>? tags, object expected)
        {
            // Arrange
            var factory = new SqliteDbParameterFactory();
            var setting = new ConfigurationSetting("TestEtag", "TestKey", DateTimeOffset.UtcNow, false, tags: tags);

            // Act
            var parameter = (SqliteParameter)factory.Create("TestName", setting.Tags);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.Equal(parameter.SqliteType, SqliteType.Text);
                Assert.Equal(parameter.Value, expected);
            });
        }

        [Theory]
        [MemberData(nameof(Create_DbParameter_ConfigurationSettingValue_TestCases))]
        public void Create_DbParameter_ConfigurationSettingValue(string? value, object expected)
        {
            // Arrange
            var factory = new SqliteDbParameterFactory();
            var setting = new ConfigurationSetting("TestEtag", "TestKey", DateTimeOffset.UtcNow, false, value: value);

            // Act
            var parameter = (SqliteParameter)factory.Create("TestName", setting.Value);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.Equal(parameter.SqliteType, SqliteType.Text);
                Assert.Equal(parameter.Value, expected);
            });
        }

        [Fact]
        public void Create_DbParameterName_NameAndValue()
        {
            // Arrange
            var factory = new SqliteDbParameterFactory();

            // Act
            var parameter = (SqliteParameter)factory.Create("TestName", "TestValue");

            // Assert
            Assert.Equal(parameter.ParameterName, "TestName");
        }

        [Theory]
        [MemberData(nameof(Create_DbParameterType_NameAndValue_TestCases))]
        public void Create_DbParameterType_NameAndValue(object? value, SqliteType expected)
        {
            // Arrange
            var factory = new SqliteDbParameterFactory();

            // Act
            var parameter = (SqliteParameter)factory.Create("TestName", value);

            // Assert
            Assert.Equal(parameter.SqliteType, expected);
        }

        [Theory]
        [MemberData(nameof(Create_DbParameterValue_NameAndValue_TestCases))]
        public void Create_DbParameterValue_NameAndValue(object? value, object expected)
        {
            // Arrange
            var factory = new SqliteDbParameterFactory();

            // Act
            var parameter = (SqliteParameter)factory.Create("TestName", value);

            // Assert
            Assert.Equal(parameter.Value, expected);
        }

        [Fact]
        public void Create_ArgumentOutOfRangeException_NonSupportedType()
        {
            // Arrange
            var factory = new SqliteDbParameterFactory();

            // Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => factory.Create("TestName", 0.0));
        }
    }
}
