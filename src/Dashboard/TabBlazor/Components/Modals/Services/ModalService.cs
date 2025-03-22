using Microsoft.AspNetCore.Components.Routing;
using TabBlazor.Components.Modals;

namespace TabBlazor.Services
{
    public class ModalService : IModalService, IDisposable
    {
        private const int zIndexIncrement = 10;
        private const int topOffsetIncrement = 20;
        private readonly NavigationManager navigationManager;
        internal ModalModel modalModel;
        private readonly Stack<ModalModel> modals = new();
        private int topOffset;


        private int zIndex = 1200;

        public ModalService(NavigationManager navigationManager)
        {
            this.navigationManager = navigationManager;
            this.navigationManager.LocationChanged += LocationChanged;
        }

        public void Dispose() => navigationManager.LocationChanged -= LocationChanged;

        public event Action OnChanged;

        public IEnumerable<ModalModel> Modals => modals;

        public Task<ModalResult> ShowAsync<TComponent>(string title, RenderComponent<TComponent> component, ModalOptions modalOptions = null) where TComponent : IComponent
        {
            modalModel = new ModalModel(component.Contents, title, modalOptions);
            modals.Push(modalModel);
            OnChanged?.Invoke();
            return modalModel.Task;
        }

        public async Task<bool> ShowDialogAsync(DialogOptions options)
        {
            var component = new RenderComponent<DialogModal>().Set(e => e.Options, options);
            var result = await ShowAsync("", component, new ModalOptions { ModalBodyCssClass = "p-0", Size = ModalSize.Small, ShowHeader = false, StatusColor = options.StatusColor });
            return !result.Cancelled;
        }

        public void Close(ModalResult modalResult)
        {
            if (modals.Any())
            {
                var modalToClose = modals.Pop();
                modalToClose.TaskSource.SetResult(modalResult);
            }

            OnChanged?.Invoke();
        }

        public void Close() => Close(ModalResult.Cancel());

        public void UpdateTitle(string title)
        {
            var modal = Modals.LastOrDefault();
            if (modal != null)
            {
                modal.Title = title;
                OnChanged?.Invoke();
            }
        }

        public void Refresh()
        {
            var modal = Modals.LastOrDefault();
            if (modal != null)
            {
                OnChanged?.Invoke();
            }
        }

        public ModalViewSettings RegisterModalView(ModalView modalView)
        {
            var settings = new ModalViewSettings { TopOffset = topOffset, ZIndex = zIndex };
            zIndex += zIndexIncrement;
            topOffset += topOffsetIncrement;

            return settings;
        }

        public void UnRegisterModalView(ModalView modalView)
        {
            zIndex -= zIndexIncrement;
            topOffset -= topOffsetIncrement;
        }

        private void LocationChanged(object sender, LocationChangedEventArgs e) => CloseAll();

        private void CloseAll()
        {
            foreach (var x in modals.ToList())
            {
                Close();
            }
        }
    }
}
