using ControlzEx.Theming;
using MahApps.Metro;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Win32;
using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;


namespace Todo.Views
{

    using Todo.Services;
    using Todo.ViewModels;

    public partial class SettingsWindow : MetroWindow
    {
        private readonly SettingsService _settingsService;
        private readonly KakaoService _kakaoService;
        private readonly MainViewModel _viewModel;
        private readonly NotificationScheduler _scheduler;

        public SettingsWindow(SettingsService settingsService, KakaoService kakaoService, MainViewModel viewModel, NotificationScheduler scheduler)
        {
            InitializeComponent();
            _settingsService = settingsService;
            _kakaoService = kakaoService;
            _viewModel = viewModel;
            _scheduler = scheduler;
            LoadCurrentSettings();
            UpdateKakaoButtons();
        }

        private void UpdateKakaoButtons()
        {
            bool isLoggedIn = !string.IsNullOrEmpty(_settingsService.Settings.KakaoRefreshToken);
            KakaoLoginButton.Visibility = isLoggedIn ? Visibility.Collapsed : Visibility.Visible;
            KakaoLogoutButton.Visibility = isLoggedIn ? Visibility.Visible : Visibility.Collapsed;
        }

        private async void KakaoLoginButton_Click(object sender, RoutedEventArgs e)
        {
            var authUrl = _kakaoService.GetAuthenticationUrl();

            Process.Start(new ProcessStartInfo(authUrl) { UseShellExecute = true });

            var result = await this.ShowInputAsync("Enter authentication code", "Copy the code that appears in your web browser,\nPaste it here.");

            if (!string.IsNullOrWhiteSpace(result))
            {
                bool success = await _kakaoService.AuthorizeAsync(result);
                await this.ShowMessageAsync("Authentication Results", success ? "Login Successful" : "Login Failed");
                UpdateKakaoButtons();
            }
        }

        private void LoadCurrentSettings()
        {
            ThemeComboBox.SelectedIndex = _settingsService.Settings.Theme == "Dark" ? 1 : 0;
            TopmostCheckBox.IsChecked = _settingsService.Settings.IsAlwaysOnTop;
            NotificationCheckBox.IsChecked = _settingsService.Settings.AreNotificationsEnabled;
            StartupCheckBox.IsChecked = _settingsService.IsStartupEnabled();
        }

        private void ThemeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.IsLoaded)
            {
                _settingsService.Settings.Theme = ThemeComboBox.SelectedIndex == 0 ? "Light" : "Dark";
                _settingsService.Save();

                (Application.Current as App)?.ApplyTheme(_settingsService.Settings.Theme);
            }
        }

        private void TopmostCheckBox_Click(object sender, RoutedEventArgs e)
        {
            _settingsService.Settings.IsAlwaysOnTop = TopmostCheckBox.IsChecked ?? false;
            _settingsService.Save();
            if (Application.Current.MainWindow is MainWindow mainWindow)
            {
                mainWindow.Topmost = _settingsService.Settings.IsAlwaysOnTop;
            }
        }

        private void NotificationCheckBox_Click(object sender, RoutedEventArgs e)
        {
            _settingsService.Settings.AreNotificationsEnabled = NotificationCheckBox.IsChecked ?? false;
            _settingsService.Save();
        }

        private async void KakaoLogoutButton_Click(object sender, RoutedEventArgs e)
        {
            await _kakaoService.LogoutAsync();
            await this.ShowMessageAsync("Logout", "You are logged out.");
            UpdateKakaoButtons();
        }

        private async void StartupCheckBox_Click(object sender, RoutedEventArgs e)
        {
            await _settingsService.SetStartupAsync(StartupCheckBox.IsChecked ?? false);
        }

        private void WindowsNotification_Click(object sender, RoutedEventArgs e)
        {
            var notification = new AppNotificationBuilder()
                .AddText("🔔 Windows 즉시 알림 테스트")
                .AddText("이 알림은 버튼 클릭 시 즉시 발생합니다.")
                .BuildNotification();
            AppNotificationManager.Default.Show(notification);
        }

        private async void WindowsImminentNotification_Click(object sender, RoutedEventArgs e)
        {
            var dueDate = DateTime.Now.AddMinutes(5); // 5분 뒤 마감되는 작업 생성
            _viewModel.NewItemTitle = "마감 임박 알림 테스트용 작업";
            await _viewModel.AddItemAsync(null, DateTime.Now, dueDate);

            _viewModel.CheckForImminentNotifications(); // 마감 임박 알림 로직 즉시 실행
            await this.ShowMessageAsync("테스트 작업 추가", $"'{dueDate:HH:mm}'에 마감되는 작업을 추가하고 마감 임박 알림을 실행했습니다.");
        }

        private void MetroWindow_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                this.Close();
            }
        }

    }
}
