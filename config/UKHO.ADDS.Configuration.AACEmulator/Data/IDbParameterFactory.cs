using System.Data.Common;

namespace UKHO.ADDS.Configuration.AACEmulator.Data
{
    public interface IDbParameterFactory
    {
        public DbParameter Create<TValue>(string name, TValue? value);
    }
}
