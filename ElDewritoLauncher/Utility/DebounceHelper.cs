using System;
using System.Windows.Threading;

namespace EDLauncher.Utility
{
    public class DebounceHelper
    {
        private readonly DispatcherTimer _timer;

        public DebounceHelper(TimeSpan interval, Action action)
        {
            _timer = new DispatcherTimer();
            _timer.Interval = interval;
            _timer.Tick += (sender, args) =>
            {
                _timer.Stop();
                action.Invoke();
            };
        }

        public void Debounce()
        {
            _timer.Stop();
            _timer.Start();
        }
    }
}
