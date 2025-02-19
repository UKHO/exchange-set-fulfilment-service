using FluentAssertions;
using UKHO.Infrastructure.Pipelines.Contexts;
using UKHO.Infrastructure.Pipelines.Nodes;
using Xunit;

namespace UKHO.Infrastructure.Pipelines.Tests
{
    public class WhenChangingTheSubject
    {
        [Fact]
        public async Task Node_May_Change_Context_Subject()
        {
            SubjectChangingNode1 testNode = new SubjectChangingNode1();
            TestObjectA testObject = new TestObjectA();
            ExecutionContext<TestObjectA> context = new ExecutionContext<TestObjectA>(testObject);

            NodeResult? result = await testNode.ExecuteAsync(context);

            result.Status.Should().Be(NodeResultStatus.Succeeded);

            result.Subject.Should().BeSameAs(context.Subject);
            result.Subject.Should().NotBeSameAs(testObject);
            result.GetSubjectAs<TestObjectA>().TestValueString.Should().Be("New Instance");
        }

        [Fact]
        public async Task Pipeline_Node_Results_Following_Subject_Change_Node_Return_Changed_Subject()
        {
            PipelineNode<TestObjectA> pipelineNode = new PipelineNode<TestObjectA>();

            SimpleTestNodeA1 node1 = new SimpleTestNodeA1();
            SubjectChangingNode1 node2 = new SubjectChangingNode1();
            SimpleTestNodeA2 node3 = new SimpleTestNodeA2();

            pipelineNode.AddChild(node1);
            pipelineNode.AddChild(node2);
            pipelineNode.AddChild(node3);

            TestObjectA testObject = new TestObjectA();
            NodeResult result = await pipelineNode.ExecuteAsync(testObject);

            pipelineNode.Status.Should().Be(NodeRunStatus.Completed);

            List<NodeResult> childResults = result.ChildResults.ToList();

            childResults[0].Subject.Should().BeSameAs(testObject);
            childResults[1].Subject.Should().NotBeSameAs(testObject);
            childResults[2].Subject.Should().NotBeSameAs(testObject);
            childResults[1].Subject.Should().Be(childResults[2].Subject);
        }

        [Fact]
        public async Task Pipeline_Overall_Result_Subject_Equals_Changed_Subject()
        {
            PipelineNode<TestObjectA> pipelineNode = new PipelineNode<TestObjectA>();

            SimpleTestNodeA1 node1 = new SimpleTestNodeA1();
            SubjectChangingNode1 node2 = new SubjectChangingNode1();
            SimpleTestNodeA2 node3 = new SimpleTestNodeA2();

            pipelineNode.AddChild(node1);
            pipelineNode.AddChild(node2);
            pipelineNode.AddChild(node3);

            TestObjectA testObject = new TestObjectA();
            NodeResult result = await pipelineNode.ExecuteAsync(testObject);

            pipelineNode.Status.Should().Be(NodeRunStatus.Completed);

            List<NodeResult> childResults = result.ChildResults.ToList();

            result.Subject.Should().NotBeSameAs(testObject);
            result.Subject.Should().BeSameAs(childResults[1].Subject);
        }

        [Fact]
        public async Task Pipeline_Overall_Result_Subject_Equals_Last_Changed_Subject()
        {
            PipelineNode<TestObjectA> pipelineNode = new PipelineNode<TestObjectA>();

            SimpleTestNodeA1 node1 = new SimpleTestNodeA1();
            SubjectChangingNode1 node2 = new SubjectChangingNode1();
            SimpleTestNodeA2 node3 = new SimpleTestNodeA2();
            SubjectChangingNode1 node4 = new SubjectChangingNode1();

            pipelineNode.AddChild(node1);
            pipelineNode.AddChild(node2);
            pipelineNode.AddChild(node3);
            pipelineNode.AddChild(node4);

            TestObjectA testObject = new TestObjectA();
            NodeResult result = await pipelineNode.ExecuteAsync(testObject);

            pipelineNode.Status.Should().Be(NodeRunStatus.Completed);

            List<NodeResult> childResults = result.ChildResults.ToList();

            result.Subject.Should().NotBeSameAs(testObject);
            result.Subject.Should().NotBeSameAs(childResults[1].Subject);
            result.Subject.Should().NotBeSameAs(childResults[2].Subject);
            result.Subject.Should().BeSameAs(childResults[3].Subject);
        }

        [Fact]
        public async Task Group_Overall_Result_Subject_Equals_Changed_Subject()
        {
            GroupNode<TestObjectA> groupNode = new GroupNode<TestObjectA>();

            SimpleTestNodeA1 node1 = new SimpleTestNodeA1();
            SubjectChangingNode1 node2 = new SubjectChangingNode1();
            SimpleTestNodeA2 node3 = new SimpleTestNodeA2();

            groupNode.AddChild(node1);
            groupNode.AddChild(node2);
            groupNode.AddChild(node3);

            TestObjectA testObject = new TestObjectA();
            NodeResult result = await groupNode.ExecuteAsync(testObject);

            groupNode.Status.Should().Be(NodeRunStatus.Completed);

            List<NodeResult> childResults = result.ChildResults.ToList();

            result.Subject.Should().NotBeSameAs(testObject);
            result.Subject.Should().BeSameAs(childResults[1].Subject);
        }

        [Fact]
        public async Task Group_Overall_Result_Subject_Equals_Last_Changed_Subject()
        {
            GroupNode<TestObjectA> groupNode = new GroupNode<TestObjectA>();

            SimpleTestNodeA1 node1 = new SimpleTestNodeA1();
            SubjectChangingNode1 node2 = new SubjectChangingNode1();
            SimpleTestNodeA2 node3 = new SimpleTestNodeA2();
            SubjectChangingNode1 node4 = new SubjectChangingNode1();

            groupNode.AddChild(node1);
            groupNode.AddChild(node2);
            groupNode.AddChild(node3);
            groupNode.AddChild(node4);

            TestObjectA testObject = new TestObjectA();
            NodeResult result = await groupNode.ExecuteAsync(testObject);

            groupNode.Status.Should().Be(NodeRunStatus.Completed);

            List<NodeResult> childResults = result.ChildResults.ToList();

            result.Subject.Should().NotBeSameAs(testObject);
            result.Subject.Should().NotBeSameAs(childResults[1].Subject);
            result.Subject.Should().NotBeSameAs(childResults[2].Subject);
            result.Subject.Should().BeSameAs(childResults[3].Subject);
        }
    }
}
