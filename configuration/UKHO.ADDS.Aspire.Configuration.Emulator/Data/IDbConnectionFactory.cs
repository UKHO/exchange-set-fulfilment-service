using System.Data.Common;

namespace UKHO.ADDS.Aspire.Configuration.Emulator.Data;

public interface IDbConnectionFactory
{
    public DbConnection Create();
}
