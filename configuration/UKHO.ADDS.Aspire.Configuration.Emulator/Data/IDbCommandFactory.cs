using System.Data.Common;

namespace UKHO.ADDS.Aspire.Configuration.Emulator.Data;

public interface IDbCommandFactory
{
    public DbCommand Create(DbConnection connection, string? text = null, IEnumerable<DbParameter>? parameters = null);
}
