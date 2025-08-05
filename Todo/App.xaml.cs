using MahApps.Metro;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Windows.AppNotifications;
using System;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Media;
using Microsoft.Windows.AppLifecycle;

namespace Todo
{
    using ControlzEx.Theming;
    using Todo.Data;
    using Todo.Services;
    using Todo.ViewModels;
    using Todo.Views;

    public partial class App : Application
    {
        public static IHost? AppHost { get; private set; }

        private Mutex? _mutex;
        private EventWaitHandle? _eventWaitHandle;

        public App()
        {
            AppHost = Host.CreateDefaultBuilder()
                .ConfigureAppConfiguration((hostContext, config) =>
                {
                    config.SetBasePath(AppContext.BaseDirectory);
                    config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
                })
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddTransient<MainWindow>();
                    services.AddDbContext<AppDbContext>(options =>
                        options.UseNpgsql(hostContext.Configuration.GetConnectionString("DefaultConnection")));

                    services.AddSingleton<MainViewModel>();
                    services.AddSingleton<SettingsService>();
                    services.AddSingleton<KakaoService>();
                    services.AddSingleton<NotificationScheduler>();
                }).Build();
        }

        protected override async void OnStartup(StartupEventArgs e)
        {
            string mutexName = "TodoAppMutex_UNIQUE_GUID";
            string eventName = "TodoAppEvent_UNIQUE_GUID";
            bool createdNew;

            _mutex = new Mutex(true, mutexName, out createdNew);
            _eventWaitHandle = new EventWaitHandle(false, EventResetMode.AutoReset, eventName);

            if (!createdNew)
            {
                _eventWaitHandle.Set();
                Application.Current.Shutdown();
                return;
            }

            RegisterSignalListener();

            await AppHost!.StartAsync();

            var settingsService = AppHost.Services.GetRequiredService<SettingsService>();
            var kakaoService = AppHost.Services.GetRequiredService<KakaoService>();
            await kakaoService.TryRefreshAccessTokenAsync();

            ApplyTheme(settingsService.Settings.Theme);

            var startupForm = AppHost.Services.GetRequiredService<MainWindow>();

            bool isPackaged = AppInstance.GetCurrent().IsCurrent;
            if (!isPackaged)
            {
                try
                {
                    AppNotificationManager.Default.Register();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Alarm registration failed: {ex.Message}");
                }
            }

            var scheduler = AppHost.Services.GetRequiredService<NotificationScheduler>();
            scheduler.Start();

            startupForm.Show();
            base.OnStartup(e);
        }

        public void ApplyTheme(string themeName)
        {
            ThemeManager.Current.ChangeTheme(this, themeName == "Dark" ? "Dark.Cobalt" : "Light.Cobalt");

            Application.Current.Resources["MahApps.Brushes.Accent"] = new SolidColorBrush(Color.FromRgb(0xbb, 0x86, 0xfc));

            var oldTheme = Resources.MergedDictionaries.FirstOrDefault(d => d.Source != null && d.Source.OriginalString.Contains("Theme.xaml"));
            if (oldTheme != null)
            {
                Resources.MergedDictionaries.Remove(oldTheme);
            }

            string themePath = themeName == "Dark" ? "Themes/DarkTheme.xaml" : "Themes/LightTheme.xaml";
            Resources.MergedDictionaries.Add(new ResourceDictionary
            {
                Source = new Uri(themePath, UriKind.Relative)
            });
        }

        protected override async void OnExit(ExitEventArgs e)
        {
            _mutex?.ReleaseMutex();
            _eventWaitHandle?.Close();
            await AppHost!.StopAsync();
            base.OnExit(e);
        }

        private void RegisterSignalListener()
        {
            Task.Run(() =>
            {
                while (_eventWaitHandle.WaitOne())
                {
                    Current.Dispatcher.Invoke(() =>
                    {
                        var mainWindow = Current.MainWindow as MainWindow;
                        if (mainWindow != null)
                        {
                            if (mainWindow.Visibility == Visibility.Hidden)
                            {
                                mainWindow.Show();
                            }
                            if (mainWindow.WindowState == WindowState.Minimized)
                            {
                                mainWindow.WindowState = WindowState.Normal;
                            }
                            mainWindow.Activate();
                            mainWindow.Topmost = true;
                            mainWindow.Topmost = false;
                            mainWindow.Focus();
                        }
                    });
                }
            });
        }
    }
}