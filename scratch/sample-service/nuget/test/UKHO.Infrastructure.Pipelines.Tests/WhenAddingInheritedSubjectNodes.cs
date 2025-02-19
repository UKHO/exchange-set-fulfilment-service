using FluentAssertions;
using UKHO.Infrastructure.Pipelines.Nodes;
using Xunit;

namespace UKHO.Infrastructure.Pipelines.Tests
{
    public class WhenAddingInheritedSubjectNodes
    {
        [Fact]
        public async Task Can_Add_Base_Type_Nodes_To_Inherited_Type_Pipeline()
        {
            SimpleTestNodeA1 testNode = new SimpleTestNodeA1();
            SimpleTestNodeASub1 testNode2 = new SimpleTestNodeASub1();

            PipelineNode<TestObjectASub> pipeline = new PipelineNode<TestObjectASub>();

            pipeline.AddChild(testNode);
            pipeline.AddChild(testNode2);

            TestObjectASub testObject = new TestObjectASub();

            NodeResult? result = await pipeline.ExecuteAsync(testObject);

            testNode.Status.Should().Be(NodeRunStatus.Completed);
            result.Status.Should().Be(NodeResultStatus.Succeeded);
        }

        [Fact]
        public async Task Can_Add_Base_Type_Pipeline_Node_To_Inherited_Type_Pipeline()
        {
            SimpleTestNodeA1 testNode = new SimpleTestNodeA1();

            PipelineNode<TestObjectA> pipeline = new PipelineNode<TestObjectA>();
            pipeline.AddChild(testNode);

            SimpleTestNodeASub1 testNode2 = new SimpleTestNodeASub1();
            PipelineNode<TestObjectASub> pipelineSub = new PipelineNode<TestObjectASub>();

            pipelineSub.AddChild(testNode2);
            pipelineSub.AddChild(pipeline);

            TestObjectASub testObject = new TestObjectASub();

            NodeResult? result = await pipeline.ExecuteAsync(testObject);

            testNode.Status.Should().Be(NodeRunStatus.Completed);
            result.Status.Should().Be(NodeResultStatus.Succeeded);
        }

        [Fact]
        public async Task ShouldExecuteFunc_On_Test_Object_Is_Evaluated()
        {
            SimpleTestNodeA1 testNode = new SimpleTestNodeA1();
            SimpleTestNodeASub1 testNode2 = new SimpleTestNodeASub1();

            testNode2.AddShouldExecute(context => Task.FromResult(context.Subject.TestValueDecimal == 1));

            PipelineNode<TestObjectASub> pipeline = new PipelineNode<TestObjectASub>();

            pipeline.AddChild(testNode);
            pipeline.AddChild(testNode2);

            TestObjectASub testObject = new TestObjectASub();

            NodeResult? result = await pipeline.ExecuteAsync(testObject);

            testNode.Status.Should().Be(NodeRunStatus.Completed);
            testNode2.Status.Should().Be(NodeRunStatus.NotRun);
        }

        [Fact]
        public async Task Can_Add_Inherited_Func_Node_To_Pipleline()
        {
            FuncNode<TestObjectA> testNode = new FuncNode<TestObjectA>();

            testNode.AddShouldExecute(context => Task.FromResult(context.Subject.TestValueInt == 0));
            testNode.ExecutedFunc = context =>
            {
                context.Subject.TestValueString = "Completed";
                return Task.FromResult(NodeResultStatus.Succeeded);
            };

            TestObjectASub testObject = new TestObjectASub();

            PipelineNode<TestObjectASub> pipeline = new PipelineNode<TestObjectASub>();
            pipeline.AddChild(testNode);

            NodeResult? result = await pipeline.ExecuteAsync(testObject);

            testNode.Status.Should().Be(NodeRunStatus.Completed);
            result.Status.Should().Be(NodeResultStatus.Succeeded);
        }
    }
}
