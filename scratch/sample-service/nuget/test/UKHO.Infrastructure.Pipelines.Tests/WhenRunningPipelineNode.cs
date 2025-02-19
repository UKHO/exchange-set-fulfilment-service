using FluentAssertions;
using UKHO.Infrastructure.Pipelines.Contexts;
using UKHO.Infrastructure.Pipelines.Nodes;
using Xunit;

namespace UKHO.Infrastructure.Pipelines.Tests
{
    public class WhenRunningPipelineNode
    {
        [Fact]
        public async Task Successful_Pipeline_Run_Status_Is_Succeeded()
        {
            PipelineNode<TestObjectA> pipelineNode = new PipelineNode<TestObjectA>();

            pipelineNode.AddChild(new SimpleTestNodeA1());
            pipelineNode.AddChild(new SimpleTestNodeA2());

            TestObjectA testObject = new TestObjectA();
            NodeResult result = await pipelineNode.ExecuteAsync(testObject);

            pipelineNode.Status.Should().Be(NodeRunStatus.Completed);
        }

        [Fact]
        public async Task Pipeline_Result_Should_Equal_Initial_Subject()
        {
            PipelineNode<TestObjectA> pipelineNode = new PipelineNode<TestObjectA>();

            pipelineNode.AddChild(new SimpleTestNodeA1());
            pipelineNode.AddChild(new SimpleTestNodeA2());

            TestObjectA testObject = new TestObjectA();
            NodeResult result = await pipelineNode.ExecuteAsync(testObject);

            pipelineNode.Status.Should().Be(NodeRunStatus.Completed);
            result.Subject.Should().BeSameAs(testObject);
        }

        [Fact]
        public async Task Pipeline_Child_Result_Count_Equals_Child_Node_Count()
        {
            PipelineNode<TestObjectA> pipelineNode = new PipelineNode<TestObjectA>();

            pipelineNode.AddChild(new SimpleTestNodeA1());
            pipelineNode.AddChild(new SimpleTestNodeA2());

            TestObjectA testObject = new TestObjectA();
            NodeResult result = await pipelineNode.ExecuteAsync(testObject);

            pipelineNode.Status.Should().Be(NodeRunStatus.Completed);
            result.ChildResults.Count().Should().Be(2);
        }

        [Fact]
        public async Task Pipeline_Result_Ids_Equal_Node_Ids()
        {
            PipelineNode<TestObjectA> pipelineNode = new PipelineNode<TestObjectA> { Id = "PipelineNode1", FlowId = "Flow1" };

            pipelineNode.AddChild(new SimpleTestNodeA1 { FlowId = "Flow1" });
            pipelineNode.AddChild(new SimpleTestNodeA2 { Id = "Child2", FlowId = "Flow1" });

            TestObjectA testObject = new TestObjectA();
            NodeResult result = await pipelineNode.ExecuteAsync(testObject);

            pipelineNode.Status.Should().Be(NodeRunStatus.Completed);
            result.FlowId.Should().Be("Flow1");
            result.Id.Should().Be("PipelineNode1");
            result.ChildResults.First().Id.Should().Be("UKHO.Infrastructure.Pipelines.Tests.SimpleTestNodeA1");
            result.ChildResults.First().FlowId.Should().Be("Flow1");

            NodeResult? secondResult = result.ChildResults.FirstOrDefault(x => x.Id == "Child2");
            secondResult.Should().NotBeNull();
            secondResult.FlowId.Should().Be("Flow1");
        }

        [Fact]
        public async Task Pipeline_Run_With_Initial_Failure_Returns_Failed_Status()
        {
            PipelineNode<TestObjectA> pipelineNode = new PipelineNode<TestObjectA>();

            pipelineNode.AddChild(new FailingTestNodeA());
            pipelineNode.AddChild(new SimpleTestNodeA1());

            TestObjectA testObject = new TestObjectA();
            NodeResult result = await pipelineNode.ExecuteAsync(testObject);

            result.Status.Should().Be(NodeResultStatus.Failed);
            pipelineNode.Status.Should().Be(NodeRunStatus.Completed);
        }

        [Fact]
        public async Task Pipeline_Run_With_Failure_After_Success_Returns_Failed_Status()
        {
            PipelineNode<TestObjectA> pipelineNode = new PipelineNode<TestObjectA>();

            pipelineNode.AddChild(new SimpleTestNodeA1());
            pipelineNode.AddChild(new FailingTestNodeA());

            TestObjectA testObject = new TestObjectA();
            NodeResult result = await pipelineNode.ExecuteAsync(testObject);

            result.Status.Should().Be(NodeResultStatus.Failed);
            pipelineNode.Status.Should().Be(NodeRunStatus.Completed);
        }

        [Fact]
        public async Task Pipeline_Run_With_Initial_Failure_And_ContinueOnError_Returns_SucceededWithErrors_Status()
        {
            PipelineNode<TestObjectA> pipelineNode = new PipelineNode<TestObjectA> { LocalOptions = new ExecutionOptions { ContinueOnFailure = true } };

            pipelineNode.AddChild(new FailingTestNodeA());
            pipelineNode.AddChild(new SimpleTestNodeA1());

            TestObjectA testObject = new TestObjectA();
            NodeResult result = await pipelineNode.ExecuteAsync(testObject);

            result.Status.Should().Be(NodeResultStatus.SucceededWithErrors);
            pipelineNode.Status.Should().Be(NodeRunStatus.Completed);
        }

        [Fact]
        public async Task Pipeline_Run_With_Final_Failure_And_ContinueOnError_Returns_SucceededWithErrors_Status()
        {
            PipelineNode<TestObjectA> pipelineNode = new PipelineNode<TestObjectA> { LocalOptions = new ExecutionOptions { ContinueOnFailure = true } };

            pipelineNode.AddChild(new SimpleTestNodeA1());
            pipelineNode.AddChild(new FailingTestNodeA());

            TestObjectA testObject = new TestObjectA();
            NodeResult result = await pipelineNode.ExecuteAsync(testObject);

            result.Status.Should().Be(NodeResultStatus.SucceededWithErrors);
            pipelineNode.Status.Should().Be(NodeRunStatus.Completed);
        }

        [Fact]
        public async Task Pipeline_Run_With_Initial_Fault_Returns_Failed_Status()
        {
            PipelineNode<TestObjectA> pipelineNode = new PipelineNode<TestObjectA>();

            pipelineNode.AddChild(new FaultingTestNodeA());
            pipelineNode.AddChild(new SimpleTestNodeA1());

            TestObjectA testObject = new TestObjectA();
            NodeResult result = await pipelineNode.ExecuteAsync(testObject);

            result.Status.Should().Be(NodeResultStatus.Failed);
            pipelineNode.Status.Should().Be(NodeRunStatus.Completed);
        }

        [Fact]
        public async Task Pipeline_Run_With_Fault_After_Success_Returns_Failed_Status()
        {
            PipelineNode<TestObjectA> pipelineNode = new PipelineNode<TestObjectA>();

            pipelineNode.AddChild(new SimpleTestNodeA1());
            pipelineNode.AddChild(new FaultingTestNodeA());

            TestObjectA testObject = new TestObjectA();
            NodeResult result = await pipelineNode.ExecuteAsync(testObject);

            result.Status.Should().Be(NodeResultStatus.Failed);
            pipelineNode.Status.Should().Be(NodeRunStatus.Completed);
        }

        [Fact]
        public async Task Pipeline_Run_With_Initial_Fault_And_ContinueOnError_Returns_SucceededWithErrors_Status()
        {
            PipelineNode<TestObjectA> pipelineNode = new PipelineNode<TestObjectA> { LocalOptions = new ExecutionOptions { ContinueOnFailure = true } };

            pipelineNode.AddChild(new FaultingTestNodeA());
            pipelineNode.AddChild(new SimpleTestNodeA1());

            TestObjectA testObject = new TestObjectA();
            NodeResult result = await pipelineNode.ExecuteAsync(testObject);

            result.Status.Should().Be(NodeResultStatus.SucceededWithErrors);
            pipelineNode.Status.Should().Be(NodeRunStatus.Completed);
        }

        [Fact]
        public async Task Pipeline_Run_With_Final_Fault_And_ContinueOnError_Returns_SucceededWithErrors_Status()
        {
            PipelineNode<TestObjectA> pipelineNode = new PipelineNode<TestObjectA> { LocalOptions = new ExecutionOptions { ContinueOnFailure = true } };

            pipelineNode.AddChild(new SimpleTestNodeA1());
            pipelineNode.AddChild(new FaultingTestNodeA());

            TestObjectA testObject = new TestObjectA();
            NodeResult result = await pipelineNode.ExecuteAsync(testObject);

            result.Status.Should().Be(NodeResultStatus.SucceededWithErrors);
            pipelineNode.Status.Should().Be(NodeRunStatus.Completed);
        }

        [Fact]
        public async Task Failed_Pipeline_Run_Returns_Failed_Status()
        {
            PipelineNode<TestObjectA> pipelineNode = new PipelineNode<TestObjectA>();

            pipelineNode.AddChild(new FailingTestNodeA());
            pipelineNode.AddChild(new FailingTestNodeA());

            TestObjectA testObject = new TestObjectA();
            NodeResult result = await pipelineNode.ExecuteAsync(testObject);

            result.Status.Should().Be(NodeResultStatus.Failed);
            pipelineNode.Status.Should().Be(NodeRunStatus.Completed);
        }

        [Fact]
        public async Task Faulted_Pipeline_Run_Returns_Failed_Status()
        {
            PipelineNode<TestObjectA> pipelineNode = new PipelineNode<TestObjectA>();

            pipelineNode.AddChild(new FaultingTestNodeA());
            pipelineNode.AddChild(new FaultingTestNodeA());

            TestObjectA testObject = new TestObjectA();
            NodeResult result = await pipelineNode.ExecuteAsync(testObject);

            result.Status.Should().Be(NodeResultStatus.Failed);
            pipelineNode.Status.Should().Be(NodeRunStatus.Completed);
        }

        [Fact]
        public async Task Successful_Pipeline_Result_Matches_Expectations()
        {
            PipelineNode<TestObjectA> pipelineNode = new PipelineNode<TestObjectA>();

            pipelineNode.AddChild(new SimpleTestNodeA1());
            pipelineNode.AddChild(new SimpleTestNodeA2());

            TestObjectA testObject = new TestObjectA();
            NodeResult result = await pipelineNode.ExecuteAsync(testObject);

            result.Status.Should().Be(NodeResultStatus.Succeeded);
            testObject.TestValueString.Should().Be("Completed");
            testObject.TestValueInt.Should().Be(100);
        }

        [Fact]
        public async Task Nested_Pipeline_Results_Contain_All_Child_Results()
        {
            PipelineNode<TestObjectA> pipelineNode = new PipelineNode<TestObjectA>();
            PipelineNode<TestObjectA> pipelineNode2 = new PipelineNode<TestObjectA>();

            pipelineNode.AddChild(pipelineNode2);
            pipelineNode2.AddChild(new SimpleTestNodeA2());
            pipelineNode2.AddChild(new SimpleTestNodeA2());

            TestObjectA testObject = new TestObjectA();
            NodeResult result = await pipelineNode.ExecuteAsync(testObject);

            result.ChildResults.Count().Should().Be(1);
            result.ChildResults.First().ChildResults.Count().Should().Be(2);
        }
    }
}
