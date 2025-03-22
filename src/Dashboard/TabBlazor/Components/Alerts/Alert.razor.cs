namespace TabBlazor
{
    public partial class Alert : TablerBaseComponent
    {
        private bool dismissed;
        [Parameter] public string Title { get; set; }
        [Parameter] public bool Dismissible { get; set; }
        [Parameter] public bool Important { get; set; }

        protected override string ClassNames => ClassBuilder
            .Add("alert")
            .Add(BackgroundColor.GetColorClass("alert"))
            .Add(TextColor.GetColorClass("text"))
            .AddIf("alert-dismissible", Dismissible)
            .AddIf("alert-important", Important)
            .ToString();

        protected void DismissAlert() => dismissed = true;
    }
}
