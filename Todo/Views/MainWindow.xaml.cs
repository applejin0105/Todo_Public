using MahApps.Metro.Controls;
using Microsoft.Extensions.DependencyInjection;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

// Todo 애플리케이션의 뷰(UI) 관련 클래스를 포함하는 네임스페이스
namespace Todo.Views
{
    using Todo.Data;
    using Todo.Models;
    using Todo.Services;
    using Todo.ViewModels;

    // MahApps.Metro의 MetroWindow를 상속받는 주 창 클래스
    public partial class MainWindow : MetroWindow
    {
        // 뷰의 데이터와 비즈니스 로직을 처리하는 MainViewModel의 읽기 전용 인스턴스
        private readonly MainViewModel _viewModel;
        // 애플리케이션 설정을 관리하는 SettingsService의 읽기 전용 인스턴스
        private readonly SettingsService _settingsService;
        // 사용자가 명시적으로 종료를 원하는지 여부를 나타내는 플래그 (트레이 아이콘 메뉴의 '종료' 등)
        private bool _isExplicitExit = false;

        // 외부에서 MainViewModel 인스턴스를 가져갈 수 있도록 하는 메서드
        public MainViewModel GetViewModel() => _viewModel;

        // MainWindow의 생성자. 의존성 주입을 통해 viewModel과 settingsService를 받음
        public MainWindow(MainViewModel viewModel, SettingsService settingsService)
        {
            // XAML에 정의된 UI 컴포넌트들을 초기화
            InitializeComponent();
            // 전달받은 viewModel 인스턴스를 클래스 필드에 할당
            _viewModel = viewModel;
            // 전달받은 settingsService 인스턴스를 클래스 필드에 할당
            _settingsService = settingsService;
            // 이 창의 데이터 컨텍스트를 viewModel로 설정. 이를 통해 XAML에서의 데이터 바인딩이 가능해짐
            DataContext = _viewModel;

            // 설정 파일에서 창 '항상 위에 표시' 여부를 불러와 적용
            this.Topmost = _settingsService.Settings.IsAlwaysOnTop;
            // 설정 파일에서 마지막 창 위치(Top)를 불러와 적용
            this.Top = _settingsService.Settings.WindowTop;
            // 설정 파일에서 마지막 창 위치(Left)를 불러와 적용
            this.Left = _settingsService.Settings.WindowLeft;
            // 설정 파일에서 마지막 창 높이를 불러와 적용
            this.Height = _settingsService.Settings.WindowHeight;
            // 설정 파일에서 마지막 창 너비를 불러와 적용
            this.Width = _settingsService.Settings.WindowWidth;
            // 설정 파일에서 창 '항상 위에 표시' 여부를 다시 적용 (앞선 코드와 중복)
            this.Topmost = _settingsService.Settings.IsAlwaysOnTop;

            // 창이 완전히 로드되었을 때 실행될 이벤트 핸들러 등록
            Loaded += MainWindow_Loaded;
            // 창이 닫히려고 할 때 실행될 이벤트 핸들러 등록
            Closing += MainWindow_Closing;
        }

        // 창 로드 완료 시 발생하는 이벤트 핸들러
        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // 뷰모델을 통해 비동기적으로 할 일 항목들을 데이터베이스에서 로드
            await _viewModel.LoadItemsAsync();
            // 마지막으로 선택했던 탭의 인덱스를 설정에서 불러와 적용
            MainTabControl.SelectedIndex = _settingsService.Settings.LastSelectedTabIndex;

            // 프로그램 시작 시 요약 알림을 보내는 메서드 호출
            _viewModel.SendStartupSummaryNotification();
        }

        // 창이 닫힐 때 발생하는 이벤트 핸들러 (X 버튼 클릭 등)
        private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            // 현재 창의 너비를 설정 객체에 저장
            _settingsService.Settings.WindowWidth = this.Width;
            // 현재 창의 Y 위치를 설정 객체에 저장
            _settingsService.Settings.WindowTop = this.Top;
            // 현재 창의 X 위치를 설정 객체에 저장
            _settingsService.Settings.WindowLeft = this.Left;
            // 현재 창의 높이를 설정 객체에 저장
            _settingsService.Settings.WindowHeight = this.Height;
            // 현재 창의 너비를 설정 객체에 다시 저장 (앞선 코드와 중복)
            _settingsService.Settings.WindowWidth = this.Width;
            // 변경된 설정들을 파일에 저장
            _settingsService.Save();

            // 현재 선택된 탭의 인덱스를 설정 객체에 저장
            _settingsService.Settings.LastSelectedTabIndex = MainTabControl.SelectedIndex;
            // 변경된 설정을 파일에 저장
            _settingsService.Save();
        }

        // '추가' 버튼 클릭 이벤트 핸들러
        private async void AddButton_Click(object sender, RoutedEventArgs e)
        {
            // 콤보박스에서 선택된 플랫폼 정보를 가져옴
            var selectedPlatform = PlatformComboBox.SelectedItem as Platform;

            // 날짜 선택기와 시간 텍스트박스의 값을 조합하여 시작 날짜와 시간 생성
            var startDate = CombineDateAndTime(StartDatePicker.SelectedDate, StartTimeTextBox.Text);
            // 날짜 선택기와 시간 텍스트박스의 값을 조합하여 마감 날짜와 시간 생성
            var dueDate = CombineDateAndTime(DueDatePicker.SelectedDate, DueTimeTextBox.Text);

            // 뷰모델을 통해 새 할 일 항목을 비동기적으로 추가
            await _viewModel.AddItemAsync(selectedPlatform, startDate, dueDate);

            // 입력 필드들 초기화
            StartDatePicker.SelectedDate = null;
            StartTimeTextBox.Clear();
            DueDatePicker.SelectedDate = null;
            DueTimeTextBox.Clear();
            PlatformComboBox.SelectedItem = null;
        }

        // nullable DateTime과 시간 문자열을 결합하여 하나의 nullable DateTime으로 반환하는 헬퍼 메서드
        private DateTime? CombineDateAndTime(DateTime? date, string timeString)
        {
            // 날짜 값이 없으면 null 반환
            if (date is null)
            {
                return null;
            }

            // 시간 문자열이 비어있으면 날짜의 0시 0분 0초로 설정하여 반환
            if (string.IsNullOrWhiteSpace(timeString))
            {
                return date.Value.Date;
            }

            // 시간 문자열을 TimeSpan으로 파싱 시도
            if (TimeSpan.TryParse(timeString, out var time))
            {
                // 파싱 성공 시, 날짜와 시간을 합쳐서 반환
                return date.Value.Date + time;
            }

            // 파싱 실패 시, 날짜의 0시 0분 0초로 설정하여 반환
            return date.Value.Date;
        }

        // '마감일 순 정렬' 메뉴 클릭 이벤트 핸들러
        private void SortByDueDate_Click(object sender, RoutedEventArgs e)
        {
            // 뷰모델의 정렬 메서드를 호출. 마감일(DueDate)을 기준으로 정렬하며, null 값은 가장 큰 값으로 취급하여 뒤로 보냄
            _viewModel.SortAllLists(item => item.DueDate ?? DateTime.MaxValue);
        }

        // '시작일 순 정렬' 메뉴 클릭 이벤트 핸들러
        private void SortByStartDate_Click(object sender, RoutedEventArgs e)
        {
            // 뷰모델의 정렬 메서드를 호출. 시작일(StartDate)을 기준으로 정렬하며, null 값은 가장 큰 값으로 취급하여 뒤로 보냄
            _viewModel.SortAllLists(item => item.StartDate ?? DateTime.MaxValue);
        }

        // 리스트 항목의 '삭제' 버튼 클릭 이벤트 핸들러
        private async void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            // 이벤트 발생 소스(sender)가 Button이고, 그 버튼의 DataContext가 TodoItem 객체인지 확인
            if (sender is Button button && button.DataContext is TodoItem item)
            {
                // 뷰모델을 통해 해당 할 일 항목을 비동기적으로 삭제
                await _viewModel.DeleteItemAsync(item);
            }
        }

        // 리스트 항목의 '상태' 콤보박스 선택 변경 이벤트 핸들러
        private async void StatusComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // 이벤트 발생 소스(sender)가 ComboBox이고, 그 콤보박스의 DataContext가 TodoItem 객체인지 확인
            if (sender is ComboBox comboBox && comboBox.DataContext is TodoItem item)
            {
                // 콤보박스가 UI에 로드되었고, 실제로 선택된 항목이 변경되었을 때만 실행 (초기 로드 시 불필요한 업데이트 방지)
                if (comboBox.IsLoaded && e.AddedItems.Count > 0)
                {
                    // 뷰모델을 통해 변경된 항목의 상태를 비동기적으로 업데이트
                    await _viewModel.UpdateItemAsync(item);
                }
            }
        }

        // '설정' 버튼 클릭 이벤트 핸들러
        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            // 의존성 주입 컨테이너(AppHost)를 통해 필요한 서비스들을 가져옴
            var settingsService = App.AppHost!.Services.GetRequiredService<SettingsService>();
            var kakaoService = App.AppHost!.Services.GetRequiredService<KakaoService>();
            var viewModel = App.AppHost!.Services.GetRequiredService<MainViewModel>();
            var scheduler = App.AppHost!.Services.GetRequiredService<NotificationScheduler>();

            // 필요한 서비스들을 전달하여 설정 창 인스턴스를 생성
            var settingsWindow = new SettingsWindow(settingsService, kakaoService, viewModel, scheduler)
            {
                // 이 창(MainWindow)을 설정 창의 부모 창으로 지정
                Owner = this
            };
            // 설정 창을 모달(Modal) 대화상자로 염. 이 창이 닫히기 전까지 부모 창은 조작 불가
            settingsWindow.ShowDialog();
        }

        // '플랫폼 관리' 버튼 클릭 이벤트 핸들러
        private async void PlatformManagementButton_Click(object sender, RoutedEventArgs e)
        {
            // DBContext 같은 일회성 서비스를 사용하기 위해 새로운 서비스 스코프 생성
            using var scope = App.AppHost!.Services.CreateScope();
            // 생성된 스코프에서 AppDbContext 인스턴스를 가져옴
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            // DBContext를 전달하여 플랫폼 관리 창 인스턴스 생성
            var managementWindow = new PlatformManagementWindow(dbContext)
            {
                // 이 창(MainWindow)을 플랫폼 관리 창의 부모 창으로 지정
                Owner = this
            };

            // 플랫폼 관리 창을 모달 대화상자로 염
            managementWindow.ShowDialog();
            // 플랫폼 관리 창이 닫힌 후, 변경사항을 반영하기 위해 뷰모델의 플랫폼 목록을 다시 로드
            await _viewModel.ReloadPlatformsAsync();
        }

        // '플랫폼' 콤보박스가 키보드 포커스를 받았을 때의 이벤트 핸들러
        private void PlatformComboBox_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            // 이벤트 발생 소스가 ComboBox인지 확인
            if (sender is ComboBox comboBox)
            {
                // 콤보박스의 드롭다운 목록을 자동으로 열어줌
                comboBox.IsDropDownOpen = true;
            }
        }

        // 새 항목 입력 관련 컨트롤에서 키를 눌렀을 때의 이벤트 핸들러
        private void NewItem_KeyDown(object sender, KeyEventArgs e)
        {
            // 눌린 키가 Enter 키인지 확인
            if (e.Key == Key.Enter)
            {
                // '추가' 버튼을 클릭한 것과 동일한 로직을 실행
                AddButton_Click(sender, e);
            }
        }

        // 리스트 항목의 '수정' 버튼 클릭 이벤트 핸들러
        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            // 이벤트 발생 소스가 Button이고, 그 버튼의 DataContext가 TodoItem 객체인지 확인
            if (sender is Button button && button.DataContext is TodoItem item)
            {
                // DBContext를 사용하기 위해 새로운 서비스 스코프 생성
                using var scope = App.AppHost!.Services.CreateScope();
                // 생성된 스코프에서 AppDbContext 인스턴스를 가져옴
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                // 수정할 항목, DBContext, MainViewModel을 전달하여 수정 창 인스턴스 생성
                var editWindow = new EditTaskWindow(item, dbContext, _viewModel) { Owner = this };
                // 수정 창을 모달 대화상자로 염
                editWindow.ShowDialog();
            }
        }

        // 창이 닫히기 직전에 호출되는 메서드 (재정의)
        protected override void OnClosing(CancelEventArgs e)
        {
            // 부모 클래스의 OnClosing 로직을 먼저 호출
            base.OnClosing(e);
            // 명시적 종료 플래그(_isExplicitExit)가 false일 경우 (예: 'X' 버튼 클릭)
            if (!_isExplicitExit)
            {
                // 창 닫기 이벤트를 취소하여 프로그램이 종료되지 않도록 함
                e.Cancel = true;
                // 창을 숨겨서 시스템 트레이에 아이콘만 남도록 함
                this.Hide();
            }
        }

        // 시스템 트레이 아이콘을 더블 클릭했을 때의 이벤트 핸들러
        private void MyNotifyIcon_TrayMouseDoubleClick(object sender, RoutedEventArgs e)
        {
            // 숨겨진 창을 다시 화면에 표시
            this.Show();
            // 창의 상태를 최소화/최대화가 아닌 보통 상태로 복원
            this.WindowState = WindowState.Normal;
        }

        // 트레이 아이콘 우클릭 메뉴의 '열기'를 클릭했을 때의 이벤트 핸들러
        private void ShowWindow_Click(object sender, RoutedEventArgs e)
        {
            // 숨겨진 창을 다시 화면에 표시
            this.Show();
            // 창의 상태를 보통 상태로 복원
            this.WindowState = WindowState.Normal;
        }

        // 트레이 아이콘 우클릭 메뉴의 '종료'를 클릭했을 때의 이벤트 핸들러
        private void ExitApplication_Click(object sender, RoutedEventArgs e)
        {
            // 명시적 종료 플래그를 true로 설정
            _isExplicitExit = true;
            // 창 닫기를 시도. OnClosing 이벤트가 호출되지만, _isExplicitExit가 true이므로 프로그램이 완전히 종료됨
            this.Close();
        }
    }
}