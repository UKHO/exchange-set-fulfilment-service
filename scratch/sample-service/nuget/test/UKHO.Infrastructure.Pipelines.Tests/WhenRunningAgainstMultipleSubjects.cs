using FluentAssertions;
using UKHO.Infrastructure.Pipelines.Contexts;
using UKHO.Infrastructure.Pipelines.Nodes;
using Xunit;

namespace UKHO.Infrastructure.Pipelines.Tests
{
    public class WhenRunningAgainstMultipleSubjects
    {
        [Fact]
        public async Task Successful_Node_Run_Status_Is_Completed()
        {
            SimpleTestNodeA1 testNode = new SimpleTestNodeA1();

            TestObjectA testObject1 = new TestObjectA();
            TestObjectA testObject2 = new TestObjectA();

            List<TestObjectA> testObjectList = new List<TestObjectA> { testObject1, testObject2 };

            NodeResult? result = await testNode.ExecuteManyAsync(testObjectList);

            result.Status.Should().Be(NodeResultStatus.Succeeded);
            result.Exception.Should().BeNull();
            testNode.Status.Should().Be(NodeRunStatus.Completed);
        }

        [Fact]
        public async Task Failed_Node_Run_Status_Is_Failed()
        {
            FailingTestNodeA testNode = new FailingTestNodeA();

            TestObjectA testObject1 = new TestObjectA();
            TestObjectA testObject2 = new TestObjectA();

            List<TestObjectA> testObjectList = new List<TestObjectA> { testObject1, testObject2 };

            NodeResult? result = await testNode.ExecuteManyAsync(testObjectList);

            result.Status.Should().Be(NodeResultStatus.Failed);
            result.Exception.Should().BeNull();
            testNode.Status.Should().Be(NodeRunStatus.Completed);
        }

        [Fact]
        public async Task Faulted_Node_Throws_If_Throw_On_Error_True()
        {
            FaultingTestNodeA testNode = new FaultingTestNodeA();

            TestObjectA testObject1 = new TestObjectA();
            TestObjectA testObject2 = new TestObjectA();

            List<TestObjectA> testObjectList = new List<TestObjectA> { testObject1, testObject2 };

            await Assert.ThrowsAsync<AggregateException>(() => testNode.ExecuteManyAsync(testObjectList, new ExecutionOptions { ThrowOnError = true }));
        }

        [Fact]
        public async Task Faulted_Node_Run_Status_Is_Failed_If_Continue_On_Failure_True()
        {
            FaultingTestNodeA testNode = new FaultingTestNodeA();

            TestObjectA testObject1 = new TestObjectA();
            TestObjectA testObject2 = new TestObjectA();

            List<TestObjectA> testObjectList = new List<TestObjectA> { testObject1, testObject2 };

            NodeResult? result = await testNode.ExecuteManyAsync(testObjectList, new ExecutionOptions { ContinueOnFailure = true });

            result.Status.Should().Be(NodeResultStatus.Failed);
            result.Exception.Should().NotBeNull();
            testNode.Status.Should().Be(NodeRunStatus.Faulted);
        }

        [Fact]
        public async Task Successful_Sync_Node_Run_Status_Is_Completed()
        {
            SimpleTestNodeA1 testNode = new SimpleTestNodeA1();

            TestObjectA testObject1 = new TestObjectA();
            TestObjectA testObject2 = new TestObjectA();

            List<TestObjectA> testObjectList = new List<TestObjectA> { testObject1, testObject2 };

            NodeResult? result = await testNode.ExecuteManySeriallyAsync(testObjectList);

            result.Status.Should().Be(NodeResultStatus.Succeeded);
            result.Exception.Should().BeNull();
            testNode.Status.Should().Be(NodeRunStatus.Completed);
        }

        [Fact]
        public async Task Failed_Sync_Node_Run_Status_Is_Failed()
        {
            FailingTestNodeA testNode = new FailingTestNodeA();

            TestObjectA testObject1 = new TestObjectA();
            TestObjectA testObject2 = new TestObjectA();

            List<TestObjectA> testObjectList = new List<TestObjectA> { testObject1, testObject2 };

            NodeResult? result = await testNode.ExecuteManySeriallyAsync(testObjectList);

            result.Status.Should().Be(NodeResultStatus.Failed);
            result.Exception.Should().BeNull();
            testNode.Status.Should().Be(NodeRunStatus.Completed);
        }

        [Fact]
        public async Task Faulted_Sync_Node_Throws_If_Throw_On_Error_True()
        {
            FaultingTestNodeA testNode = new FaultingTestNodeA();

            TestObjectA testObject1 = new TestObjectA();
            TestObjectA testObject2 = new TestObjectA();

            List<TestObjectA> testObjectList = new List<TestObjectA> { testObject1, testObject2 };

            await Assert.ThrowsAsync<Exception>(() => testNode.ExecuteManySeriallyAsync(testObjectList, new ExecutionOptions { ThrowOnError = true }));
        }

        [Fact]
        public async Task Faulted_Sync_Node_Run_Status_Is_Failed_If_Continue_On_Failure_True()
        {
            FaultingTestNodeA testNode = new FaultingTestNodeA();

            TestObjectA testObject1 = new TestObjectA();
            TestObjectA testObject2 = new TestObjectA();

            List<TestObjectA> testObjectList = new List<TestObjectA> { testObject1, testObject2 };

            NodeResult? result = await testNode.ExecuteManySeriallyAsync(testObjectList, new ExecutionOptions { ContinueOnFailure = true });

            result.Status.Should().Be(NodeResultStatus.Failed);
            result.Exception.Should().NotBeNull();
            testNode.Status.Should().Be(NodeRunStatus.Faulted);
        }
    }
}
