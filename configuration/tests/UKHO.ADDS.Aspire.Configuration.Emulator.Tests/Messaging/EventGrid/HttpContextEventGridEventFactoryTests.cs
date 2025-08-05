using Microsoft.AspNetCore.Http;
using NSubstitute;
using UKHO.ADDS.Aspire.Configuration.Emulator.Messaging.EventGrid;
using Xunit;

namespace UKHO.ADDS.Aspire.Configuration.Emulator.Tests.Messaging.EventGrid
{
    public class HttpContextEventGridEventFactoryTests
    {
        public HttpContextEventGridEventFactoryTests()
        {
            HttpContextAccessor = Substitute.For<IHttpContextAccessor>();

            EventGridEventFactory = new HttpContextEventGridEventFactory(HttpContextAccessor);
        }

        private HttpContextEventGridEventFactory EventGridEventFactory { get; }

        private IHttpContextAccessor HttpContextAccessor { get; }

        [Fact]
        public void Create_EventGridEventData_Data()
        {
            // Arrange
            var data = new BinaryData(new
            {
                key = "TestKey", label = "TestLabel", etag = "TestEtag"
            });

            // Act
            var eventGridEvent = EventGridEventFactory.Create("TestEventType", "TestDataVersion", data);

            // Assert
            Assert.Equal(eventGridEvent.Data.ToString(), data.ToString());
        }

        [Fact]
        public void Create_EventGridEventDataVersion_DataVersion()
        {
            // Act
            var eventGridEvent = EventGridEventFactory.Create("TestEventType", "TestDataVersion", new BinaryData(""));

            // Assert
            Assert.Equal(eventGridEvent.DataVersion, "TestDataVersion");
        }

        [Fact]
        public void Create_EventGridEventEventType_EventType()
        {
            // Act
            var eventGridEvent = EventGridEventFactory.Create("TestEventType", "TestDataVersion", new BinaryData(""));

            // Assert
            Assert.Equal(eventGridEvent.EventType, "TestEventType");
        }

        [Fact]
        public void Create_EventGridEventSubject_Subject()
        {
            // Arrange
            HttpContextAccessor.HttpContext.Returns(new DefaultHttpContext
            {
                Request =
                {
                    Scheme = "https", Host = new HostString("contoso.azconfig.io"), Path = new PathString("/kv/TestKey"), QueryString = new QueryString("?label=TestLabel")
                }
            });

            // Act
            var eventGridEvent = EventGridEventFactory.Create("TestEventType", "TestDataVersion", new BinaryData(""));

            // Assert
            Assert.Equal(eventGridEvent.Subject, "https://contoso.azconfig.io/kv/TestKey?label=TestLabel");
        }
    }
}
