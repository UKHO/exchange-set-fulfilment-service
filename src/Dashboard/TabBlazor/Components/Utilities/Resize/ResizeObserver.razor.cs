using Microsoft.JSInterop;

namespace TabBlazor
{
    public partial class ResizeObserver : TablerBaseComponent
    {
        private ResizeObserverEntry currentEntry;


        private ElementReference elementRef;
        [Inject] private IJSRuntime jSRuntime { get; set; }
        [Parameter] public string Tag { get; set; } = "div";
        [Parameter] public EventCallback<ResizeObserverEntry> OnResized { get; set; }
        [Parameter] public EventCallback<ResizeObserverEntry> OnWidthResized { get; set; }
        [Parameter] public EventCallback<ResizeObserverEntry> OnHeightResized { get; set; }

        protected override string ClassNames => ClassBuilder
            .Add(BackgroundColor.GetColorClass("bg"))
            .Add(TextColor.GetColorClass("text"))
            .ToString();

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                await jSRuntime.InvokeVoidAsync("tabBlazor.addResizeObserver", elementRef, DotNetObjectReference.Create(this));
            }
        }

        [JSInvokable]
        public async Task ElementResized(ResizeObserverEntry resizeObserverEntry)
        {
            await OnResized.InvokeAsync(resizeObserverEntry);
            if (currentEntry?.ContentRect?.Width != resizeObserverEntry?.ContentRect?.Width)
            {
                await OnWidthResized.InvokeAsync(resizeObserverEntry);
            }

            if (currentEntry?.ContentRect?.Height != resizeObserverEntry?.ContentRect?.Height)
            {
                await OnHeightResized.InvokeAsync(resizeObserverEntry);
            }

            currentEntry = resizeObserverEntry;
        }
    }
}
