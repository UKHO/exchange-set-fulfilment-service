namespace TabBlazor
{
    public partial class CarouselItem : IDisposable
    {
        [CascadingParameter] private Carousel Carousel { get; set; }

        [Parameter] public string ImageSrc { get; set; }
        [Parameter] public RenderFragment ChildContent { get; set; }

        [Parameter] public RenderFragment IndicatorTemplate { get; set; }

        [Parameter] public RenderFragment CaptionTemplate { get; set; }


        [Parameter] public object Data { get; set; }

        private bool isActive => Carousel.activeItem == this;


        public void Dispose() => Carousel?.RemoveCarouselItem(this);


        protected override void OnInitialized()
        {
            base.OnInitialized();
            Carousel?.AddCarouselItem(this);
        }
    }
}
