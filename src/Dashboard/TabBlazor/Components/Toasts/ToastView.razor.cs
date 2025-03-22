using TabBlazor.Services;

namespace TabBlazor.Components.Toasts
{
    public partial class ToastView : IDisposable
    {
        private CountdownTimer _countdownTimer;
        private int _progress = 100;
        [Inject] private ToastService ToastService { get; set; }
        [Parameter] public ToastModel Toast { get; set; }

        public void Dispose()
        {
            _countdownTimer?.Dispose();
            _countdownTimer = null;
        }

        protected override void OnInitialized()
        {
            if (Toast.Options.AutoClose)
            {
                _countdownTimer = new CountdownTimer(Toast.Options.Delay * 1000);
                _countdownTimer.OnTick += CalculateProgress;
                _countdownTimer.Start();
            }
        }

        private async void CalculateProgress(int percentComplete)
        {
            _progress = 100 - percentComplete;
            if (percentComplete >= 100)
            {
                await Close();
            }

            await InvokeAsync(StateHasChanged);
        }

        public async Task Close() => await ToastService.RemoveToastAsync(Toast);
    }
}
