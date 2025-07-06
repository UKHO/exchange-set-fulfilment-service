using System.Data.Common;

namespace UKHO.ADDS.EFS.BuildRequestMonitor.Builders
{
    internal class BuildRequestMonitor
    {
        protected int ExtractPort(string connectionString, string name)
        {
            // Slight parsing hack here!

            var builder = new DbConnectionStringBuilder { ConnectionString = connectionString };

            if (builder.TryGetValue(name, out var value) && value is string endpoint && Uri.TryCreate(endpoint, UriKind.Absolute, out var uri))
            {
                return uri.Port;
            }

            return -1;
        }
    }
}
