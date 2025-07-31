using Microsoft.AspNetCore.Components;

namespace UKHO.ADDS.Configuration.AACEmulator.Common
{
    public class DialogReference(Type type, IDictionary<string, object?>? parameters = null) : IDialogReference
    {
        private TaskCompletionSource<ElementReference> ElementTaskCompletionSource { get; } = new();

        private TaskCompletionSource<IDialogResult?> ResultTaskCompletionSource { get; } = new();
        public Task<ElementReference> Element => ElementTaskCompletionSource.Task;

        public IDictionary<string, object?>? Parameters { get; } = parameters;

        public Task<IDialogResult?> Result => ResultTaskCompletionSource.Task;

        public Type Type { get; } = type;

        public bool TrySetElement(ElementReference element) => ElementTaskCompletionSource.TrySetResult(element);

        public bool TrySetResult(IDialogResult? result = null) => ResultTaskCompletionSource.TrySetResult(result);
    }

    public class DialogResult(object? data = null) : IDialogResult
    {
        public object? Data { get; } = data;
    }

    public class DialogService : IDialogService
    {
        public event Func<IDialogReference, IDialogResult?, Task>? OnDialogClosed;

        public event Func<IDialogReference, Task>? OnDialogShown;

        public async Task Close(IDialogReference reference, IDialogResult? result = null)
        {
            if (OnDialogClosed is not null)
            {
                await OnDialogClosed.Invoke(reference, result);
            }

            reference.TrySetResult(result);
        }

        public async Task<IDialogReference> Show<TComponent>(IDictionary<string, object?>? parameters = null)
        {
            var reference = new DialogReference(typeof(TComponent), parameters);

            if (OnDialogShown is not null)
            {
                await OnDialogShown.Invoke(reference);
            }

            return reference;
        }
    }
}
