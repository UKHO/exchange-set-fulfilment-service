using UKHO.ADDS.Aspire.Configuration.Emulator.ConfigurationSettings;
using Xunit;

namespace UKHO.ADDS.Aspire.Configuration.Emulator.Tests.ConfigurationSettings
{
    public class ConfigurationSettingFactoryTests
    {
        public ConfigurationSettingFactoryTests() => Factory = new ConfigurationSettingFactory();

        private ConfigurationSettingFactory Factory { get; }

        [Theory]
        [InlineData("application/json", typeof(ConfigurationSetting))]
        [InlineData("application/json;charset=utf-8", typeof(ConfigurationSetting))]
        [InlineData("application/vnd.microsoft.appconfig.ff+json", typeof(FeatureFlagConfigurationSetting))]
        [InlineData("application/vnd.microsoft.appconfig.ff+json;charset=utf-8", typeof(FeatureFlagConfigurationSetting))]
        [InlineData("Invalid.Content.Type", typeof(ConfigurationSetting))]
        [InlineData(null, typeof(ConfigurationSetting))]
        public void Create_ConfigurationSetting_ContentType(string? contentType, Type expected)
        {
            // Arrange
            const string etag = "TestEtag";
            const string key = "TestKey";
            var date = DateTimeOffset.UtcNow;
            const string value = "{\"id\":\"TestId\",\"enabled\":true}";

            // Act
            var setting = Factory.Create(etag, key, date, false, null, contentType, value);

            // Assert
            Assert.IsType(expected, setting);
        }
    }
}
