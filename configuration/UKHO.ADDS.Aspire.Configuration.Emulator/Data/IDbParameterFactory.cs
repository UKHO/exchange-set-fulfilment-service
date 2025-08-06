using System.Data.Common;

namespace UKHO.ADDS.Aspire.Configuration.Emulator.Data;

public interface IDbParameterFactory
{
    public DbParameter Create<TValue>(string name, TValue? value);
}
