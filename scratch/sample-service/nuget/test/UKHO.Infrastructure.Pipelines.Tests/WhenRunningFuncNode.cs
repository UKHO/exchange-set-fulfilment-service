using FluentAssertions;
using UKHO.Infrastructure.Pipelines.Nodes;
using Xunit;

namespace UKHO.Infrastructure.Pipelines.Tests
{
    public class WhenRunningFuncNode
    {
        [Fact]
        public async Task Successful_FuncNode_Values_Match_Expected()
        {
            FuncNode<TestObjectA> node = new FuncNode<TestObjectA>();

            node.AddShouldExecute(context => Task.FromResult(context.Subject.TestValueInt == 0));
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
        public async Task FuncNode_With_ShouldExecute_False_Shouldnt_Run()
        {
            FuncNode<TestObjectA> node = new FuncNode<TestObjectA>();

            node.AddShouldExecute(context => Task.FromResult(context.Subject.TestValueInt == 5));
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


        [Fact]
        public async Task Can_Run_Func_Node_On_Inherited_Type()
        {
            FuncNode<TestObjectA> node = new FuncNode<TestObjectA>();

            node.AddShouldExecute(context => Task.FromResult(context.Subject.TestValueInt == 0));
            node.ExecutedFunc = context =>
            {
                context.Subject.TestValueString = "Completed";
                return Task.FromResult(NodeResultStatus.Succeeded);
            };

            TestObjectASub testObject = new TestObjectASub();
            NodeResult result = await node.ExecuteAsync(testObject);

            node.Status.Should().Be(NodeRunStatus.Completed);
            result.Status.Should().Be(NodeResultStatus.Succeeded);
            result.GetSubjectAs<TestObjectA>().TestValueString.Should().Be("Completed");
        }
    }
}
