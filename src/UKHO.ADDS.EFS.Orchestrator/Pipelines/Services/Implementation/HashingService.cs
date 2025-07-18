using System.Text;
using Konscious.Security.Cryptography;

namespace UKHO.ADDS.EFS.Orchestrator.Pipelines.Services.Implementation
{
    internal class HashingService : IHashingService
    {
        public string CalculateHash(string value)
        {
            // Convert the input string to UTF-8 bytes
            var inputBytes = Encoding.UTF8.GetBytes(value);

            // TODO Can remove Konscious implementation once Blake2B supported by BCL

            using var hasher = new HMACBlake2B(256);
            var hash = hasher.ComputeHash(inputBytes);

            var hex = Convert.ToHexString(hash);
            return hex;
        }
    }
}
