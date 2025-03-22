using TabBlazor.Components.Offcanvas;

namespace TabBlazor
{
    public class OffcanvasService : IOffcanvasService
    {
        private readonly Stack<OffcanvasModel> models = new();

        public event Action OnChanged;
        public IEnumerable<OffcanvasModel> Models => models;

        public Task<OffcanvasResult> ShowAsync<TComponent>(string title, RenderComponent<TComponent> component, OffcanvasOptions options = null) where TComponent : IComponent
        {
            var offcanvasModel = new OffcanvasModel { Title = title, Contents = component.Contents, Options = options ?? new OffcanvasOptions() };
            models.Push(offcanvasModel);
            OnChanged?.Invoke();
            return offcanvasModel.Task;
        }

        public void Close()
        {
            if (models.Any())
            {
                models.Pop();
            }

            OnChanged?.Invoke();
        }
    }
}
