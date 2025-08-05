using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

using MahApps.Metro.Controls;
using Microsoft.EntityFrameworkCore;

// Todo 애플리케이션의 뷰(UI) 관련 클래스를 포함하는 네임스페이스
namespace Todo.Views
{
    // 내부 using 선언으로 코드 간결화
    using Todo.Data;
    using Todo.Models;

    // 플랫폼 관리를 위한 MetroWindow 창 클래스
    public partial class PlatformManagementWindow : MetroWindow
    {
        // 데이터베이스 컨텍스트의 읽기 전용 인스턴스. 데이터베이스 작업을 위해 사용됨
        private readonly AppDbContext _context;

        // PlatformManagementWindow의 생성자. 의존성 주입을 통해 AppDbContext를 받음
        public PlatformManagementWindow(AppDbContext context)
        {
            // XAML에 정의된 UI 컴포넌트들을 초기화
            InitializeComponent();
            // 전달받은 context 인스턴스를 클래스 필드에 할당
            _context = context;
            // 창이 열릴 때 플랫폼 목록을 로드하는 메서드 호출
            LoadPlatforms();
        }

        // 데이터베이스에서 플랫폼 목록을 로드하여 ListView에 바인딩하는 메서드
        private void LoadPlatforms()
        {
            // DB에서 플랫폼 목록을 이름 순으로 정렬하여 가져온 후, ListView의 ItemsSource에 할당
            PlatformListView.ItemsSource = _context.Platforms.OrderBy(p => p.Name).ToList();
        }

        // '플랫폼 추가' 버튼 클릭 이벤트 핸들러
        private async void AddPlatform_Click(object sender, RoutedEventArgs e)
        {
            // 텍스트박스에서 새 플랫폼 이름을 가져옴
            var newName = NewPlatformTextBox.Text;
            // 플랫폼 이름이 공백이 아닌지 확인
            if (!string.IsNullOrWhiteSpace(newName))
            {
                // 데이터베이스에 동일한 이름의 플랫폼이 이미 존재하는지 비동기적으로 확인
                if (!await _context.Platforms.AnyAsync(p => p.Name == newName))
                {
                    // 존재하지 않으면 새 Platform 객체를 생성하여 DB Context에 추가
                    _context.Platforms.Add(new Platform { Name = newName });
                    // 변경사항을 데이터베이스에 비동기적으로 저장
                    await _context.SaveChangesAsync();
                    // 입력 텍스트박스를 비움
                    NewPlatformTextBox.Clear();
                    // 플랫폼 목록을 다시 로드하여 추가된 항목을 화면에 반영
                    LoadPlatforms();
                }
                else
                {
                    // 이미 존재하는 경우 사용자에게 알림 메시지 표시
                    MessageBox.Show("Already exist Platform.");
                }
            }
        }

        // '플랫폼 삭제' 버튼 클릭 이벤트 핸들러
        private async void DeletePlatform_Click(object sender, RoutedEventArgs e)
        {
            // 이벤트 발생 소스(sender)가 Button이고, 그 버튼의 Tag 속성에 플랫폼 ID(int)가 있는지 확인
            if (sender is Button button && button.Tag is int platformId)
            {
                // 해당 ID를 가진 플랫폼을 데이터베이스에서 비동기적으로 찾음
                var platformToDelete = await _context.Platforms.FindAsync(platformId);
                // 플랫폼이 성공적으로 찾아졌는지 확인
                if (platformToDelete != null)
                {
                    // DB Context에서 해당 플랫폼을 제거 대상으로 표시
                    _context.Platforms.Remove(platformToDelete);
                    // 변경사항(삭제)을 데이터베이스에 비동기적으로 저장
                    await _context.SaveChangesAsync();
                    // 플랫폼 목록을 다시 로드하여 삭제된 항목을 화면에서 제거
                    LoadPlatforms();
                }
            }
        }

        // 새 플랫폼 입력 텍스트박스에서 키를 눌렀을 때의 이벤트 핸들러
        private void NewPlatform_KeyDown(object sender, KeyEventArgs e)
        {
            // 눌린 키가 Enter 키인지 확인
            if (e.Key == Key.Enter)
            {
                // '플랫폼 추가' 버튼을 클릭한 것과 동일한 로직을 실행
                AddPlatform_Click(sender, e);
            }
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
