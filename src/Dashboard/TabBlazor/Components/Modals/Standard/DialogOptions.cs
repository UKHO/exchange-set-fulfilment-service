namespace TabBlazor.Components.Modals
{
    public class DialogOptions
    {
        public TablerColor StatusColor = TablerColor.Default;
        public string MainText { get; set; }
        public string SubText { get; set; }
        public IIconType IconType { get; set; }

        public string CancelText { get; set; } = "Cancel";
        public string OkText { get; set; } = "Ok";
    }
}
