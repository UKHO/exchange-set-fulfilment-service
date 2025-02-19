using FluentAssertions;
using UKHO.Infrastructure.Pipelines.Contexts;
using UKHO.Infrastructure.Pipelines.Nodes;
using Xunit;

namespace UKHO.Infrastructure.Pipelines.Tests
{
    public class WhenPassingState
    {
        [Fact]
        public async Task Adding_State_To_A_Node_Is_Available_In_Following_Node()
        {
            PipelineNode<TestObjectA> pipelineNode = new PipelineNode<TestObjectA>();

            pipelineNode.AddChild(new SimpleTestNodeA1());
            pipelineNode.AddChild(new FuncNode<TestObjectA>
            {
                ExecutedFunc = ctxt =>
                {
                    ctxt.State.Foo = "Bar";
                    return Task.FromResult(NodeResultStatus.Succeeded);
                }
            });
            pipelineNode.AddChild(new FuncNode<TestObjectA>
            {
                ExecutedFunc = ctxt =>
                    ctxt.State.Foo == "Bar"
                        ? Task.FromResult(NodeResultStatus.Succeeded)
                        : Task.FromResult(NodeResultStatus.Failed)
            });

            TestObjectA testObject = new TestObjectA();
            NodeResult? result = await pipelineNode.ExecuteAsync(testObject);
            result.Status.Should().Be(NodeResultStatus.Succeeded);
        }

        [Fact]
        public async Task Adding_State_To_A_Node_Is_Available_In_Global_Context()
        {
            PipelineNode<TestObjectA> pipelineNode = new PipelineNode<TestObjectA>();

            pipelineNode.AddChild(new SimpleTestNodeA1());
            pipelineNode.AddChild(new FuncNode<TestObjectA>
            {
                ExecutedFunc = ctxt =>
                {
                    ctxt.State.Foo = "Bar";
                    return Task.FromResult(NodeResultStatus.Succeeded);
                }
            });

            TestObjectA testObject = new TestObjectA();
            ExecutionContext<TestObjectA> context = new ExecutionContext<TestObjectA>(testObject);
            NodeResult? result = await pipelineNode.ExecuteAsync(context);
            result.Status.Should().Be(NodeResultStatus.Succeeded);

            Assert.Equal("Bar", context.State.Foo);
        }

        [Fact]
        public void Accessing_Nonexistent_State_Returns_Null()
        {
            TestObjectA testObject = new TestObjectA();
            ExecutionContext<TestObjectA> context = new ExecutionContext<TestObjectA>(testObject);

            object result = context.State.NonexistentProperty;

            result.Should().BeNull();
        }
    }
}
