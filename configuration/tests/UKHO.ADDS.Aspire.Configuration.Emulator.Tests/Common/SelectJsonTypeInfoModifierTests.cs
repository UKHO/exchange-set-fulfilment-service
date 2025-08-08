using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using UKHO.ADDS.Aspire.Configuration.Emulator.Common;
using UKHO.ADDS.Aspire.Configuration.Emulator.ConfigurationSettings;
using Xunit;

namespace UKHO.ADDS.Aspire.Configuration.Emulator.Tests.Common
{
    public class SelectJsonTypeInfoModifierTests
    {
        // ReSharper disable once InconsistentNaming
        public static IEnumerable<object?[]> Modify_JsonTypeInfo_Names_TestCases =
        [
            [
                new[]
                {
                    nameof(ConfigurationSetting.Key)
                },
                new[]
                {
                    nameof(ConfigurationSetting.Key)
                }
            ],
            [
                new[]
                {
                    nameof(ConfigurationSetting.Key), nameof(ConfigurationSetting.Value)
                },
                new[]
                {
                    nameof(ConfigurationSetting.Key), nameof(ConfigurationSetting.Value)
                }
            ],
            [
                Array.Empty<string>(), typeof(ConfigurationSetting).GetProperties().Select(property => property.Name).ToArray()
            ],
            [
                null, typeof(ConfigurationSetting).GetProperties().Select(property => property.Name).ToArray()
            ]
        ];

        public SelectJsonTypeInfoModifierTests()
        {
            Type = JsonTypeInfo.CreateJsonTypeInfo<ConfigurationSetting>(JsonSerializerOptions.Default);

            foreach (var property in typeof(ConfigurationSetting).GetProperties())
            {
                Type.Properties.Add(Type.CreateJsonPropertyInfo(property.PropertyType, property.Name));
            }
        }

        private JsonTypeInfo Type { get; }

        [Theory]
        [MemberData(nameof(Modify_JsonTypeInfo_Names_TestCases))]
        public void Modify_JsonTypeInfo_Names(string[] names, string[] expected)
        {
            // Arrange
            var modifier = new SelectJsonTypeInfoModifier(names);

            // Act
            modifier.Modify(Type);

            // Assert
            Assert.Equal(Type.Properties.Select(property => property.Name).ToArray(), expected);
        }
    }
}
