// Services/NotificationScheduler.cs
using System;
using System.Threading;
using Todo.ViewModels;

namespace Todo.Services
{
    public class NotificationScheduler
    {
        private readonly MainViewModel _viewModel;
        private Timer? _timer;

        public NotificationScheduler(MainViewModel viewModel)
        {
            _viewModel = viewModel;
        }

        public void Start()
        {
            _timer = new Timer(CheckForImminentWindowsNotifications, null, TimeSpan.Zero, TimeSpan.FromMinutes(15));
        }

        private void CheckForImminentWindowsNotifications(object? state)
        {
            _viewModel.CheckForImminentNotifications();
        }
    }
}