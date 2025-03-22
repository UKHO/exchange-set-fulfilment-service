namespace TabBlazor
{
    public partial class NavbarMenu : TablerBaseComponent
    {
        [CascadingParameter(Name = "Navbar")] private Navbar Navbar { get; set; }

        private bool isExpanded => Navbar.IsExpanded;
        protected string HtmlTag => "div";

        protected override string ClassNames => ClassBuilder
            .Add("navbar-collapse")
            .AddIf("collapse", !isExpanded)
            .ToString();

        private string menuCollapse => isExpanded ? "" : "collapse";


        public void ToogleExpanded() => Navbar.ToogleExpand();
    }
}
