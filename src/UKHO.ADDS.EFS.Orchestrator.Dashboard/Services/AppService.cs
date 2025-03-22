using System;

namespace UKHO.ADDS.EFS.Orchestrator.Dashboard.Services
{
    public class AppService
    {
        public Action OnSettingsUpdated;
        private readonly AppSettings settings = new();

        public AppSettings Settings => settings;

        public void SettingsUpdated() => OnSettingsUpdated.Invoke();
    }
}
