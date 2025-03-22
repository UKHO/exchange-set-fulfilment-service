using Microsoft.AspNetCore.Components.Web;

namespace TabBlazor
{
    public partial class DropdownItem : TablerBaseComponent, IDisposable
    {
        private List<DropdownItem> subItems = new();
        private bool subMenuVisible;
        [CascadingParameter(Name = "Dropdown")] public Dropdown Dropdown { get; set; }
        [CascadingParameter(Name = "DropdownMenu")] public DropdownMenu ParentMenu { get; set; }
        [Parameter] public bool Active { get; set; }
        [Parameter] public bool Disabled { get; set; }
        [Parameter] public bool Highlight { get; set; }

        [Parameter] public RenderFragment SubMenu { get; set; }

        private bool hasSubMenu => SubMenu != null;

        protected override string ClassNames => ClassBuilder
            .Add("dropdown-item")
            .Add(BackgroundColor.GetColorClass("bg"))
            .Add(TextColor.GetColorClass("text"))
            .AddIf("active", Active)
            .AddIf("disabled", Disabled)
            .AddIf("highlight", Highlight)
            .AddIf("dropdown-toggle", hasSubMenu)
            .ToString();

        public void Dispose() => ParentMenu?.RemoveSubMenuItem(this);

        protected override void OnInitialized()
        {
            if (hasSubMenu)
            {
                ParentMenu?.AddSubMenuItem(this);
            }
        }

        private void ItemClicked(MouseEventArgs e)
        {
            if (hasSubMenu)
            {
                ToogleSubMenus(e);
            }
            else if (!hasSubMenu && Dropdown.CloseOnClick)
            {
                Dropdown.Close();
            }

            OnClick.InvokeAsync(e);
        }


        public void CloseSubMenu() => subMenuVisible = false;

        private void ToogleSubMenus(MouseEventArgs e)
        {
            var visible = subMenuVisible;
            ParentMenu?.CloseAllSubMenus();

            subMenuVisible = !visible;
        }

        private string GetWrapperClass()
        {
            if (hasSubMenu)
            {
                if (Dropdown.SubMenusDirection == DropdownDirection.Down)
                {
                    return "dropdown";
                }

                return "dropend";
            }

            return "";
        }
    }
}
