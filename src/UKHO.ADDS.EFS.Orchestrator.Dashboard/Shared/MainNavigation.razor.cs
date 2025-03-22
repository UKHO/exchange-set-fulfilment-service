using Microsoft.AspNetCore.Components;
using UKHO.ADDS.EFS.Orchestrator.Dashboard.Services;

namespace UKHO.ADDS.EFS.Orchestrator.Dashboard.Shared
{
    public partial class MainNavigation : ComponentBase
    {
        [Inject] private AppService appService { get; set; }


        protected override void OnInitialized() => appService.OnSettingsUpdated += SettingsUpdated;

        private void SettingsUpdated() => InvokeAsync(() => StateHasChanged());

        public void Dispose() => appService.OnSettingsUpdated -= SettingsUpdated;
    }
}
