using TabBlazor;

namespace UKHO.ADDS.EFS.Orchestrator.Dashboard.Services
{
    public class AppSettings
    {
        public AppSettings() => DarkMode = true;

        public bool DarkMode { get; set; }
        public NavbarDirection NavbarDirection { get; set; } = NavbarDirection.Left;
        public NavbarBackground NavbarBackground { get; set; } = NavbarBackground.Dark;
    }
}
