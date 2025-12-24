using System.Reflection;
using UKHO.ADDS.EFS.Builder.S100.Pipelines;

namespace UKHO.ADDS.EFS.Builder.S100.UnitTests.Services
{
    [TestFixture]
    public class IncrementingCounterTests
    {
        [SetUp]
        public void Setup()
        {
            typeof(IncrementingCounter).GetField("_counter", BindingFlags.NonPublic | BindingFlags.Static)?.SetValue(null, 0);
        }

        [Test]
        public void WhenGetNextCalled_ThenReturnSequentialIncrementedValues()
        {
            var firstValue = IncrementingCounter.GetNext();
            var secondValue = IncrementingCounter.GetNext();
            var thirdValue = IncrementingCounter.GetNext();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(firstValue, Is.EqualTo("0001"));
                Assert.That(secondValue, Is.EqualTo("0002"));
                Assert.That(thirdValue, Is.EqualTo("0003"));
            }
        }

        [Test]
        public void WhenGetNextInMultithreadedEnvironment_ThenGenerateUniqueValues()
        {
            const int threadCount = 100;
            var results = new string[threadCount];
            var threads = new Thread[threadCount];

            for (var i = 0; i < threadCount; i++)
            {
                var index = i;
                threads[i] = new Thread(() => results[index] = IncrementingCounter.GetNext());
            }

            foreach (var thread in threads)
            {
                thread.Start();
            }
            foreach (var thread in threads)
            {
                thread.Join();
            }

            using (Assert.EnterMultipleScope())
            {
                Assert.That(results.Distinct().Count(), Is.EqualTo(threadCount));
                Assert.That(results.OrderBy(x => x), Is.EqualTo(Enumerable.Range(1, threadCount).Select(x => x.ToString("D4"))).AsCollection);
            }
        }
    }
}

