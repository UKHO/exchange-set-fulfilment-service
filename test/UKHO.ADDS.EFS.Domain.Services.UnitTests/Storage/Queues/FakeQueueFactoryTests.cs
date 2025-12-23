using System.Text.RegularExpressions;
using Xunit;

namespace UKHO.ADDS.EFS.Domain.Services.UnitTests.Storage.Queues
{
    public partial class FakeQueueFactoryTests
    {
        public static IEnumerable<object[]> ValidNames =>
        [
            [
                "abc"
            ],
            [
                "queue-name-1"
            ],
            [
                "a23"
            ],
            [
                "a" + new string('b', 61)
            ]
        ];

        public static IEnumerable<object[]> InvalidNames =>
        [
            [
                null!
            ],
            [
                string.Empty
            ],
            [
                " "
            ],
            [
                "Abc"
            ],
            [
                "ab_"
            ],
            [
                "-abc"
            ],
            [
                "abc-"
            ],
            [
                "a--b"
            ],
            [
                "ab"
            ],
            [
                "a" + new string('b', 63)
            ]
        ];

        [Theory]
        [MemberData(nameof(ValidNames))]
        public void Valid_Names_Accepted(string name)
        {
            var f = new FakeQueueFactory();
            var q = f.GetQueue(name);
            Assert.NotNull(q);
            Assert.Matches(NameRegex(), name);
        }

        [Theory]
        [MemberData(nameof(InvalidNames))]
        public void Invalid_Names_Throw(string name)
        {
            var f = new FakeQueueFactory();
            Assert.Throws<ArgumentException>(() => f.GetQueue(name!));
        }

        [GeneratedRegex("^(?=.{3,63}$)(?!-)(?!.*--)[a-z0-9-]+(?<!-)$", RegexOptions.Compiled)]
        private static partial Regex NameRegex();
    }
}
