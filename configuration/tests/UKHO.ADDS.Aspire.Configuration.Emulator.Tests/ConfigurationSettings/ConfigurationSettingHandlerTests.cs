using Microsoft.AspNetCore.Http.HttpResults;
using NSubstitute;
using UKHO.ADDS.Aspire.Configuration.Emulator.Common;
using UKHO.ADDS.Aspire.Configuration.Emulator.ConfigurationSettings;
using Xunit;

namespace UKHO.ADDS.Aspire.Configuration.Emulator.Tests.ConfigurationSettings
{
    public class ConfigurationSettingHandlerTests
    {
        public ConfigurationSettingHandlerTests() => Repository = Substitute.For<IConfigurationSettingRepository>();

        private IConfigurationSettingRepository Repository { get; }

        [Fact]
        public async Task Delete_ConfigurationSettingResult_ExistingConfigurationSetting()
        {
            // Arrange
            var settings = new List<ConfigurationSetting>
            {
                new("TestEtag", "TestKey", DateTimeOffset.UtcNow, false)
            };
            Repository.Get(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<DateTimeOffset?>(), Arg.Any<CancellationToken>()).Returns(settings.ToAsyncEnumerable());

            // Act
            var results = await ConfigurationSettingHandler.Delete(Repository, "TestKey");

            // Assert
            Assert.IsType(typeof(ConfigurationSettingResult), results.Result);
        }

        [Theory]
        [InlineData("TestEtag")]
        [InlineData("*")]
        public async Task Delete_ConfigurationSettingResult_ExistingConfigurationSettingMatchingIfMatch(string ifMatch)
        {
            // Arrange
            var settings = new List<ConfigurationSetting>
            {
                new("TestEtag", "TestKey", DateTimeOffset.UtcNow, false)
            };
            Repository.Get(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<DateTimeOffset?>(), Arg.Any<CancellationToken>()).Returns(settings.ToAsyncEnumerable());

            // Act
            var results = await ConfigurationSettingHandler.Delete(Repository, "TestKey", ifMatch: ifMatch);

            // Assert
            Assert.IsType(typeof(ConfigurationSettingResult), results.Result);
        }

        [Fact]
        public async Task Delete_ConfigurationSettingResult_ExistingConfigurationSettingNonMatchingIfNoneMatch()
        {
            // Arrange
            var settings = new List<ConfigurationSetting>
            {
                new("TestEtag", "TestKey", DateTimeOffset.UtcNow, false)
            };
            Repository.Get(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<DateTimeOffset?>(), Arg.Any<CancellationToken>()).Returns(settings.ToAsyncEnumerable());

            // Act
            var results = await ConfigurationSettingHandler.Delete(Repository, "TestKey", ifNoneMatch: "abc");

            // Assert
            Assert.IsType(typeof(ConfigurationSettingResult), results.Result);
        }

        [Fact]
        public async Task Delete_NoContentResult_NonExistingConfigurationSetting()
        {
            // Arrange
            var settings = Enumerable.Empty<ConfigurationSetting>();
            Repository.Get(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<DateTimeOffset?>(), Arg.Any<CancellationToken>()).Returns(settings.ToAsyncEnumerable());

            // Act
            var results = await ConfigurationSettingHandler.Delete(Repository, "TestKey");

            // Assert
            Assert.IsType(typeof(NoContent), results.Result);
        }

        [Fact]
        public async Task Delete_NoContentResult_NonExistingConfigurationSettingMatchingIfNoneMatch()
        {
            // Arrange
            var settings = Enumerable.Empty<ConfigurationSetting>();
            Repository.Get(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<DateTimeOffset?>(), Arg.Any<CancellationToken>()).Returns(settings.ToAsyncEnumerable());

            // Act
            var results = await ConfigurationSettingHandler.Delete(Repository, "TestKey", ifNoneMatch: "*");

            // Assert
            Assert.IsType(typeof(NoContent), results.Result);
        }

        [Fact]
        public async Task Delete_NoContentResult_NonExistingConfigurationSettingNonMatchingIfMatch()
        {
            // Arrange
            var settings = Enumerable.Empty<ConfigurationSetting>();
            Repository.Get(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<DateTimeOffset?>(), Arg.Any<CancellationToken>()).Returns(settings.ToAsyncEnumerable());

            // Act
            var results = await ConfigurationSettingHandler.Delete(Repository, "TestKey", ifMatch: "TestEtag");

            // Assert
            Assert.IsType(typeof(NoContent), results.Result);
        }

        [Theory]
        [InlineData("TestEtag")]
        [InlineData("*")]
        public async Task Delete_PreconditionFailedResult_ExistingConfigurationSettingMatchingIfNoneMatch(string ifNoneMatch)
        {
            // Arrange
            var settings = new List<ConfigurationSetting>
            {
                new("TestEtag", "TestKey", DateTimeOffset.UtcNow, false)
            };
            Repository.Get(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<DateTimeOffset?>(), Arg.Any<CancellationToken>()).Returns(settings.ToAsyncEnumerable());

            // Act
            var results = await ConfigurationSettingHandler.Delete(Repository, "TestKey", ifNoneMatch: ifNoneMatch);

            // Assert
            Assert.IsType(typeof(PreconditionFailedResult), results.Result);
        }

        [Fact]
        public async Task Delete_PreconditionFailedResult_ExistingConfigurationSettingNonMatchingIfMatch()
        {
            // Arrange
            var settings = new List<ConfigurationSetting>
            {
                new("TestEtag", "TestKey", DateTimeOffset.UtcNow, false)
            };
            Repository.Get(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<DateTimeOffset?>(), Arg.Any<CancellationToken>()).Returns(settings.ToAsyncEnumerable());

            // Act
            var results = await ConfigurationSettingHandler.Delete(Repository, "TestKey", ifMatch: "abc");

            // Assert
            Assert.IsType(typeof(PreconditionFailedResult), results.Result);
        }

        [Fact]
        public async Task Delete_PreconditionFailedResult_NonExistingConfigurationSettingMatchingIfNoneMatch()
        {
            // Arrange
            var settings = Enumerable.Empty<ConfigurationSetting>();
            Repository.Get(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<DateTimeOffset?>(), Arg.Any<CancellationToken>()).Returns(settings.ToAsyncEnumerable());

            // Act
            var results = await ConfigurationSettingHandler.Delete(Repository, "TestKey", ifNoneMatch: "TestEtag");

            // Assert
            Assert.IsType(typeof(PreconditionFailedResult), results.Result);
        }

        [Fact]
        public async Task Delete_PreconditionFailedResult_NonExistingConfigurationSettingNonMatchingIfMatch()
        {
            // Arrange
            var settings = Enumerable.Empty<ConfigurationSetting>();
            Repository.Get(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<DateTimeOffset?>(), Arg.Any<CancellationToken>()).Returns(settings.ToAsyncEnumerable());

            // Act
            var results = await ConfigurationSettingHandler.Delete(Repository, "TestKey", ifMatch: "*");

            // Assert
            Assert.IsType(typeof(PreconditionFailedResult), results.Result);
        }

        [Fact]
        public async Task Delete_ReadOnlyResult_LockedConfigurationSetting()
        {
            // Arrange
            var settings = new List<ConfigurationSetting>
            {
                new("TestEtag", "TestKey", DateTimeOffset.UtcNow, true)
            };
            Repository.Get(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<DateTimeOffset?>(), Arg.Any<CancellationToken>()).Returns(settings.ToAsyncEnumerable());

            // Act
            var results = await ConfigurationSettingHandler.Delete(Repository, "TestKey");

            // Assert
            Assert.IsType(typeof(ReadOnlyResult), results.Result);
        }

        [Theory]
        [InlineData("TestKey", "TestKey")]
        [InlineData("Test Key", "Test Key")]
        [InlineData("Test%20Key", "Test Key")]
        [InlineData(".appconfig.featureflag/ab+cd", ".appconfig.featureflag/ab+cd")]
        [InlineData(".appconfig.featureflag%2Fab+cd", ".appconfig.featureflag/ab+cd")]
        [InlineData(".appconfig.featureflag/ab%2Bcd", ".appconfig.featureflag/ab+cd")]
        [InlineData(".appconfig.featureflag%2Fab%2Bcd", ".appconfig.featureflag/ab+cd")]
        public async Task Delete_UnescapedDataString_Key(string key, string expected)
        {
            // Act
            await ConfigurationSettingHandler.Delete(Repository, key);

            // Assert
            Repository
                .Received()
                .Get(
                    Arg.Is(expected),
                    Arg.Any<string>(),
                    Arg.Any<DateTimeOffset?>(),
                    Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task Get_ConfigurationSettingResult_ExistingConfigurationSetting()
        {
            // Arrange
            var settings = new List<ConfigurationSetting>
            {
                new("TestEtag", "TestKey", DateTimeOffset.UtcNow, false)
            };
            Repository.Get(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<DateTimeOffset?>(), Arg.Any<CancellationToken>()).Returns(settings.ToAsyncEnumerable());

            // Act
            var results = await ConfigurationSettingHandler.Get(Repository, "TestKey");

            // Assert
            Assert.IsType(typeof(ConfigurationSettingResult), results.Result);
        }

        [Theory]
        [InlineData("TestEtag")]
        [InlineData("*")]
        public async Task Get_ConfigurationSettingResult_ExistingConfigurationSettingMatchingIfMatch(string ifMatch)
        {
            // Arrange
            var settings = new List<ConfigurationSetting>
            {
                new("TestEtag", "TestKey", DateTimeOffset.UtcNow, false)
            };
            Repository.Get(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<DateTimeOffset?>(), Arg.Any<CancellationToken>()).Returns(settings.ToAsyncEnumerable());

            // Act
            var results = await ConfigurationSettingHandler.Get(Repository, "TestKey", ifMatch: ifMatch);

            // Assert
            Assert.IsType(typeof(ConfigurationSettingResult), results.Result);
        }

        [Fact]
        public async Task Get_ConfigurationSettingResult_ExistingConfigurationSettingNonMatchingIfNoneMatch()
        {
            // Arrange
            var settings = new List<ConfigurationSetting>
            {
                new("TestEtag", "TestKey", DateTimeOffset.UtcNow, false)
            };
            Repository.Get(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<DateTimeOffset?>(), Arg.Any<CancellationToken>()).Returns(settings.ToAsyncEnumerable());

            // Act
            var results = await ConfigurationSettingHandler.Get(Repository, "TestKey", ifNoneMatch: "abc");

            // Assert
            Assert.IsType(typeof(ConfigurationSettingResult), results.Result);
        }

        [Fact]
        public async Task Get_NotFoundResult_NonExistingConfigurationSetting()
        {
            // Arrange
            var settings = Enumerable.Empty<ConfigurationSetting>();
            Repository.Get(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<DateTimeOffset?>(), Arg.Any<CancellationToken>()).Returns(settings.ToAsyncEnumerable());

            // Act
            var results = await ConfigurationSettingHandler.Get(Repository, "TestKey");

            // Assert
            Assert.IsType(typeof(NotFound), results.Result);
        }

        [Fact]
        public async Task Get_NotFoundResult_NonExistingConfigurationSettingMatchingIfNoneMatch()
        {
            // Arrange
            var settings = Enumerable.Empty<ConfigurationSetting>();
            Repository.Get(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<DateTimeOffset?>(), Arg.Any<CancellationToken>()).Returns(settings.ToAsyncEnumerable());

            // Act
            var results = await ConfigurationSettingHandler.Get(Repository, "TestKey", ifNoneMatch: "TestEtag");

            // Assert
            Assert.IsType(typeof(NotFound), results.Result);
        }

        [Fact]
        public async Task Get_NotFoundResult_NonExistingConfigurationSettingNonMatchingIfMatch()
        {
            // Arrange
            var settings = Enumerable.Empty<ConfigurationSetting>();
            Repository.Get(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<DateTimeOffset?>(), Arg.Any<CancellationToken>()).Returns(settings.ToAsyncEnumerable());

            // Act
            var results = await ConfigurationSettingHandler.Get(Repository, "TestKey", ifMatch: "*");

            // Assert
            Assert.IsType(typeof(NotFound), results.Result);
        }

        [Theory]
        [InlineData("TestEtag")]
        [InlineData("*")]
        public async Task Get_NotModifiedResult_ExistingConfigurationSettingMatchingIfNoneMatch(string ifNoneMatch)
        {
            // Arrange
            var settings = new List<ConfigurationSetting>
            {
                new("TestEtag", "TestKey", DateTimeOffset.UtcNow, false)
            };
            Repository.Get(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<DateTimeOffset?>(), Arg.Any<CancellationToken>()).Returns(settings.ToAsyncEnumerable());

            // Act
            var results = await ConfigurationSettingHandler.Get(Repository, "TestKey", ifNoneMatch: ifNoneMatch);

            // Assert
            Assert.IsType(typeof(NotModifiedResult), results.Result);
        }

        [Fact]
        public async Task Get_PreconditionFailedResult_ExistingConfigurationSettingNonMatchingIfMatch()
        {
            // Arrange
            var settings = new List<ConfigurationSetting>
            {
                new("TestEtag", "TestKey", DateTimeOffset.UtcNow, false)
            };
            Repository.Get(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<DateTimeOffset?>(), Arg.Any<CancellationToken>()).Returns(settings.ToAsyncEnumerable());

            // Act
            var results = await ConfigurationSettingHandler.Get(Repository, "TestKey", ifMatch: "abc");

            // Assert
            Assert.IsType(typeof(PreconditionFailedResult), results.Result);
        }

        [Theory]
        [InlineData("TestKey", "TestKey")]
        [InlineData("Test Key", "Test Key")]
        [InlineData("Test%20Key", "Test Key")]
        [InlineData(".appconfig.featureflag/ab+cd", ".appconfig.featureflag/ab+cd")]
        [InlineData(".appconfig.featureflag%2Fab+cd", ".appconfig.featureflag/ab+cd")]
        [InlineData(".appconfig.featureflag/ab%2Bcd", ".appconfig.featureflag/ab+cd")]
        [InlineData(".appconfig.featureflag%2Fab%2Bcd", ".appconfig.featureflag/ab+cd")]
        public async Task Get_UnescapedDataString_Key(string key, string expected)
        {
            // Act
            await ConfigurationSettingHandler.Get(Repository, key);

            // Assert
            Repository
                .Received()
                .Get(
                    Arg.Is(expected),
                    Arg.Any<string>(),
                    Arg.Any<DateTimeOffset?>(),
                    Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task Set_ConfigurationSettingResult_ExistingConfigurationSetting()
        {
            // Arrange
            var settings = new List<ConfigurationSetting>
            {
                new("TestEtag", "TestKey", DateTimeOffset.UtcNow, false)
            };
            Repository.Get(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<DateTimeOffset?>(), Arg.Any<CancellationToken>()).Returns(settings.ToAsyncEnumerable());
            var input = new ConfigurationSettingHandler.SetInput(null, null, null);

            // Act
            var results = await ConfigurationSettingHandler.Set(Repository, input, "TestKey");

            // Assert
            Assert.IsType(typeof(ConfigurationSettingResult), results.Result);
        }

        [Theory]
        [InlineData("TestEtag")]
        [InlineData("*")]
        public async Task Set_ConfigurationSettingResult_ExistingConfigurationSettingMatchingIfMatch(string ifMatch)
        {
            // Arrange
            var settings = new List<ConfigurationSetting>
            {
                new("TestEtag", "TestKey", DateTimeOffset.UtcNow, false)
            };
            Repository.Get(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<DateTimeOffset?>(), Arg.Any<CancellationToken>()).Returns(settings.ToAsyncEnumerable());
            var input = new ConfigurationSettingHandler.SetInput(null, null, null);

            // Act
            var results = await ConfigurationSettingHandler.Set(Repository, input, "TestKey", ifMatch: ifMatch);

            // Assert
            Assert.IsType(typeof(ConfigurationSettingResult), results.Result);
        }

        [Fact]
        public async Task Set_ConfigurationSettingResult_ExistingConfigurationSettingNonMatchingIfNoneMatch()
        {
            // Arrange
            var settings = new List<ConfigurationSetting>
            {
                new("TestEtag", "TestKey", DateTimeOffset.UtcNow, false)
            };
            Repository.Get(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<DateTimeOffset?>(), Arg.Any<CancellationToken>()).Returns(settings.ToAsyncEnumerable());
            var input = new ConfigurationSettingHandler.SetInput(null, null, null);

            // Act
            var results = await ConfigurationSettingHandler.Set(Repository, input, "TestKey", ifNoneMatch: "abc");

            // Assert
            Assert.IsType(typeof(ConfigurationSettingResult), results.Result);
        }

        [Fact]
        public async Task Set_ConfigurationSettingResult_NonExistingConfigurationSetting()
        {
            // Arrange
            var settings = Enumerable.Empty<ConfigurationSetting>();
            Repository.Get(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<DateTimeOffset?>(), Arg.Any<CancellationToken>()).Returns(settings.ToAsyncEnumerable());
            var input = new ConfigurationSettingHandler.SetInput(null, null, null);

            // Act
            var results = await ConfigurationSettingHandler.Set(Repository, input, "TestKey");

            // Assert
            Assert.IsType(typeof(ConfigurationSettingResult), results.Result);
        }

        [Fact]
        public async Task Set_ConfigurationSettingResult_NonExistingConfigurationSettingMatchingIfNoneMatch()
        {
            // Arrange
            var settings = Enumerable.Empty<ConfigurationSetting>();
            Repository.Get(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<DateTimeOffset?>(), Arg.Any<CancellationToken>()).Returns(settings.ToAsyncEnumerable());
            var input = new ConfigurationSettingHandler.SetInput(null, null, null);

            // Act
            var results = await ConfigurationSettingHandler.Set(Repository, input, "TestKey", ifNoneMatch: "*");

            // Assert
            Assert.IsType(typeof(ConfigurationSettingResult), results.Result);
        }

        [Fact]
        public async Task Set_ConfigurationSettingResult_NonExistingConfigurationSettingNonMatchingIfMatch()
        {
            // Arrange
            var settings = Enumerable.Empty<ConfigurationSetting>();
            Repository.Get(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<DateTimeOffset?>(), Arg.Any<CancellationToken>()).Returns(settings.ToAsyncEnumerable());
            var input = new ConfigurationSettingHandler.SetInput(null, null, null);

            // Act
            var results = await ConfigurationSettingHandler.Set(Repository, input, "TestKey", ifMatch: "TestEtag");

            // Assert
            Assert.IsType(typeof(ConfigurationSettingResult), results.Result);
        }

        [Theory]
        [InlineData("TestEtag")]
        [InlineData("*")]
        public async Task Set_PreconditionFailedResult_ExistingConfigurationSettingMatchingIfNoneMatch(string ifNoneMatch)
        {
            // Arrange
            var settings = new List<ConfigurationSetting>
            {
                new("TestEtag", "TestKey", DateTimeOffset.UtcNow, false)
            };
            Repository.Get(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<DateTimeOffset?>(), Arg.Any<CancellationToken>()).Returns(settings.ToAsyncEnumerable());
            var input = new ConfigurationSettingHandler.SetInput(null, null, null);

            // Act
            var results = await ConfigurationSettingHandler.Set(Repository, input, "TestKey", ifNoneMatch: ifNoneMatch);

            // Assert
            Assert.IsType(typeof(PreconditionFailedResult), results.Result);
        }

        [Fact]
        public async Task Set_PreconditionFailedResult_ExistingConfigurationSettingNonMatchingIfMatch()
        {
            // Arrange
            var settings = new List<ConfigurationSetting>
            {
                new("TestEtag", "TestKey", DateTimeOffset.UtcNow, false)
            };
            Repository.Get(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<DateTimeOffset?>(), Arg.Any<CancellationToken>()).Returns(settings.ToAsyncEnumerable());
            var input = new ConfigurationSettingHandler.SetInput(null, null, null);

            // Act
            var results = await ConfigurationSettingHandler.Set(Repository, input, "TestKey", ifMatch: "abc");

            // Assert
            Assert.IsType(typeof(PreconditionFailedResult), results.Result);
        }

        [Fact]
        public async Task Set_PreconditionFailedResult_NonExistingConfigurationSettingMatchingIfNoneMatch()
        {
            // Arrange
            var settings = Enumerable.Empty<ConfigurationSetting>();
            Repository.Get(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<DateTimeOffset?>(), Arg.Any<CancellationToken>()).Returns(settings.ToAsyncEnumerable());
            var input = new ConfigurationSettingHandler.SetInput(null, null, null);

            // Act
            var results = await ConfigurationSettingHandler.Set(Repository, input, "TestKey", ifNoneMatch: "TestEtag");

            // Assert
            Assert.IsType(typeof(PreconditionFailedResult), results.Result);
        }

        [Fact]
        public async Task Set_PreconditionFailedResult_NonExistingConfigurationSettingNonMatchingIfMatch()
        {
            // Arrange
            var settings = Enumerable.Empty<ConfigurationSetting>();
            Repository.Get(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<DateTimeOffset?>(), Arg.Any<CancellationToken>()).Returns(settings.ToAsyncEnumerable());
            var input = new ConfigurationSettingHandler.SetInput(null, null, null);

            // Act
            var results = await ConfigurationSettingHandler.Set(Repository, input, "TestKey", ifMatch: "*");

            // Assert
            Assert.IsType(typeof(PreconditionFailedResult), results.Result);
        }

        [Fact]
        public async Task Set_ReadOnlyResult_LockedConfigurationSetting()
        {
            // Arrange
            var settings = new List<ConfigurationSetting>
            {
                new("TestEtag", "TestKey", DateTimeOffset.UtcNow, true)
            };
            Repository.Get(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<DateTimeOffset?>(), Arg.Any<CancellationToken>()).Returns(settings.ToAsyncEnumerable());
            var input = new ConfigurationSettingHandler.SetInput(null, null, null);

            // Act
            var results = await ConfigurationSettingHandler.Set(Repository, input, "TestKey");

            // Assert
            Assert.IsType(typeof(ReadOnlyResult), results.Result);
        }

        [Theory]
        [InlineData("TestKey", "TestKey")]
        [InlineData("Test Key", "Test Key")]
        [InlineData("Test%20Key", "Test Key")]
        [InlineData(".appconfig.featureflag/ab+cd", ".appconfig.featureflag/ab+cd")]
        [InlineData(".appconfig.featureflag%2Fab+cd", ".appconfig.featureflag/ab+cd")]
        [InlineData(".appconfig.featureflag/ab%2Bcd", ".appconfig.featureflag/ab+cd")]
        [InlineData(".appconfig.featureflag%2Fab%2Bcd", ".appconfig.featureflag/ab+cd")]
        public async Task Set_UnescapedDataString_Key(string key, string expected)
        {
            // Arrange
            var input = new ConfigurationSettingHandler.SetInput(null, null, null);

            // Act
            await ConfigurationSettingHandler.Set(Repository, input, key);

            // Assert
            Repository
                .Received()
                .Get(
                    Arg.Is(expected),
                    Arg.Any<string>(),
                    Arg.Any<DateTimeOffset?>(),
                    Arg.Any<CancellationToken>());
        }
    }
}
