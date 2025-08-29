using Xunit;
using Xunit.Abstractions;

namespace UKHO.ADDS.EFS.UnitTests.Steps.Tests
{
    public class StepTests : GivenWhenThenTest
    {
        public StepTests(ITestOutputHelper output) : base(output) { }

        [Fact]
        public async Task It_Works_With_Sync_Arrange_And_Async_Act_And_Sync_Assert()
        {
            await Given("I create the SUT", () => new Sut())
                .When("I do something async", async sut => await sut.DoSomethingAsync())
                .Then("It should be ready", sut => Assert.True(sut.IsReady));
        }

        [Fact]
        public async Task It_Works_With_All_Async_Steps()
        {
            await Given("I create SUT async", async () =>
                {
                    await Task.Delay(10);
                    return new Sut();
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
            await Given("I create the SUT", () => new Sut())
                .When("I do something", sut => sut.DoSomething())
                .Then("It should be ready", sut => Assert.True(sut.IsReady));
        }
    }
}
