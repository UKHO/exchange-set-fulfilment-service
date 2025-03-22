using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using TabBlazor;
using TabBlazor.Services;
using UKHO.ADDS.EFS.Orchestrator.Dashboard.Components.TypeBrowsers;

namespace UKHO.ADDS.EFS.Orchestrator.Dashboard.Components
{
    public partial class DocsExample : ComponentBase
    {
        public List<CodeSnippet> CodeSnippets = new();
        [Inject] public TablerService TablerService { get; set; }
        [Inject] private IModalService modalService { get; set; }
        [Parameter] public string Title { get; set; }
        [Parameter] public RenderFragment ChildContent { get; set; }
        [Parameter] public RenderFragment Description { get; set; }
        [Parameter] public Type ComponentType { get; set; }

        private async Task NavigateTo(CodeSnippet codeSnippet) => await TablerService.ScrollToFragment(codeSnippet.Id.ToString());

        public void AddCodeSnippet(CodeSnippet codeSnippet)
        {
            CodeSnippets.Add(codeSnippet);
            StateHasChanged();
        }

        private async Task OpenComponentModal()
        {
            if (ComponentType != null)
            {
                var component = new RenderComponent<TypeBrowser>().Set(e => e.Type, ComponentType);
                var result = await modalService.ShowAsync("Component API", component, new ModalOptions { Size = ModalSize.Large });
            }
        }

        public void RemoveCodeSnippet(CodeSnippet codeSnippet)
        {
            CodeSnippets.Remove(codeSnippet);
            StateHasChanged();
        }
    }
}
