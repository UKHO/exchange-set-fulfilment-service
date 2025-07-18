using UKHO.ADDS.EFS.Orchestrator.UnitTests.Infrastructure;
using Xunit;
using Xunit.Abstractions;

namespace UKHO.ADDS.EFS.Orchestrator.UnitTests
{
    public class ExampleTests : OrchestratorTest
    {
        public ExampleTests(ITestOutputHelper output) : base(output) { }

        [Fact]
        public async Task It_Works_With_Sync_Arrange_And_Async_Act_And_Sync_Assert()
        {
            await Given("I create the SUT", () => new SUT())
                .When("I do something async", async sut => await sut.DoSomethingAsync())
                .Then("It should be ready", sut => Assert.True(sut.IsReady));
        }

        [Fact]
        public async Task It_Works_With_All_Async_Steps()
        {
            await Given("I create SUT async", async () =>
                {
                    await Task.Delay(10);
                    return new SUT();
                })
                .When("I do something async", async sut => await sut.DoSomethingAsync())
                .Then("It should be ready", async sut =>
                {
                    await Task.Delay(10);
                    Assert.True(sut.IsReady);
                });
        }

        [Fact]
        public async Task It_Works_With_All_Sync_Steps()
        {
            await Given("I create the SUT", () => new SUT())
                .When("I do something", sut => sut.DoSomething())
                .Then("It should be ready", sut => Assert.True(sut.IsReady));
        }
    }

    // Dummy SUT for illustration
    public class SUT
    {
        public bool IsReady { get; private set; }

        public void DoSomething()
        {
            IsReady = true;
        }

        public async Task DoSomethingAsync()
        {
            await Task.Delay(10);
            IsReady = true;
        }
    }
}
