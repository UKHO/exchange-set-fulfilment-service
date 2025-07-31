using System.Data.Common;

namespace UKHO.ADDS.Configuration.AACEmulator.Data
{
    public interface IDbCommandFactory
    {
        public DbCommand Create(DbConnection connection, string? text = null, IEnumerable<DbParameter>? parameters = null);
    }
}
