using Microsoft.AspNetCore.Http.HttpResults;
using NSubstitute;
using UKHO.ADDS.Aspire.Configuration.Emulator.Common;
using UKHO.ADDS.Aspire.Configuration.Emulator.ConfigurationSettings;
using UKHO.ADDS.Aspire.Configuration.Emulator.Locks;
using Xunit;

namespace UKHO.ADDS.Aspire.Configuration.Emulator.Tests.Locks
{
    public class LockHandlerTests
    {
        public LockHandlerTests() => Repository = Substitute.For<IConfigurationSettingRepository>();

        private IConfigurationSettingRepository Repository { get; }

        [Fact]
        public async Task Lock_ConfigurationSettingResult_ExistingConfigurationSetting()
        {
            // Arrange
            var settings = new List<ConfigurationSetting>
            {
                new("TestEtag", "TestKey", DateTimeOffset.UtcNow, false)
            };
            Repository.Get(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<DateTimeOffset?>(), Arg.Any<CancellationToken>()).Returns(settings.ToAsyncEnumerable());

            // Act
            var results = await LockHandler.Lock(Repository, "TestKey");

            // Assert
            Assert.IsType(typeof(ConfigurationSettingResult), results.Result);
        }

        [Theory]
        [InlineData("TestEtag")]
        [InlineData("*")]
        public async Task Lock_ConfigurationSettingResult_MatchingIfMatch(string ifMatch)
        {
            // Arrange
            var settings = new List<ConfigurationSetting>
            {
                new("TestEtag", "TestKey", DateTimeOffset.UtcNow, false)
            };
            Repository.Get(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<DateTimeOffset?>(), Arg.Any<CancellationToken>()).Returns(settings.ToAsyncEnumerable());

            // Act
            var results = await LockHandler.Lock(Repository, "TestKey", ifMatch: ifMatch);

            // Assert
            Assert.IsType(typeof(ConfigurationSettingResult), results.Result);
        }

        [Fact]
        public async Task Lock_ConfigurationSettingResult_NonMatchingIfNoneMatch()
        {
            // Arrange
            var settings = new List<ConfigurationSetting>
            {
                new("TestEtag", "TestKey", DateTimeOffset.UtcNow, false)
            };
            Repository.Get(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<DateTimeOffset?>(), Arg.Any<CancellationToken>()).Returns(settings.ToAsyncEnumerable());

            // Act
            var results = await LockHandler.Lock(Repository, "TestKey", ifNoneMatch: "abc");

            // Assert
            Assert.IsType(typeof(ConfigurationSettingResult), results.Result);
        }

        [Fact]
        public async Task Lock_NotFoundResult_NonExistingConfigurationSetting()
        {
            // Arrange
            var settings = Enumerable.Empty<ConfigurationSetting>();
            Repository.Get(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<DateTimeOffset?>(), Arg.Any<CancellationToken>()).Returns(settings.ToAsyncEnumerable());

            // Act
            var results = await LockHandler.Lock(Repository, "TestKey");

            // Assert
            Assert.IsType(typeof(NotFound), results.Result);
        }

        [Theory]
        [InlineData("TestEtag")]
        [InlineData("*")]
        public async Task Lock_PreconditionFailedResult_MatchingIfNoneMatch(string ifNoneMatch)
        {
            // Arrange
            var settings = new List<ConfigurationSetting>
            {
                new("TestEtag", "TestKey", DateTimeOffset.UtcNow, false)
            };
            Repository.Get(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<DateTimeOffset?>(), Arg.Any<CancellationToken>()).Returns(settings.ToAsyncEnumerable());

            // Act
            var results = await LockHandler.Lock(Repository, "TestKey", ifNoneMatch: ifNoneMatch);

            // Assert
            Assert.IsType(typeof(PreconditionFailedResult), results.Result);
        }

        [Fact]
        public async Task Lock_PreconditionFailedResult_NonMatchingIfMatch()
        {
            // Arrange
            var settings = new List<ConfigurationSetting>
            {
                new("TestEtag", "TestKey", DateTimeOffset.UtcNow, false)
            };
            Repository.Get(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<DateTimeOffset?>(), Arg.Any<CancellationToken>()).Returns(settings.ToAsyncEnumerable());

            // Act
            var results = await LockHandler.Lock(Repository, "TestKey", ifMatch: "abc");

            // Assert
            Assert.IsType(typeof(PreconditionFailedResult), results.Result);
        }

        [Fact]
        public async Task Unlock_ConfigurationSettingResult_ExistingConfigurationSetting()
        {
            // Arrange
            var settings = new List<ConfigurationSetting>
            {
                new("TestEtag", "TestKey", DateTimeOffset.UtcNow, false)
            };
            Repository.Get(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<DateTimeOffset?>(), Arg.Any<CancellationToken>()).Returns(settings.ToAsyncEnumerable());

            // Act
            var results = await LockHandler.Unlock(Repository, "TestKey");

            // Assert
            Assert.IsType(typeof(ConfigurationSettingResult), results.Result);
        }

        [Theory]
        [InlineData("TestEtag")]
        [InlineData("*")]
        public async Task Unlock_ConfigurationSettingResult_MatchingIfMatch(string ifMatch)
        {
            // Arrange
            var settings = new List<ConfigurationSetting>
            {
                new("TestEtag", "TestKey", DateTimeOffset.UtcNow, false)
            };
            Repository.Get(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<DateTimeOffset?>(), Arg.Any<CancellationToken>()).Returns(settings.ToAsyncEnumerable());

            // Act
            var results = await LockHandler.Unlock(Repository, "TestKey", ifMatch: ifMatch);

            // Assert
            Assert.IsType(typeof(ConfigurationSettingResult), results.Result);
        }

        [Fact]
        public async Task Unlock_ConfigurationSettingResult_NonMatchingIfNoneMatch()
        {
            // Arrange
            var settings = new List<ConfigurationSetting>
            {
                new("TestEtag", "TestKey", DateTimeOffset.UtcNow, false)
            };
            Repository.Get(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<DateTimeOffset?>(), Arg.Any<CancellationToken>()).Returns(settings.ToAsyncEnumerable());

            // Act
            var results = await LockHandler.Unlock(Repository, "TestKey", ifNoneMatch: "abc");

            // Assert
            Assert.IsType(typeof(ConfigurationSettingResult), results.Result);
        }

        [Fact]
        public async Task Unlock_NotFoundResult_NonExistingConfigurationSetting()
        {
            // Arrange
            var settings = Enumerable.Empty<ConfigurationSetting>();
            Repository.Get(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<DateTimeOffset?>(), Arg.Any<CancellationToken>()).Returns(settings.ToAsyncEnumerable());

            // Act
            var results = await LockHandler.Unlock(Repository, "TestKey");

            // Assert
            Assert.IsType(typeof(NotFound), results.Result);
        }

        [Theory]
        [InlineData("TestEtag")]
        [InlineData("*")]
        public async Task Unlock_PreconditionFailedResult_MatchingIfNoneMatch(string ifNoneMatch)
        {
            // Arrange
            var settings = new List<ConfigurationSetting>
            {
                new("TestEtag", "TestKey", DateTimeOffset.UtcNow, false)
            };
            Repository.Get(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<DateTimeOffset?>(), Arg.Any<CancellationToken>()).Returns(settings.ToAsyncEnumerable());

            // Act
            var results = await LockHandler.Unlock(Repository, "TestKey", ifNoneMatch: ifNoneMatch);

            // Assert
            Assert.IsType(typeof(PreconditionFailedResult), results.Result);
        }

        [Fact]
        public async Task Unlock_PreconditionFailedResult_NonMatchingIfMatch()
        {
            // Arrange
            var settings = new List<ConfigurationSetting>
            {
                new("TestEtag", "TestKey", DateTimeOffset.UtcNow, false)
            };
            Repository.Get(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<DateTimeOffset?>(), Arg.Any<CancellationToken>()).Returns(settings.ToAsyncEnumerable());

            // Act
            var results = await LockHandler.Unlock(Repository, "TestKey", ifMatch: "abc");

            // Assert
            Assert.IsType(typeof(PreconditionFailedResult), results.Result);
        }
    }
}
