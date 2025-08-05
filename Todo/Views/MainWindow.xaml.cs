using ControlzEx.Theming;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;
using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Todo.Data;
using Todo.Models;
using Todo.Services;
using Todo.ViewModels;

namespace Todo.Views
{
    public partial class MainWindow : MetroWindow
    {
        private readonly MainViewModel _viewModel;
        private readonly SettingsService _settingsService;
        private bool _isExplicitExit = false;

        public MainViewModel GetViewModel() => _viewModel;

        public MainWindow(MainViewModel viewModel, SettingsService settingsService)
        {
            InitializeComponent();
            _viewModel = viewModel;
            _settingsService = settingsService;
            DataContext = _viewModel;

            this.Topmost = _settingsService.Settings.IsAlwaysOnTop;
            this.Top = _settingsService.Settings.WindowTop;
            this.Left = _settingsService.Settings.WindowLeft;
            this.Height = _settingsService.Settings.WindowHeight;
            this.Width = _settingsService.Settings.WindowWidth;
            this.Topmost = _settingsService.Settings.IsAlwaysOnTop;

            Loaded += MainWindow_Loaded;
            Closing += MainWindow_Closing;

        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            await _viewModel.LoadItemsAsync();
            MainTabControl.SelectedIndex = _settingsService.Settings.LastSelectedTabIndex;

            _viewModel.SendStartupSummaryNotification();
        }

        private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            _settingsService.Settings.WindowWidth = this.Width;

            _settingsService.Settings.WindowTop = this.Top;
            _settingsService.Settings.WindowLeft = this.Left;
            _settingsService.Settings.WindowHeight = this.Height;
            _settingsService.Settings.WindowWidth = this.Width;
            _settingsService.Save();

            _settingsService.Settings.LastSelectedTabIndex = MainTabControl.SelectedIndex;

            _settingsService.Save();
        }

        private async void AddButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedPlatform = PlatformComboBox.SelectedItem as Platform;

            var startDate = CombineDateAndTime(StartDatePicker.SelectedDate, StartTimeTextBox.Text);
            var dueDate = CombineDateAndTime(DueDatePicker.SelectedDate, DueTimeTextBox.Text);

            await _viewModel.AddItemAsync(selectedPlatform, startDate, dueDate);

            StartDatePicker.SelectedDate = null;
            StartTimeTextBox.Clear();
            DueDatePicker.SelectedDate = null;
            DueTimeTextBox.Clear();
            PlatformComboBox.SelectedItem = null;
        }

        private DateTime? CombineDateAndTime(DateTime? date, string timeString)
        {
            if (date is null)
            {
                return null;
            }

            if (string.IsNullOrWhiteSpace(timeString))
            {
                return date.Value.Date;
            }

            if (TimeSpan.TryParse(timeString, out var time))
            {
                return date.Value.Date + time;
            }

            return date.Value.Date;
        }

        private void SortByDueDate_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.SortAllLists(item => item.DueDate ?? DateTime.MaxValue);
        }

        private void SortByStartDate_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.SortAllLists(item => item.StartDate ?? DateTime.MaxValue);
        }

        private async void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is TodoItem item)
            {
                await _viewModel.DeleteItemAsync(item);
            }
        }

        private async void StatusComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox comboBox && comboBox.DataContext is TodoItem item)
            {
                if (comboBox.IsLoaded && e.AddedItems.Count > 0)
                {
                    await _viewModel.UpdateItemAsync(item);
                }
            }
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            var settingsService = App.AppHost!.Services.GetRequiredService<SettingsService>();
            var kakaoService = App.AppHost!.Services.GetRequiredService<KakaoService>();
            var viewModel = App.AppHost!.Services.GetRequiredService<MainViewModel>();
            var scheduler = App.AppHost!.Services.GetRequiredService<NotificationScheduler>();

            var settingsWindow = new SettingsWindow(settingsService, kakaoService, viewModel, scheduler)
            {
                Owner = this
            };
            settingsWindow.ShowDialog();
        }

        private async void PlatformManagementButton_Click(object sender, RoutedEventArgs e)
        {
            using var scope = App.AppHost!.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var managementWindow = new PlatformManagementWindow(dbContext)
            {
                Owner = this
            };

            managementWindow.ShowDialog();
            await _viewModel.ReloadPlatformsAsync();
        }

        private void PlatformComboBox_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            if (sender is ComboBox comboBox)
            {
                comboBox.IsDropDownOpen = true;
            }
        }

        private void NewItem_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                AddButton_Click(sender, e);
            }
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is TodoItem item)
            {
                using var scope = App.AppHost!.Services.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                // MainViewModel을 생성자에 함께 전달
                var editWindow = new EditTaskWindow(item, dbContext, _viewModel) { Owner = this };
                editWindow.ShowDialog();
            }
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);
            if (!_isExplicitExit)
            {
                e.Cancel = true; // 종료 취소
                this.Hide();     // 창 숨기기
            }
        }

        private void MyNotifyIcon_TrayMouseDoubleClick(object sender, RoutedEventArgs e)
        {
            this.Show();
            this.WindowState = WindowState.Normal;
        }

        // 우클릭 메뉴 '열기' 클릭 시
        private void ShowWindow_Click(object sender, RoutedEventArgs e)
        {
            this.Show();
            this.WindowState = WindowState.Normal;
        }

        // 우클릭 메뉴 '종료' 클릭 시
        private void ExitApplication_Click(object sender, RoutedEventArgs e)
        {
            _isExplicitExit = true; // 진짜 종료 플래그 설정
            this.Close();           // 창 닫기 (OnClosing이 호출됨)
        }

    }
}