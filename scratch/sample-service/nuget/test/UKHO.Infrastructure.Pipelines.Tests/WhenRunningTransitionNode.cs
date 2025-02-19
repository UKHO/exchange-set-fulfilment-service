using FluentAssertions;
using UKHO.Infrastructure.Pipelines.Nodes;
using Xunit;

namespace UKHO.Infrastructure.Pipelines.Tests
{
    public class WhenRunningTransitionNode
    {
        [Fact]
        public async Task Successful_Transition_Run_Status_Is_Succeeded()
        {
            PipelineNode<TestObjectA> pipelineNode = new PipelineNode<TestObjectA>();

            pipelineNode.AddChild(new SimpleTestNodeA1());
            pipelineNode.AddChild(new SimpleTransitionNode { ChildNode = new SimpleTestNodeB1() });
            pipelineNode.AddChild(new SimpleTestNodeA2());

            TestObjectA testObject = new TestObjectA();
            NodeResult result = await pipelineNode.ExecuteAsync(testObject);

            result.Status.Should().Be(NodeResultStatus.Succeeded);
            pipelineNode.Status.Should().Be(NodeRunStatus.Completed);
        }

        [Fact]
        public async Task Failed_Transition_Node_Child_Results_In_Failed_Parent()
        {
            PipelineNode<TestObjectA> pipelineNode = new PipelineNode<TestObjectA>();

            pipelineNode.AddChild(new SimpleTestNodeA1());
            pipelineNode.AddChild(new SimpleTransitionNode { ChildNode = new FailingTestNodeB() });
            pipelineNode.AddChild(new SimpleTestNodeA2());

            TestObjectA testObject = new TestObjectA();
            NodeResult result = await pipelineNode.ExecuteAsync(testObject);

            result.Status.Should().Be(NodeResultStatus.Failed);
            pipelineNode.Status.Should().Be(NodeRunStatus.Completed);
        }

        [Fact]
        public async Task Faulted_Transition_Node_Child_Results_In_Failed_Parent_With_Exception()
        {
            PipelineNode<TestObjectA> pipelineNode = new PipelineNode<TestObjectA>();

            pipelineNode.AddChild(new SimpleTestNodeA1());
            pipelineNode.AddChild(new SimpleTransitionNode { ChildNode = new FaultingTestNodeB() });
            pipelineNode.AddChild(new SimpleTestNodeA2());

            TestObjectA testObject = new TestObjectA();
            NodeResult result = await pipelineNode.ExecuteAsync(testObject);

            result.Status.Should().Be(NodeResultStatus.Failed);
            pipelineNode.Status.Should().Be(NodeRunStatus.Completed);
            result.Exception.Should().NotBeNull();
        }

        public class SimpleTransitionNode : TransitionNode<TestObjectA, TestObjectB>
        {
            protected override Task<TestObjectB> TransitionSourceAsync(IExecutionContext<TestObjectA> sourceContext) => Task.FromResult(new TestObjectB());

            protected override Task<TestObjectA> TransitionResultAsync(IExecutionContext<TestObjectA> sourceContext, NodeResult result) => Task.FromResult(sourceContext.Subject);
        }
    }
}
