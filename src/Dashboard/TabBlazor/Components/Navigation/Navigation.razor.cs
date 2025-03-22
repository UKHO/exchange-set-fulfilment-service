namespace TabBlazor
{
    public partial class Navigation : NavigationBase
    {
        [Parameter] public bool Bordered { get; set; }

        [Parameter] public EventCallback<NavigationItem> OnItemClicked { get; set; }

        [Parameter] public bool ExpandOnClick { get; set; }

        protected override string ClassNames => ClassBuilder
            .Add("nav")
            .AddIf("nav-bordered", Bordered)
            .ToString();


        protected override void OnInitialized()
        {
            ExpandClick = ExpandOnClick;

            base.OnInitialized();
        }

        public override void ChildSelected(NavigationItem child) => _ = NavigationItemClicked(child);

        internal async Task NavigationItemClicked(NavigationItem item)
        {
            SelectedItem = item;
            await OnItemClicked.InvokeAsync(item);
        }

        private void OnClickOutside()
        {
            foreach (var child in Children)
            {
                child.SetExpanded(false);
            }
        }
    }
}
