using TabBlazor.Services;

namespace TabBlazor
{
    public partial class ContentViewer : TablerBaseComponent, IAsyncDisposable
    {
        private string objectURL;
        [Inject] private TablerService TablerService { get; set; }
        [Parameter] public string ContentType { get; set; }
        [Parameter] public byte[] Content { get; set; }
        [Parameter] public string UrlSuffix { get; set; }


        public async ValueTask DisposeAsync()
        {
            if (objectURL != null)
            {
                await TablerService.RevokeObjectURLAsync(objectURL);
            }
        }


        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (Content != null && objectURL == null)
            {
                objectURL = await TablerService.CreateObjectURLAsync(ContentType, Content);
                StateHasChanged();
            }
        }
    }
}
