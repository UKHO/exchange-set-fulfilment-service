using System.Text.RegularExpressions;
using Xunit;

namespace UKHO.ADDS.EFS.Domain.Services.UnitTests.Storage.Queues
{
    public class FakeQueueFactoryTests
    {
        private static readonly Regex NameRegex = new("^(?=.{3,63}$)(?!-)(?!.*--)[a-z0-9-]+(?<!-)$", RegexOptions.Compiled);

        public static IEnumerable<object[]> ValidNames => new[]
        {
            new object[]
            {
                "abc"
            },
            new object[]
            {
                "queue-name-1"
            },
            new object[]
            {
                "a23"
            },
            new object[]
            {
                "a" + new string('b', 61)
            } // 62
        };

        public static IEnumerable<object[]> InvalidNames => new[]
        {
            new object?[]
            {
                null
            },
            new object[]
            {
                string.Empty
            },
            new object[]
            {
                " "
            },
            new object[]
            {
                "Abc"
            },
            new object[]
            {
                "ab_"
            },
            new object[]
            {
                "-abc"
            },
            new object[]
            {
                "abc-"
            },
            new object[]
            {
                "a--b"
            },
            new object[]
            {
                "ab"
            },
            new object[]
            {
                "a" + new string('b', 63)
            } // 64
        };

        [Theory]
        [MemberData(nameof(ValidNames))]
        public void Valid_Names_Accepted(string name)
        {
            var f = new FakeQueueFactory();
            var q = f.GetQueue(name);
            Assert.NotNull(q);
            Assert.Matches(NameRegex, name);
        }

        [Theory]
        [MemberData(nameof(InvalidNames))]
        public void Invalid_Names_Throw(string name)
        {
            var f = new FakeQueueFactory();
            Assert.Throws<ArgumentException>(() => f.GetQueue(name!));
        }
    }
}
