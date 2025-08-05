using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;


// Todo 애플리케이션의 뷰(UI) 관련 클래스를 포함하는 네임스페이스
namespace Todo.Views
{
    // 내부 using 선언으로 코드 간결화
    using Todo.Services;
    using Todo.ViewModels;

    // 애플리케이션 설정을 관리하는 MetroWindow 창 클래스
    public partial class SettingsWindow : MetroWindow
    {
        // 애플리케이션 설정 관리 서비스
        private readonly SettingsService _settingsService;
        // 카카오 로그인/알림 관련 서비스
        private readonly KakaoService _kakaoService;
        // 메인 뷰모델 참조. 다른 뷰모델 기능에 접근하기 위해 사용
        private readonly MainViewModel _viewModel;
        // 알림 스케줄링 서비스
        private readonly NotificationScheduler _scheduler;

        // SettingsWindow의 생성자. 필요한 서비스들을 의존성 주입으로 받음
        public SettingsWindow(SettingsService settingsService, KakaoService kakaoService, MainViewModel viewModel, NotificationScheduler scheduler)
        {
            // XAML에 정의된 UI 컴포넌트들을 초기화
            InitializeComponent();
            // 전달받은 서비스 인스턴스들을 클래스 필드에 할당
            _settingsService = settingsService;
            _kakaoService = kakaoService;
            _viewModel = viewModel;
            _scheduler = scheduler;
            // 현재 저장된 설정들을 UI에 로드
            LoadCurrentSettings();
            // 카카오 로그인 상태에 따라 버튼 UI를 업데이트
            UpdateKakaoButtons();
        }

        // 카카오 로그인 상태에 따라 로그인/로그아웃 버튼의 표시 여부를 결정하는 메서드
        private void UpdateKakaoButtons()
        {
            // 설정에 카카오 리프레시 토큰이 있는지 여부로 로그인 상태를 판단
            bool isLoggedIn = !string.IsNullOrEmpty(_settingsService.Settings.KakaoRefreshToken);
            // 로그인 상태이면 로그인 버튼 숨기고, 아니면 표시
            KakaoLoginButton.Visibility = isLoggedIn ? Visibility.Collapsed : Visibility.Visible;
            // 로그인 상태이면 로그아웃 버튼 표시하고, 아니면 숨김
            KakaoLogoutButton.Visibility = isLoggedIn ? Visibility.Visible : Visibility.Collapsed;
        }

        // '카카오 로그인' 버튼 클릭 이벤트 핸들러
        private async void KakaoLoginButton_Click(object sender, RoutedEventArgs e)
        {
            // 카카오 인증 URL을 가져옴
            var authUrl = _kakaoService.GetAuthenticationUrl();

            // 기본 웹 브라우저로 인증 URL을 열어 사용자 로그인을 유도
            Process.Start(new ProcessStartInfo(authUrl) { UseShellExecute = true });

            // 사용자에게 인증 코드를 입력받기 위한 비동기 대화상자를 표시
            var result = await this.ShowInputAsync("Enter authentication code", "Copy the code that appears in your web browser,\nPaste it here.");

            // 사용자가 코드를 입력하고 '확인'을 눌렀는지 확인
            if (!string.IsNullOrWhiteSpace(result))
            {
                // 입력받은 코드로 인증(토큰 발급)을 시도
                bool success = await _kakaoService.AuthorizeAsync(result);
                // 인증 결과를 메시지 대화상자로 사용자에게 알림
                await this.ShowMessageAsync("Authentication Results", success ? "Login Successful" : "Login Failed");
                // 로그인 상태가 변경되었으므로 버튼 UI를 업데이트
                UpdateKakaoButtons();
            }
        }

        // 저장된 설정 값을 UI 컨트롤에 로드하는 메서드
        private void LoadCurrentSettings()
        {
            // 저장된 테마 설정에 따라 콤보박스 선택
            ThemeComboBox.SelectedIndex = _settingsService.Settings.Theme == "Dark" ? 1 : 0;
            // '항상 위에 표시' 설정 로드
            TopmostCheckBox.IsChecked = _settingsService.Settings.IsAlwaysOnTop;
            // '알림 사용' 설정 로드
            NotificationCheckBox.IsChecked = _settingsService.Settings.AreNotificationsEnabled;
            // '윈도우 시작 시 자동 실행' 설정 로드
            StartupCheckBox.IsChecked = _settingsService.IsStartupEnabled();
        }

        // '테마' 콤보박스 선택 변경 이벤트 핸들러
        private void ThemeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // 창이 완전히 로드된 후에만 실행 (초기 로드 시 불필요한 실행 방지)
            if (this.IsLoaded)
            {
                // 선택된 인덱스에 따라 테마 이름을 결정
                _settingsService.Settings.Theme = ThemeComboBox.SelectedIndex == 0 ? "Light" : "Dark";
                // 변경된 설정을 저장
                _settingsService.Save();

                // 현재 애플리케이션 전체에 변경된 테마를 적용
                (Application.Current as App)?.ApplyTheme(_settingsService.Settings.Theme);
            }
        }

        // '항상 위에 표시' 체크박스 클릭 이벤트 핸들러
        private void TopmostCheckBox_Click(object sender, RoutedEventArgs e)
        {
            // 체크박스 상태를 설정에 저장 (null일 경우 false로 처리)
            _settingsService.Settings.IsAlwaysOnTop = TopmostCheckBox.IsChecked ?? false;
            _settingsService.Save();
            // 현재 애플리케이션의 메인 창을 찾아 'Topmost' 속성을 즉시 변경
            if (Application.Current.MainWindow is MainWindow mainWindow)
            {
                mainWindow.Topmost = _settingsService.Settings.IsAlwaysOnTop;
            }
        }

        // '알림 사용' 체크박스 클릭 이벤트 핸들러
        private void NotificationCheckBox_Click(object sender, RoutedEventArgs e)
        {
            // 체크박스 상태를 설정에 저장
            _settingsService.Settings.AreNotificationsEnabled = NotificationCheckBox.IsChecked ?? false;
            _settingsService.Save();
        }

        // '카카오 로그아웃' 버튼 클릭 이벤트 핸들러
        private async void KakaoLogoutButton_Click(object sender, RoutedEventArgs e)
        {
            // 카카오 로그아웃(토큰 폐기)을 비동기적으로 처리
            await _kakaoService.LogoutAsync();
            // 로그아웃 완료 메시지를 표시
            await this.ShowMessageAsync("Logout", "You are logged out.");
            // 버튼 UI를 업데이트
            UpdateKakaoButtons();
        }

        // '윈도우 시작 시 자동 실행' 체크박스 클릭 이벤트 핸들러
        private async void StartupCheckBox_Click(object sender, RoutedEventArgs e)
        {
            // 체크박스 상태에 따라 시작 프로그램 등록/해제를 비동기적으로 처리
            await _settingsService.SetStartupAsync(StartupCheckBox.IsChecked ?? false);
        }

        // '윈도우 알림 테스트' 버튼 클릭 이벤트 핸들러
        private void WindowsNotification_Click(object sender, RoutedEventArgs e)
        {
            // AppNotificationBuilder를 사용하여 테스트용 알림 객체를 생성
            var notification = new AppNotificationBuilder()
                .AddText("🔔 Windows 즉시 알림 테스트")
                .AddText("이 알림은 버튼 클릭 시 즉시 발생합니다.")
                .BuildNotification();
            // 생성된 알림을 즉시 표시
            AppNotificationManager.Default.Show(notification);
        }

        // '마감 임박 알림 테스트' 버튼 클릭 이벤트 핸들러
        private async void WindowsImminentNotification_Click(object sender, RoutedEventArgs e)
        {
            // 현재 시간으로부터 5분 뒤를 마감 시간으로 설정
            var dueDate = DateTime.Now.AddMinutes(5);
            // 뷰모델에 테스트용 작업 제목 설정
            _viewModel.NewItemTitle = "마감 임박 알림 테스트용 작업";
            // 테스트용 작업을 비동기적으로 추가
            await _viewModel.AddItemAsync(null, DateTime.Now, dueDate);

            // 마감 임박 알림 확인 로직을 즉시 실행
            _viewModel.CheckForImminentNotifications();
            // 테스트 작업이 추가되었음을 사용자에게 알림
            await this.ShowMessageAsync("테스트 작업 추가", $"'{dueDate:HH:mm}'에 마감되는 작업을 추가하고 마감 임박 알림을 실행했습니다.");
        }

        // 창 전체에서 키 입력 이벤트를 미리 감지하는 핸들러
        private void MetroWindow_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // 눌린 키가 Escape 키인지 확인
            if (e.Key == Key.Escape)
            {
                // 창을 닫음
                this.Close();
            }
        }
    }
}