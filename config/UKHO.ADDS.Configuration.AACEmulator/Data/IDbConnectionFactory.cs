using System.Data.Common;

namespace UKHO.ADDS.Configuration.AACEmulator.Data
{
    public interface IDbConnectionFactory
    {
        public DbConnection Create();
    }
}
