using MahApps.Metro.Controls;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;

// Todo 애플리케이션의 뷰(UI) 관련 클래스를 포함하는 네임스페이스
namespace Todo.Views
{
    // 필요한 네임스페이스 using 선언
    using Todo.Data;
    using Todo.Models;
    using Todo.ViewModels;

    // 할 일 항목 수정을 위한 MetroWindow 창 클래스
    public partial class EditTaskWindow : MetroWindow
    {
        // 현재 수정 중인 TodoItem 객체. XAML에서 바인딩하기 위해 public으로 선언
        public TodoItem EditingItem { get; }
        // 데이터베이스 컨텍스트의 읽기 전용 인스턴스. 데이터베이스 변경사항 저장을 위해 사용됨
        private readonly AppDbContext _context;
        // MainViewModel의 참조. 플랫폼 목록과 같은 공유 데이터에 접근하기 위해 사용
        private readonly MainViewModel _mainViewModel;

        // 시작 시간을 문자열로 바인딩하기 위한 속성
        public string StartTime { get; set; }
        // 마감 시간을 문자열로 바인딩하기 위한 속성
        public string DueTime { get; set; }

        // 플랫폼 목록을 바인딩하기 위한 속성. MainViewModel의 목록을 그대로 사용
        public ObservableCollection<Platform> AllPlatforms => _mainViewModel.AllPlatforms;

        // EditTaskWindow의 생성자. 수정할 항목(item), DB 컨텍스트, MainViewModel을 전달받음
        public EditTaskWindow(TodoItem item, AppDbContext context, MainViewModel mainViewModel)
        {
            // XAML에 정의된 UI 컴포넌트들을 초기화
            InitializeComponent();
            // 전달받은 item을 EditingItem 속성에 할당
            EditingItem = item;
            // 전달받은 context를 _context 필드에 할당
            _context = context;
            // 전달받은 mainViewModel을 _mainViewModel 필드에 할당
            _mainViewModel = mainViewModel;

            // 기존 항목의 시작 시간을 "HH:mm" 형식의 문자열로 변환하여 StartTime 속성에 할당
            // DB에는 UTC 시간으로 저장되어 있으므로, 표시를 위해 로컬 시간으로 변환
            StartTime = EditingItem.StartDate?.ToLocalTime().ToString("HH:mm") ?? "";
            // 기존 항목의 마감 시간을 "HH:mm" 형식의 문자열로 변환하여 DueTime 속성에 할당
            DueTime = EditingItem.DueDate?.ToLocalTime().ToString("HH:mm") ?? "";

            // 이 창의 데이터 컨텍스트를 자기 자신(this)으로 설정. XAML에서 이 클래스의 속성들을 바인딩할 수 있게 됨
            DataContext = this;
        }

        // '저장' 버튼 클릭 이벤트 핸들러
        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            // UI에서 수정된 시간(StartTime 문자열)을 다시 날짜와 조합하고, DB 저장을 위해 UTC로 변환
            EditingItem.StartDate = CombineDateAndTime(EditingItem.StartDate, StartTime)?.ToUniversalTime();
            // UI에서 수정된 시간(DueTime 문자열)을 다시 날짜와 조합하고, DB 저장을 위해 UTC로 변환
            EditingItem.DueDate = CombineDateAndTime(EditingItem.DueDate, DueTime)?.ToUniversalTime();

            // DB Context를 통해 변경된 내용을 데이터베이스에 비동기적으로 저장
            await _context.SaveChangesAsync();
            // 이 대화상자의 결과를 true로 설정하여, 부모 창에서 저장이 성공했음을 알림
            this.DialogResult = true;
            // 창을 닫음
            this.Close();
        }

        // nullable DateTime과 시간 문자열을 결합하여 하나의 nullable DateTime으로 반환하는 헬퍼 메서드
        private DateTime? CombineDateAndTime(DateTime? date, string timeString)
        {
            // 날짜 값이 없으면 null 반환
            if (date is null) return null;
            // 시간 문자열이 비어있으면 날짜의 0시 0분 0초로 설정하여 반환
            if (string.IsNullOrWhiteSpace(timeString)) return date.Value.Date;
            // 시간 문자열을 TimeSpan으로 파싱 시도
            if (TimeSpan.TryParse(timeString, out var time))
            {
                // 파싱 성공 시, 날짜와 시간을 합쳐서 반환
                return date.Value.Date + time;
            }
            // 파싱 실패 시, 날짜의 0시 0분 0초로 설정하여 반환
            return date.Value.Date;
        }

        // 창 전체에서 키 입력 이벤트를 미리 감지하는 핸들러
        private void MetroWindow_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // 눌린 키가 Escape 키인지 확인
            if (e.Key == Key.Escape)
            {
                // '저장' 버튼을 클릭한 것과 동일한 로직을 실행 (저장 후 닫기)
                SaveButton_Click(sender, e);
            }
        }
    }
}