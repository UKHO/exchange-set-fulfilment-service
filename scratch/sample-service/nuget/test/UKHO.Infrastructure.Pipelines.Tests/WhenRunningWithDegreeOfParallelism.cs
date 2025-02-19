using FluentAssertions;
using UKHO.Infrastructure.Pipelines.Contexts;
using UKHO.Infrastructure.Pipelines.Nodes;
using Xunit;

namespace UKHO.Infrastructure.Pipelines.Tests
{
    public class WhenRunningWithDegreeOfParallelism
    {
        public IEnumerable<TestObjectA> GetTestObjects(int count = 100)
        {
            List<TestObjectA> testObjects = new(count);
            for (int i = 0; i < count; i++)
            {
                testObjects.Add(new TestObjectA());
            }

            return testObjects;
        }

        [Fact]
        public async Task Successful_Node_Run_Status_Is_Completed()
        {
            SimpleTestNodeA1 testNode = new();

            IEnumerable<TestObjectA> testObjectList = GetTestObjects();

            NodeResult? result = await testNode.ExecuteManyAsync(testObjectList, new ExecutionOptions { DegreeOfParallelism = 4 });

            result.Status.Should().Be(NodeResultStatus.Succeeded);
            result.Exception.Should().BeNull();
            testNode.Status.Should().Be(NodeRunStatus.Completed);
        }

        [Fact]
        public async Task Successful_Node_Run_Status_Is_Completed_When_Fewer_Subjects_Than_DegreeOfParallelism()
        {
            SimpleTestNodeA1 testNode = new();

            IEnumerable<TestObjectA> testObjectList = GetTestObjects(2);

            NodeResult? result = await testNode.ExecuteManyAsync(testObjectList, new ExecutionOptions { DegreeOfParallelism = 4 });

            result.Status.Should().Be(NodeResultStatus.Succeeded);
            result.Exception.Should().BeNull();
            testNode.Status.Should().Be(NodeRunStatus.Completed);
        }

        [Fact]
        public async Task Failed_Node_Run_Status_Is_Failed()
        {
            FailingTestNodeA testNode = new();

            IEnumerable<TestObjectA> testObjectList = GetTestObjects();

            NodeResult? result = await testNode.ExecuteManyAsync(testObjectList, new ExecutionOptions { DegreeOfParallelism = 4 });

            result.Status.Should().Be(NodeResultStatus.Failed);
            result.Exception.Should().BeNull();
            testNode.Status.Should().Be(NodeRunStatus.Completed);
        }

        [Fact]
        public async Task Faulted_Node_Throws_If_Throw_On_Error_True()
        {
            FaultingTestNodeA testNode = new();

            IEnumerable<TestObjectA> testObjectList = GetTestObjects();

            await Assert.ThrowsAsync<AggregateException>(() => testNode.ExecuteManyAsync(testObjectList, new ExecutionOptions { ThrowOnError = true, DegreeOfParallelism = 4 }));
        }

        [Fact]
        public async Task Faulted_Node_Run_Status_Is_Failed_If_Continue_On_Failure_True()
        {
            FaultingTestNodeA testNode = new();

            IEnumerable<TestObjectA> testObjectList = GetTestObjects();

            NodeResult? result = await testNode.ExecuteManyAsync(testObjectList, new ExecutionOptions { ContinueOnFailure = true, DegreeOfParallelism = 4 });

            result.Status.Should().Be(NodeResultStatus.Failed);
            result.Exception.Should().NotBeNull();
            testNode.Status.Should().Be(NodeRunStatus.Faulted);
        }
    }
}
