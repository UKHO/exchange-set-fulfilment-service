using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Components;
using TabBlazor.Services;

namespace UKHO.ADDS.EFS.Orchestrator.Dashboard.Components.TypeBrowsers
{
    public partial class TypeBrowser : ComponentBase
    {
        private List<MethodInfo> methods;

        private IList<PropertyView> properties;
        [Inject] private IModalService modalService { get; set; }
        [Parameter] public Type Type { get; set; }

        protected override void OnInitialized()
        {
            if (Type == null)
            {
                return;
            }

            modalService.UpdateTitle(Type.GetFriendlyName());

            properties = Type.GetProperties().Select(e => new PropertyView(e)).ToList();

            //methods = BadgeType
            //           .GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public)
            //           .Where(m => !m.IsSpecialName).ToList();

            methods = Type
                .GetMethods()
                .Where(m => !m.IsSpecialName && !m.IsVirtual && m.MethodImplementationFlags != MethodImplAttributes.InternalCall)
                .ToList();
        }
    }
}
