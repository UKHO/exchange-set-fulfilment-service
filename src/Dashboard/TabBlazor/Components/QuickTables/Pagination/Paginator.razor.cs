using TabBlazor.Components.QuickTables.Infrastructure;

namespace TabBlazor.Components.QuickTables
{
    public partial class Paginator : IDisposable
    {
        private readonly EventCallbackSubscriber<PaginationState> _totalItemCountChanged;

        public Paginator() =>
            // The "total item count" handler doesn't need to do anything except cause this component to re-render
            _totalItemCountChanged =
                new EventCallbackSubscriber<PaginationState>(new EventCallback<PaginationState>(this, null));

        [Parameter] [EditorRequired] public PaginationState Value { get; set; } = default!;

        [Parameter] public RenderFragment SummaryTemplate { get; set; }

        private bool CanGoBack => Value.CurrentPageIndex > 0;
        private bool CanGoForwards => Value.CurrentPageIndex < Value.LastPageIndex;

        public void Dispose() => _totalItemCountChanged.Dispose();

        private Task GoFirstAsync() => !CanGoBack ? Task.CompletedTask : GoToPageAsync(0);

        private Task GoPreviousAsync() => !CanGoBack ? Task.CompletedTask : GoToPageAsync(Value.CurrentPageIndex - 1);

        private Task GoNextAsync() => !CanGoForwards ? Task.CompletedTask : GoToPageAsync(Value.CurrentPageIndex + 1);

        private Task GoLastAsync() => !CanGoForwards ? Task.CompletedTask : GoToPageAsync(Value.LastPageIndex.GetValueOrDefault(0));

        private Task GoToPageAsync(int pageIndex) => Value.SetCurrentPageIndexAsync(pageIndex);

        protected override void OnParametersSet() => _totalItemCountChanged.SubscribeOrMove(Value.TotalItemCountChangedSubscribable);
    }
}
