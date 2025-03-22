using System.Timers;
using Timer = System.Timers.Timer;

namespace TabBlazor.Components.Toasts
{
    internal class CountdownTimer : IDisposable
    {
        private int _percentComplete;
        private Timer _timer;

        internal Action<int> OnTick;


        internal CountdownTimer(int timeout)
        {
            _timer = new Timer(timeout) { Interval = timeout / 100, AutoReset = true };

            _timer.Elapsed += HandleTick;

            _percentComplete = 0;
        }

        public void Dispose()
        {
            _timer.Dispose();
            _timer = null;
        }

        internal void Start() => _timer.Start();

        private void HandleTick(object sender, ElapsedEventArgs args)
        {
            _percentComplete += 1;
            OnTick?.Invoke(_percentComplete);

            if (_percentComplete >= 100)
            {
            }
        }
    }
}
