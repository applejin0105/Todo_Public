using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Windows.AppNotifications;
using System.Windows;
using System.Windows.Media;
using Microsoft.Windows.AppLifecycle;

// WPF 애플리케이션의 메인 클래스가 포함된 네임스페이스
namespace Todo
{
    // 내부 using 선언으로 코드 간결화
    using ControlzEx.Theming;
    using Todo.Data;
    using Todo.Services;
    using Todo.ViewModels;
    using Todo.Views;

    // 애플리케이션의 주 진입점 및 수명 주기를 관리하는 클래스
    public partial class App : Application
    {
        // 의존성 주입(DI) 컨테이너 역할을 하는 IHost. 앱 전역에서 서비스에 접근할 수 있도록 static으로 선언.
        public static IHost? AppHost { get; private set; }

        // --- 앱 중복 실행 방지를 위한 필드 ---
        // Mutex: 시스템 전역에서 유일한 이름을 가진 객체로, 앱이 이미 실행 중인지 확인하는 데 사용됨.
        private Mutex? _mutex;
        // EventWaitHandle: 이미 실행 중인 앱에 신호를 보내 창을 활성화하도록 요청하는 데 사용됨.
        private EventWaitHandle? _eventWaitHandle;

        // App 클래스 생성자
        public App()
        {
            // 의존성 주입을 위한 호스트(Host)를 구성하고 빌드
            AppHost = Host.CreateDefaultBuilder()
                // appsettings.json 파일을 읽어오도록 구성
                .ConfigureAppConfiguration((hostContext, config) =>
                {
                    config.SetBasePath(AppContext.BaseDirectory);
                    config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
                })
                // 서비스(클래스)들을 의존성 주입 컨테이너에 등록
                .ConfigureServices((hostContext, services) =>
                {
                    // 요청할 때마다 새 인스턴스 생성 (Transient)
                    services.AddTransient<MainWindow>();
                    // DB 컨텍스트 등록. appsettings.json의 연결 문자열을 사용하여 PostgreSQL에 연결.
                    services.AddDbContext<AppDbContext>(options =>
                        options.UseNpgsql(hostContext.Configuration.GetConnectionString("DefaultConnection")));

                    // 앱 수명 주기 동안 단 하나의 인스턴스만 생성 (Singleton)
                    services.AddSingleton<MainViewModel>();
                    services.AddSingleton<SettingsService>();
                    services.AddSingleton<KakaoService>();
                    services.AddSingleton<NotificationScheduler>();
                }).Build();
        }

        // 애플리케이션 시작 시 호출되는 메서드
        protected override async void OnStartup(StartupEventArgs e)
        {
            // --- 중복 실행 방지 로직 ---
            string mutexName = "TodoAppMutex_UNIQUE_GUID"; // 시스템에서 유일해야 하는 뮤텍스 이름
            string eventName = "TodoAppEvent_UNIQUE_GUID"; // 시스템에서 유일해야 하는 이벤트 이름
            bool createdNew;

            _mutex = new Mutex(true, mutexName, out createdNew);
            _eventWaitHandle = new EventWaitHandle(false, EventResetMode.AutoReset, eventName);

            // createdNew가 false이면 이미 다른 인스턴스가 실행 중이라는 의미
            if (!createdNew)
            {
                _eventWaitHandle.Set(); // 이미 실행 중인 인스턴스에 신호를 보냄
                Application.Current.Shutdown(); // 현재 인스턴스는 종료
                return;
            }

            // --- 첫 인스턴스 실행 로직 ---
            RegisterSignalListener(); // 다른 인스턴스로부터 신호를 받을 리스너 등록

            await AppHost!.StartAsync(); // DI 호스트 시작

            // 필요한 서비스들을 DI 컨테이너에서 가져옴
            var settingsService = AppHost.Services.GetRequiredService<SettingsService>();
            var kakaoService = AppHost.Services.GetRequiredService<KakaoService>();
            await kakaoService.TryRefreshAccessTokenAsync(); // 시작 시 카카오 토큰 갱신 시도

            ApplyTheme(settingsService.Settings.Theme); // 저장된 테마 적용

            var startupForm = AppHost.Services.GetRequiredService<MainWindow>(); // 메인 창 인스턴스 생성

            // 앱이 MSIX로 패키징되었는지 확인
            bool isPackaged = AppInstance.GetCurrent().IsCurrent;
            // 패키징되지 않은 경우(예: 디버그 모드) 수동으로 알림 등록
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

            // 알림 스케줄러를 가져와서 시작
            var scheduler = AppHost.Services.GetRequiredService<NotificationScheduler>();
            scheduler.Start();

            startupForm.Show(); // 메인 창 표시
            base.OnStartup(e);
        }

        // 애플리케이션의 테마를 동적으로 변경하는 메서드
        public void ApplyTheme(string themeName)
        {
            // MahApps.Metro의 테마 매니저를 사용하여 기본 테마와 강조 색상을 변경
            ThemeManager.Current.ChangeTheme(this, themeName == "Dark" ? "Dark.Cobalt" : "Light.Cobalt");

            // 기본 강조(Accent) 색상을 사용자 정의 색상으로 덮어씀
            Application.Current.Resources["MahApps.Brushes.Accent"] = new SolidColorBrush(Color.FromRgb(0xbb, 0x86, 0xfc));

            // 이전에 적용된 사용자 정의 테마 리소스가 있으면 제거
            var oldTheme = Resources.MergedDictionaries.FirstOrDefault(d => d.Source != null && d.Source.OriginalString.Contains("Theme.xaml"));
            if (oldTheme != null)
            {
                Resources.MergedDictionaries.Remove(oldTheme);
            }

            // 새 테마 경로를 결정하고 리소스 사전에 추가
            string themePath = themeName == "Dark" ? "Themes/DarkTheme.xaml" : "Themes/LightTheme.xaml";
            Resources.MergedDictionaries.Add(new ResourceDictionary
            {
                Source = new Uri(themePath, UriKind.Relative)
            });
        }

        // 애플리케이션 종료 시 호출되는 메서드
        protected override async void OnExit(ExitEventArgs e)
        {
            _mutex?.ReleaseMutex(); // 뮤텍스 해제
            _eventWaitHandle?.Close(); // 이벤트 핸들 해제
            await AppHost!.StopAsync(); // DI 호스트 정지
            base.OnExit(e);
        }

        // 다른 인스턴스로부터 '창 활성화' 신호를 받기 위한 리스너를 등록하고 실행하는 메서드
        private void RegisterSignalListener()
        {
            // 백그라운드 스레드에서 실행
            Task.Run(() =>
            {
                // EventWaitHandle이 Set() 될 때까지 무한 대기
                while (_eventWaitHandle.WaitOne())
                {
                    // 신호를 받으면 UI 스레드에서 창 활성화 로직을 실행
                    Current.Dispatcher.Invoke(() =>
                    {
                        var mainWindow = Current.MainWindow as MainWindow;
                        if (mainWindow != null)
                        {
                            // 창이 숨겨져 있으면 보이게 함
                            if (mainWindow.Visibility == Visibility.Hidden)
                            {
                                mainWindow.Show();
                            }
                            // 창이 최소화되어 있으면 보통 상태로 복원
                            if (mainWindow.WindowState == WindowState.Minimized)
                            {
                                mainWindow.WindowState = WindowState.Normal;
                            }
                            // 창을 활성화하고 맨 앞으로 가져옴
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