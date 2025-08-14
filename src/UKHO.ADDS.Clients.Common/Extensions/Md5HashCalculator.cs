using System.Security.Cryptography;

namespace UKHO.ADDS.Clients.Common.Extensions
{
    internal static class Md5HashCalculator
    {
        public static byte[] CalculateMd5(this Stream stream)
        {
            var position = stream.Position;
            stream.Seek(0, SeekOrigin.Begin);
            using (var md5 = MD5.Create())
            {
                var hash = md5.ComputeHash(stream);
                stream.Seek(position, SeekOrigin.Begin);

                return hash;
            }
        }
    }
}
