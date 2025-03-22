using System;
using Microsoft.AspNetCore.Components;
using UKHO.ADDS.EFS.Orchestrator.Dashboard.Services;

namespace UKHO.ADDS.EFS.Orchestrator.Dashboard.Shared
{
    public partial class MainLayout : LayoutComponentBase, IDisposable
    {
        [Inject] private AppService appService { get; set; }

        public void Dispose() => appService.OnSettingsUpdated -= SettingsUpdated;


        protected override void OnInitialized() => appService.OnSettingsUpdated += SettingsUpdated;

        private void SettingsUpdated() => InvokeAsync(() => StateHasChanged());
    }
}
