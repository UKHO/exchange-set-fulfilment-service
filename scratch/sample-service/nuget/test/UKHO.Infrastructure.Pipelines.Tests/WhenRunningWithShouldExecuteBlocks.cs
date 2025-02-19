using FluentAssertions;
using UKHO.Infrastructure.Pipelines.Nodes;
using Xunit;

namespace UKHO.Infrastructure.Pipelines.Tests
{
    public class WhenRunningWithShouldExecuteBlocks
    {
        [Fact]
        public async Task Node_With_ShouldExecuteBlock_Should_Run()
        {
            FuncNode<TestObjectA> node = new FuncNode<TestObjectA>();

            node.AddShouldExecuteBlock(new ShouldExecuteBlockA());
            node.ExecutedFunc = context =>
            {
                context.Subject.TestValueString = "Completed";
                return Task.FromResult(NodeResultStatus.Succeeded);
            };

            TestObjectA testObject = new TestObjectA();
            NodeResult result = await node.ExecuteAsync(testObject);

            node.Status.Should().Be(NodeRunStatus.Completed);
            result.Status.Should().Be(NodeResultStatus.Succeeded);
            result.GetSubjectAs<TestObjectA>().TestValueString.Should().Be("Completed");
        }

        [Fact]
        public async Task Node_With_ShouldExecuteBlock_False_Shouldnt_Run()
        {
            FuncNode<TestObjectA> node = new FuncNode<TestObjectA>();

            node.AddShouldExecuteBlock(new ShouldNotExecuteBlockA());
            node.ExecutedFunc = context =>
            {
                context.Subject.TestValueString = "Completed";
                return Task.FromResult(NodeResultStatus.Succeeded);
            };

            TestObjectA testObject = new TestObjectA();
            NodeResult result = await node.ExecuteAsync(testObject);

            node.Status.Should().Be(NodeRunStatus.NotRun);
            result.Status.Should().Be(NodeResultStatus.NotRun);
            result.GetSubjectAs<TestObjectA>().TestValueString.Should().BeNull();
        }
    }
}
