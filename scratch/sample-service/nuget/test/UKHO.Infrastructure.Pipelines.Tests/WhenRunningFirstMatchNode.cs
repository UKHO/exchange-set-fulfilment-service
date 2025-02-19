using FluentAssertions;
using UKHO.Infrastructure.Pipelines.Nodes;
using Xunit;

namespace UKHO.Infrastructure.Pipelines.Tests
{
    public class WhenRunningFirstMatchNode
    {
        [Fact]
        public async Task Successful_FirstMatch_Node_Runs_First_Node_And_Not_Second_Node_When_Matched()
        {
            FirstMatchNode<TestObjectA> matchNode = new FirstMatchNode<TestObjectA>();

            SimpleTestNodeA1 firstNode = new SimpleTestNodeA1();
            matchNode.AddChild(firstNode);

            SimpleTestNodeA2 secondNode = new SimpleTestNodeA2();
            matchNode.AddChild(secondNode);

            TestObjectA testObject = new TestObjectA();
            NodeResult result = await matchNode.ExecuteAsync(testObject);

            matchNode.Status.Should().Be(NodeRunStatus.Completed);
            firstNode.Status.Should().Be(NodeRunStatus.Completed);
            secondNode.Status.Should().Be(NodeRunStatus.NotRun);
            result.Status.Should().Be(NodeResultStatus.Succeeded);

            testObject.TestValueString.Should().Be("Completed");
            testObject.TestValueInt.Should().Be(0);
        }

        [Fact]
        public async Task Successful_FirstMatch_Node_Runs_Second_Node_When_First_Not_Matched()
        {
            FirstMatchNode<TestObjectA> matchNode = new FirstMatchNode<TestObjectA>();

            SimpleTestNodeA1 firstNode = new SimpleTestNodeA1(false);
            matchNode.AddChild(firstNode);

            SimpleTestNodeA2 secondNode = new SimpleTestNodeA2();
            matchNode.AddChild(secondNode);

            TestObjectA testObject = new TestObjectA();
            NodeResult result = await matchNode.ExecuteAsync(testObject);

            matchNode.Status.Should().Be(NodeRunStatus.Completed);
            firstNode.Status.Should().Be(NodeRunStatus.NotRun);
            secondNode.Status.Should().Be(NodeRunStatus.Completed);
            result.Status.Should().Be(NodeResultStatus.Succeeded);

            testObject.TestValueString.Should().BeNull();
            testObject.TestValueInt.Should().Be(100);
        }

        [Fact]
        public async Task FirstMatch_Node_Fails_When_Selected_Node_Fails()
        {
            FirstMatchNode<TestObjectA> matchNode = new FirstMatchNode<TestObjectA>();

            FailingTestNodeA firstNode = new FailingTestNodeA();
            matchNode.AddChild(firstNode);

            SimpleTestNodeA2 secondNode = new SimpleTestNodeA2();
            matchNode.AddChild(secondNode);

            TestObjectA testObject = new TestObjectA();
            NodeResult result = await matchNode.ExecuteAsync(testObject);

            matchNode.Status.Should().Be(NodeRunStatus.Completed);
            firstNode.Status.Should().Be(NodeRunStatus.Completed);
            secondNode.Status.Should().Be(NodeRunStatus.NotRun);
            result.Status.Should().Be(NodeResultStatus.Failed);

            testObject.TestValueString.Should().Be("Failed");
            testObject.TestValueInt.Should().Be(0);
        }

        [Fact]
        public async Task FirstMatch_Node_Fails_When_Selected_Node_Faults()
        {
            FirstMatchNode<TestObjectA> matchNode = new FirstMatchNode<TestObjectA>();

            FaultingTestNodeA firstNode = new FaultingTestNodeA();
            matchNode.AddChild(firstNode);

            SimpleTestNodeA2 secondNode = new SimpleTestNodeA2();
            matchNode.AddChild(secondNode);

            TestObjectA testObject = new TestObjectA();
            NodeResult result = await matchNode.ExecuteAsync(testObject);

            matchNode.Status.Should().Be(NodeRunStatus.Completed);
            firstNode.Status.Should().Be(NodeRunStatus.Faulted);
            secondNode.Status.Should().Be(NodeRunStatus.NotRun);
            result.Status.Should().Be(NodeResultStatus.Failed);

            testObject.TestValueString.Should().Be("Faulted");
            testObject.TestValueInt.Should().Be(0);
        }
    }
}
