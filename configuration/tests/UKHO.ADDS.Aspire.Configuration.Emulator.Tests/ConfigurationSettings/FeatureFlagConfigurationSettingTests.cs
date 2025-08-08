using UKHO.ADDS.Aspire.Configuration.Emulator.ConfigurationSettings;
using Xunit;

namespace UKHO.ADDS.Aspire.Configuration.Emulator.Tests.ConfigurationSettings
{
    public class FeatureFlagConfigurationSettingTests
    {
        // ReSharper disable once InconsistentNaming
        public static IEnumerable<object?[]> ValueGetter_Value_SerializesValue_TestCases =
        [
            [
                "TestId", false, new List<FeatureFlagFilter>(), "TestDescription", "TestDisplayName", "{\"id\":\"TestId\",\"enabled\":false,\"conditions\":{\"client_filters\":[]},\"description\":\"TestDescription\",\"display_name\":\"TestDisplayName\"}"
            ],
            [
                "TestId", false, new List<FeatureFlagFilter>
                {
                    new("TestName", new Dictionary<string, object>())
                },
                "TestDescription", "TestDisplayName", "{\"id\":\"TestId\",\"enabled\":false,\"conditions\":{\"client_filters\":[{\"name\":\"TestName\",\"parameters\":{}}]},\"description\":\"TestDescription\",\"display_name\":\"TestDisplayName\"}"
            ],
            [
                "TestId", false, new List<FeatureFlagFilter>
                {
                    new("TestName", new Dictionary<string, object>
                    {
                        {
                            "TestKey", "TestValue"
                        }
                    })
                },
                "TestDescription", "TestDisplayName", "{\"id\":\"TestId\",\"enabled\":false,\"conditions\":{\"client_filters\":[{\"name\":\"TestName\",\"parameters\":{\"TestKey\":\"TestValue\"}}]},\"description\":\"TestDescription\",\"display_name\":\"TestDisplayName\"}"
            ],
            [
                "TestId", false, new List<FeatureFlagFilter>
                {
                    new("TestName", new Dictionary<string, object>
                    {
                        {
                            "TestKey1", "TestValue1"
                        },
                        {
                            "TestKey2", "TestValue2"
                        }
                    })
                },
                "TestDescription", "TestDisplayName", "{\"id\":\"TestId\",\"enabled\":false,\"conditions\":{\"client_filters\":[{\"name\":\"TestName\",\"parameters\":{\"TestKey1\":\"TestValue1\",\"TestKey2\":\"TestValue2\"}}]},\"description\":\"TestDescription\",\"display_name\":\"TestDisplayName\"}"
            ],
            [
                "TestId", false, new List<FeatureFlagFilter>
                {
                    new("TestName1", new Dictionary<string, object>()), new("TestName2", new Dictionary<string, object>())
                },
                "TestDescription", "TestDisplayName", "{\"id\":\"TestId\",\"enabled\":false,\"conditions\":{\"client_filters\":[{\"name\":\"TestName1\",\"parameters\":{}},{\"name\":\"TestName2\",\"parameters\":{}}]},\"description\":\"TestDescription\",\"display_name\":\"TestDisplayName\"}"
            ],
            [
                "TestId", false, new List<FeatureFlagFilter>
                {
                    new("TestName1", new Dictionary<string, object>
                    {
                        {
                            "TestKey1", "TestValue1"
                        }
                    }),
                    new("TestName2", new Dictionary<string, object>
                    {
                        {
                            "TestKey2", "TestValue2"
                        }
                    })
                },
                "TestDescription", "TestDisplayName", "{\"id\":\"TestId\",\"enabled\":false,\"conditions\":{\"client_filters\":[{\"name\":\"TestName1\",\"parameters\":{\"TestKey1\":\"TestValue1\"}},{\"name\":\"TestName2\",\"parameters\":{\"TestKey2\":\"TestValue2\"}}]},\"description\":\"TestDescription\",\"display_name\":\"TestDisplayName\"}"
            ],
            [
                "TestId", false, new List<FeatureFlagFilter>
                {
                    new("TestName1", new Dictionary<string, object>
                    {
                        {
                            "TestKey1", "TestValue1"
                        },
                        {
                            "TestKey2", "TestValue2"
                        }
                    }),
                    new("TestName2", new Dictionary<string, object>
                    {
                        {
                            "TestKey3", "TestValue3"
                        },
                        {
                            "TestKey4", "TestValue4"
                        }
                    })
                },
                "TestDescription", "TestDisplayName", "{\"id\":\"TestId\",\"enabled\":false,\"conditions\":{\"client_filters\":[{\"name\":\"TestName1\",\"parameters\":{\"TestKey1\":\"TestValue1\",\"TestKey2\":\"TestValue2\"}},{\"name\":\"TestName2\",\"parameters\":{\"TestKey3\":\"TestValue3\",\"TestKey4\":\"TestValue4\"}}]},\"description\":\"TestDescription\",\"display_name\":\"TestDisplayName\"}"
            ],
            [
                "TestId", false, new List<FeatureFlagFilter>(), null, null, "{\"id\":\"TestId\",\"enabled\":false,\"conditions\":{\"client_filters\":[]}}"
            ]
        ];

        [Fact]
        public void Constructor_ValueWithClientFiltersAsEmptyList_InitializesClientFiltersAsEmptyList()
        {
            // Arrange
            const string value = "{\"id\":\"TestId\",\"enabled\":false,\"conditions\":{\"client_filters\":[]}}";

            // Act
            var setting = new FeatureFlagConfigurationSetting("TestEtag", "TestKey", value, DateTimeOffset.UtcNow, false);

            // Assert
            Assert.Empty(setting.ClientFilters);
        }

        [Fact]
        public void Constructor_ValueWithClientFiltersAsNull_InitializesClientFiltersAsEmptyList()
        {
            // Arrange
            const string value = "{\"id\":\"TestId\",\"enabled\":false,\"conditions\":{\"client_filters\":null}}";

            // Act
            var setting = new FeatureFlagConfigurationSetting("TestEtag", "TestKey", value, DateTimeOffset.UtcNow, false);

            // Assert
            Assert.Empty(setting.ClientFilters);
        }

        [Fact]
        public void Constructor_ValueWithClientFiltersWithMultipleClientFilters_InitializesClientFiltersWithMultipleClientFilters()
        {
            // Arrange
            const string value = "{\"id\":\"TestId\",\"enabled\":false,\"conditions\":{\"client_filters\":[{\"name\":\"TestName1\",\"parameters\":{}},{\"name\":\"TestName2\",\"parameters\":{}}]}}";

            // Act
            var setting = new FeatureFlagConfigurationSetting("TestEtag", "TestKey", value, DateTimeOffset.UtcNow, false);

            // Assert
            Assert.Equal(setting.ClientFilters.Count, 2);
        }

        [Fact]
        public void Constructor_ValueWithClientFiltersWithSingleClientFilter_InitializesClientFiltersWithSingleClientFilter()
        {
            // Arrange
            const string value = "{\"id\":\"TestId\",\"enabled\":false,\"conditions\":{\"client_filters\":[{\"name\":\"TestName\",\"parameters\":{}}]}}";

            // Act
            var setting = new FeatureFlagConfigurationSetting("TestEtag", "TestKey", value, DateTimeOffset.UtcNow, false);

            // Assert
            Assert.Equal(setting.ClientFilters.Count, 1);
        }

        [Fact]
        public void Constructor_ValueWithClientFiltersWithSingleClientFilterWithMultipleParameters_InitializesClientFiltersWithSingleClientFilterWithMultipleParameters()
        {
            // Arrange
            var parameter1 = KeyValuePair.Create("TestKey1", "TestValue1");
            var parameter2 = KeyValuePair.Create("TestKey2", "TestValue2");
            var value = $"{{\"id\":\"TestId\",\"enabled\":false,\"conditions\":{{\"client_filters\":[{{\"name\":\"TestName\",\"parameters\":{{\"{parameter1.Key}\":\"{parameter1.Value}\",\"{parameter2.Key}\":\"{parameter2.Value}\"}}}}]}}}}";

            // Act
            var setting = new FeatureFlagConfigurationSetting("TestEtag", "TestKey", value, DateTimeOffset.UtcNow, false);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.Equal(setting.ClientFilters.ElementAt(0).Parameters.ElementAt(0).Key, parameter1.Key);
                Assert.Equal(setting.ClientFilters.ElementAt(0).Parameters.ElementAt(0).Value, parameter1.Value);
                Assert.Equal(setting.ClientFilters.ElementAt(0).Parameters.ElementAt(1).Key, parameter2.Key);
                Assert.Equal(setting.ClientFilters.ElementAt(0).Parameters.ElementAt(1).Value, parameter2.Value);
            });
        }

        [Fact]
        public void Constructor_ValueWithClientFiltersWithSingleClientFilterWithName_InitializesClientFiltersWithSingleClientFilterWithName()
        {
            // Arrange
            const string name = "TestName";
            const string value = $"{{\"id\":\"TestId\",\"enabled\":false,\"conditions\":{{\"client_filters\":[{{\"name\":\"{name}\",\"parameters\":{{}}}}]}}}}";

            // Act
            var setting = new FeatureFlagConfigurationSetting("TestEtag", "TestKey", value, DateTimeOffset.UtcNow, false);

            // Assert
            Assert.Equal(setting.ClientFilters.ElementAt(0).Name, name);
        }

        [Fact]
        public void Constructor_ValueWithClientFiltersWithSingleClientFilterWithSingleParameter_InitializesClientFiltersWithSingleClientFilterWithSingleParameter()
        {
            // Arrange
            var parameter = KeyValuePair.Create("TestKey", "TestValue");
            var value = $"{{\"id\":\"TestId\",\"enabled\":false,\"conditions\":{{\"client_filters\":[{{\"name\":\"TestName\",\"parameters\":{{\"{parameter.Key}\":\"{parameter.Value}\"}}}}]}}}}";

            // Act
            var setting = new FeatureFlagConfigurationSetting("TestEtag", "TestKey", value, DateTimeOffset.UtcNow, false);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.Equal(setting.ClientFilters.ElementAt(0).Parameters.ElementAt(0).Key, parameter.Key);
                Assert.Equal(setting.ClientFilters.ElementAt(0).Parameters.ElementAt(0).Value, parameter.Value);
            });
        }

        [Theory]
        [InlineData("\"TestDescription\"", "TestDescription")]
        [InlineData("null", null)]
        public void Constructor_ValueWithDescription_InitializesDescription(string description, string? expected)
        {
            // Arrange
            var value = $"{{\"id\":\"TestId\",\"enabled\":false,\"conditions\":{{\"client_filters\":[]}},\"description\":{description}}}";

            // Act
            var setting = new FeatureFlagConfigurationSetting("TestEtag", "TestKey", value, DateTimeOffset.UtcNow, false);

            // Assert
            Assert.Equal(setting.Description, expected);
        }

        [Theory]
        [InlineData("\"TestDisplayName\"", "TestDisplayName")]
        [InlineData("null", null)]
        public void Constructor_ValueWithDisplayName_InitializesDisplayName(string displayName, string? expected)
        {
            // Arrange
            var value = $"{{\"id\":\"TestId\",\"enabled\":false,\"conditions\":{{\"client_filters\":[]}},\"display_name\":{displayName}}}";

            // Act
            var setting = new FeatureFlagConfigurationSetting("TestEtag", "TestKey", value, DateTimeOffset.UtcNow, false);

            // Assert
            Assert.Equal(setting.DisplayName, expected);
        }

        [Theory]
        [InlineData("false", false)]
        [InlineData("true", true)]
        public void Constructor_ValueWithEnabled_InitializesEnabled(string enabled, bool expected)
        {
            // Arrange
            var value = $"{{\"id\":\"TestId\",\"enabled\":{enabled},\"conditions\":{{\"client_filters\":[]}}}}";

            // Act
            var setting = new FeatureFlagConfigurationSetting("TestEtag", "TestKey", value, DateTimeOffset.UtcNow, false);

            // Assert
            Assert.Equal(setting.Enabled, expected);
        }

        [Theory]
        [InlineData("\"TestId\"", "TestId")]
        public void Constructor_ValueWithId_InitializesId(string id, string expected)
        {
            // Arrange
            var value = $"{{\"id\":{id},\"enabled\":false,\"conditions\":{{\"client_filters\":[]}}}}";

            // Act
            var setting = new FeatureFlagConfigurationSetting("TestEtag", "TestKey", value, DateTimeOffset.UtcNow, false);

            // Assert
            Assert.Equal(setting.Id, expected);
        }

        [Theory]
        [MemberData(nameof(ValueGetter_Value_SerializesValue_TestCases))]
        public void ValueGetter_Value_SerializesValue(string id, bool enabled, ICollection<FeatureFlagFilter> clientFilters, string? description, string? displayName, string expected)
        {
            // Arrange
            var setting = new FeatureFlagConfigurationSetting(id, enabled, clientFilters, "TestEtag", "TestKey", DateTimeOffset.UtcNow, false, description, displayName);

            // Act
            var value = setting.Value;

            // Assert
            Assert.Equal(value, expected);
        }
    }
}
