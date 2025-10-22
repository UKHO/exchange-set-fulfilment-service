using System.Text.Json;
using Azure.Messaging.EventGrid;
using NSubstitute;
using UKHO.ADDS.Aspire.Configuration.Emulator.Common;
using UKHO.ADDS.Aspire.Configuration.Emulator.ConfigurationSettings;
using UKHO.ADDS.Aspire.Configuration.Emulator.Messaging.EventGrid;
using Xunit;

namespace UKHO.ADDS.Aspire.Configuration.Emulator.Tests.ConfigurationSettings
{
    public class EventGridMessagingConfigurationSettingRepositoryTests
    {
        public EventGridMessagingConfigurationSettingRepositoryTests()
        {
            EventGridEventFactory = Substitute.For<IEventGridEventFactory>();
            EventGridEventFactory
                .Create(
                    Arg.Any<string>(),
                    Arg.Any<string>(),
                    Arg.Any<BinaryData>())
                .Returns(info => new EventGridEvent(
                    "TestSubject",
                    info.ArgAt<string>(0),
                    info.ArgAt<string>(1),
                    info.ArgAt<BinaryData>(2)));

            EventGridPublisherClient = Substitute.For<EventGridPublisherClient>();

            Repository = new EventGridMessagingConfigurationSettingRepository(
                Substitute.For<IConfigurationSettingRepository>(),
                EventGridEventFactory,
                EventGridPublisherClient);
        }

        private IEventGridEventFactory EventGridEventFactory { get; }

        private EventGridPublisherClient EventGridPublisherClient { get; }

        private EventGridMessagingConfigurationSettingRepository Repository { get; }

        [Fact]
        public async Task Add_EventGridEventData_ConfigurationSetting()
        {
            // Arrange
            const string etag = "TestEtag";
            const string key = "TestKey";
            const string label = "TestLabel";
            var setting = new ConfigurationSetting(etag, key, DateTimeOffset.UtcNow, false, label);

            // Act
            await Repository.Add(setting);

            // Assert
            await EventGridPublisherClient
                .Received()
                .SendEventAsync(
                    Arg.Is<EventGridEvent>(eventGridEvent =>
                        eventGridEvent.Data.ToString() == new BinaryData(new
                        {
                            key, label, etag
                        }, (JsonSerializerOptions?)null, null).ToString()));
        }

        [Fact]
        public async Task Add_EventGridEventEventType_ConfigurationSetting()
        {
            // Arrange
            const string etag = "TestEtag";
            const string key = "TestKey";
            const string label = "TestLabel";
            var setting = new ConfigurationSetting(etag, key, DateTimeOffset.UtcNow, false, label);

            // Act
            await Repository.Add(setting);

            // Assert
            await EventGridPublisherClient
                .Received()
                .SendEventAsync(
                    Arg.Is<EventGridEvent>(eventGridEvent =>
                        eventGridEvent.EventType == EventType.ConfigurationSettingModified));
        }

        [Fact]
        public async Task Remove_EventGridEventData_ConfigurationSetting()
        {
            // Arrange
            const string etag = "TestEtag";
            const string key = "TestKey";
            const string label = "TestLabel";
            var setting = new ConfigurationSetting(etag, key, DateTimeOffset.UtcNow, false, label);

            // Act
            await Repository.Remove(setting);

            // Assert
            await EventGridPublisherClient
                .Received()
                .SendEventAsync(
                    Arg.Is<EventGridEvent>(eventGridEvent =>
                        eventGridEvent.Data.ToString() == new BinaryData(new
                        {
                            key, label, etag
                        }, (JsonSerializerOptions?)null, null).ToString()));
        }

        [Fact]
        public async Task Remove_EventGridEventEventType_ConfigurationSetting()
        {
            // Arrange
            const string etag = "TestEtag";
            const string key = "TestKey";
            const string label = "TestLabel";
            var setting = new ConfigurationSetting(etag, key, DateTimeOffset.UtcNow, false, label);

            // Act
            await Repository.Remove(setting);

            // Assert
            await EventGridPublisherClient
                .Received()
                .SendEventAsync(
                    Arg.Is<EventGridEvent>(eventGridEvent =>
                        eventGridEvent.EventType == EventType.ConfigurationSettingDeleted));
        }

        [Fact]
        public async Task Update_EventGridEventData_ConfigurationSetting()
        {
            // Arrange
            const string etag = "TestEtag";
            const string key = "TestKey";
            const string label = "TestLabel";
            var setting = new ConfigurationSetting(etag, key, DateTimeOffset.UtcNow, false, label);

            // Act
            await Repository.Update(setting);

            // Assert
            await EventGridPublisherClient
                .Received()
                .SendEventAsync(
                    Arg.Is<EventGridEvent>(eventGridEvent =>
                        eventGridEvent.Data.ToString() == new BinaryData(new
                        {
                            key, label, etag
                        }, (JsonSerializerOptions?)null, null).ToString()));
        }

        [Fact]
        public async Task Update_EventGridEventEventType_ConfigurationSetting()
        {
            // Arrange
            const string etag = "TestEtag";
            const string key = "TestKey";
            const string label = "TestLabel";
            var setting = new ConfigurationSetting(etag, key, DateTimeOffset.UtcNow, false, label);

            // Act
            await Repository.Update(setting);

            // Assert
            await EventGridPublisherClient
                .Received()
                .SendEventAsync(
                    Arg.Is<EventGridEvent>(eventGridEvent =>
                        eventGridEvent.EventType == EventType.ConfigurationSettingModified));
        }
    }
}
