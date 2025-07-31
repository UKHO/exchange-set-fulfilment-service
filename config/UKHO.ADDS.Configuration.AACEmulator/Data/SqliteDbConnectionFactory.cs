using System.Data.Common;
using Microsoft.Data.Sqlite;

namespace UKHO.ADDS.Configuration.AACEmulator.Data
{
    public class SqliteDbConnectionFactory(IConfiguration? configuration = null) : IDbConnectionFactory
    {
        private string ConnectionString { get; } = configuration?.GetConnectionString("DefaultConnection") ?? $"Data Source={DatabasePath}";

        private static string DatabasePath { get; } = Environment.OSVersion.Platform switch
        {
            PlatformID.Win32S => Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "UKHO", "aacemulator", "emulator.db"),
            PlatformID.Win32Windows => Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "UKHO", "aacemulator", "emulator.db"),
            PlatformID.Win32NT => Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "UKHO", "aacemulator", "emulator.db"),
            PlatformID.WinCE => Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "UKHO", "aacemulator", "emulator.db"),
            PlatformID.Unix => Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "aacemulator", "emulator.db"),
            PlatformID.MacOSX => Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "aacemulator", "emulator.db"),
            _ => throw new ArgumentOutOfRangeException()
        };

        public DbConnection Create() => new SqliteConnection(ConnectionString);
    }
}
