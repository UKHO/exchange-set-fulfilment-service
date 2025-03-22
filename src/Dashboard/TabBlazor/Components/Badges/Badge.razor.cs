namespace TabBlazor
{
    public enum BadgeShape
    {
        Default,
        Pill
    }

    public enum BadgeSize
    {
        Default,
        Large
    }

    public enum BadgeType
    {
        Default,
        Light,
        Outline
    }


    public partial class Badge : TablerBaseComponent
    {
        [Parameter] public BadgeShape Shape { get; set; }
        [Parameter] public BadgeSize Size { get; set; } = BadgeSize.Default;
        [Parameter] public BadgeType BadgeType { get; set; } = BadgeType.Default;

        [Parameter] public bool Notification { get; set; }
        [Parameter] public bool Blink { get; set; }

        protected string HtmlTag => "span";


        protected override string ClassNames
        {
            get
            {
                var builder = ClassBuilder;

                builder = builder
                    .Add("badge")
                    .AddCompare("badge-md", Size, BadgeSize.Default)
                    .AddCompare("badge-pill", Shape, BadgeShape.Pill)
                    .AddIf("badge-notification", Notification)
                    .AddIf("badge-blink", Blink)
                    .AddIf("cursor-pointer", OnClick.HasDelegate);


                if (BadgeType == BadgeType.Default)
                {
                    builder = builder
                        .Add(BackgroundColor.GetColorClass("text") + "-fg")
                        .Add(BackgroundColor.GetColorClass("bg"));
                }
                else if (BadgeType == BadgeType.Light)
                {
                    builder = builder
                        .Add(BackgroundColor.GetColorClass("bg") + "-lt");
                }
                else if (BadgeType == BadgeType.Outline)
                {
                    builder = builder
                        .Add("badge-outline")
                        .Add(BackgroundColor.GetColorClass("text"));
                }


                return builder.ToString();
            }
        }
    }
}
