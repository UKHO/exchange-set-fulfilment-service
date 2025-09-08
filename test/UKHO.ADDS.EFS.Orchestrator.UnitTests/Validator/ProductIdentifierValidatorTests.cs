using NUnit.Framework;
using UKHO.ADDS.EFS.Orchestrator.Validators;

namespace UKHO.ADDS.EFS.Orchestrator.UnitTests.Validator
{
    [TestFixture]
    internal class ProductIdentifierValidatorTests
    {
        [TestCase(null)]
        public void WhenProductIdentifierIsNull_ThenIsValidReturnsTrue(string? productIdentifier)
        {
            var result = ActIsValid(productIdentifier);

            Assert.That(result, Is.True);
        }

        [TestCase("")]
        [TestCase(" ")]
        [TestCase("   ")]
        [TestCase("S ")]
        [TestCase("S1 ")]
        [TestCase(" S123")]
        [TestCase("S12")]
        [TestCase("S1234")]
        [TestCase("s1234")]
        [TestCase("S12A")]
        [TestCase("A123")]
        [TestCase("s12")]
        [TestCase("S1A3")]
        [TestCase("S1@3")]
        [TestCase("S1 3")]
        [TestCase("S1-3")]
        [TestCase("S1_3")]
        [TestCase("S1.3")]
        [TestCase("S1,3")]
        [TestCase("S1/3")]
        [TestCase("S1\\3")]
        [TestCase("S1\n3")]
        [TestCase("S1\t3")]
        public void WhenProductIdentifierIsInvalid_ThenIsValidReturnsFalse(string productIdentifier)
        {
            var result = ActIsValid(productIdentifier);

            Assert.That(result, Is.False);   
        }

        [TestCase("S123")]
        [TestCase("s123")]
        public void WhenProductIdentifierIsValid_ThenIsValidReturnsTrue(string productIdentifier)
        {
            var result = ActIsValid(productIdentifier);

            Assert.That(result, Is.True);    
        }

        private bool ActIsValid(string? productIdentifier)
        {
            return ProductIdentifierValidator.IsValid(productIdentifier);
        }
    }
}
