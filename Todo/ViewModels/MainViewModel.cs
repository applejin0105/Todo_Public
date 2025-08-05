using Microsoft.EntityFrameworkCore;
using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;
using System.Collections.ObjectModel;
using System.ComponentModel;

// 뷰모델 클래스를 포함하는 네임스페이스
namespace Todo.ViewModels
{
    // 프로젝트 내부 네임스페이스 참조
    using Todo.Data;
    using Todo.Models;
    using Todo.Services;

    // INotifyPropertyChanged 인터페이스를 구현하여 속성 변경 시 UI에 알리는 기능을 제공
    public class MainViewModel : INotifyPropertyChanged
    {
        // 데이터베이스 컨텍스트, 설정 서비스, 카카오 서비스를 위한 읽기 전용 필드
        private readonly AppDbContext _context;
        private readonly SettingsService _settingsService;
        private readonly KakaoService _kakaoService;

        // '시작 안 함' 상태의 할 일 목록. ObservableCollection은 UI와 데이터 바인딩 시 목록 변경을 자동으로 감지
        public ObservableCollection<TodoItem> NotStartedItems { get; set; }
        // '진행 중' 상태의 할 일 목록
        public ObservableCollection<TodoItem> InProgressItems { get; set; }
        // '완료' 상태의 할 일 목록
        public ObservableCollection<TodoItem> CompletedItems { get; set; }
        // 모든 플랫폼 목록
        public ObservableCollection<Platform> AllPlatforms { get; set; }

        // 새 할 일 항목의 제목. UI의 입력 필드와 바인딩됨
        public string NewItemTitle { get; set; } = "";
        // 새 할 일 항목의 플랫폼. UI와 바인딩됨
        public string NewItemPlatform { get; set; } = "";

        // MainViewModel 생성자. 의존성 주입으로 필요한 서비스들을 받음
        public MainViewModel(AppDbContext context, SettingsService settingsService, KakaoService kakaoService)
        {
            // 전달받은 인스턴스들을 필드에 할당
            _context = context;
            // 각 할 일 목록과 플랫폼 목록을 초기화
            NotStartedItems = new ObservableCollection<TodoItem>();
            InProgressItems = new ObservableCollection<TodoItem>();
            CompletedItems = new ObservableCollection<TodoItem>();
            AllPlatforms = new ObservableCollection<Platform>();
            _settingsService = settingsService;
            _kakaoService = kakaoService;
        }

        // 데이터베이스에서 모든 항목과 플랫폼을 비동기적으로 로드하는 메서드
        public async Task LoadItemsAsync()
        {
            // 플랫폼 목록을 DB에서 이름순으로 정렬하여 로드
            var platforms = await _context.Platforms.OrderBy(p => p.Name).ToListAsync();
            AllPlatforms.Clear(); // 기존 목록을 비움
            foreach (var p in platforms) // 로드한 플랫폼을 컬렉션에 추가
            {
                AllPlatforms.Add(p);
            }

            // 할 일 항목들을 관련 플랫폼 정보와 함께(Include) DB에서 로드
            var itemsFromDb = await _context.TodoItems.Include(i => i.Platform).ToListAsync();
            NotStartedItems.Clear();
            InProgressItems.Clear();
            CompletedItems.Clear();

            // 로드한 각 항목을 상태에 맞는 목록으로 분배
            foreach (var item in itemsFromDb)
            {
                DistributeItem(item);
            }

            // 모든 목록을 마감일(기본값) 기준으로 정렬
            SortAllLists(item => item.DueDate ?? DateTime.MaxValue);
        }

        // 새 할 일 항목을 추가하는 비동기 메서드
        public async Task AddItemAsync(Platform? selectedPlatform, DateTime? startDate, DateTime? dueDate)
        {
            // 제목이 비어있으면 아무 작업도 하지 않음
            if (string.IsNullOrWhiteSpace(NewItemTitle)) return;

            // 새 TodoItem 객체 생성
            var newItem = new TodoItem
            {
                Title = NewItemTitle,
                Platform = selectedPlatform,
                Status = WorkStatus.NotStarted,
                // 날짜/시간은 DB 저장을 위해 UTC로 변환
                StartDate = startDate?.ToUniversalTime(),
                DueDate = dueDate?.ToUniversalTime()
            };

            _context.TodoItems.Add(newItem); // DB 컨텍스트에 새 항목 추가
            await _context.SaveChangesAsync(); // 변경사항을 DB에 저장

            NotStartedItems.Add(newItem); // UI의 '시작 안 함' 목록에 즉시 추가
            await _kakaoService.SendMessageAsync($"[작업 추가] {newItem.Title}"); // 카카오톡으로 알림 전송

            NewItemTitle = string.Empty; // 입력 필드 초기화
            OnPropertyChanged(nameof(NewItemTitle)); // 제목 속성이 변경되었음을 UI에 알림
        }

        // 기존 할 일 항목을 업데이트하는 비동기 메서드 (주로 상태 변경 시 사용)
        public async Task UpdateItemAsync(TodoItem item)
        {
            // 상태가 '완료'로 변경되면 완료 시간을 현재 UTC 시간으로 기록
            if (item.Status == WorkStatus.Completed)
            {
                item.CompletedDate = DateTime.UtcNow;
            }
            else // 그 외의 상태로 변경되면 완료 시간을 null로 설정
            {
                item.CompletedDate = null;
            }

            _context.Update(item); // DB 컨텍스트에 항목 업데이트
            await _context.SaveChangesAsync(); // 변경사항을 DB에 저장

            RemoveItemFromAllLists(item); // 기존 목록에서 항목 제거
            DistributeItem(item); // 변경된 상태에 맞는 새 목록으로 항목 분배
        }

        // 할 일 항목을 삭제하는 비동기 메서드
        public async Task DeleteItemAsync(TodoItem item)
        {
            var itemToDelete = await _context.TodoItems.FindAsync(item.Id); // ID로 DB에서 항목 찾기
            if (itemToDelete != null)
            {
                _context.TodoItems.Remove(itemToDelete); // DB 컨텍스트에서 항목 제거
                await _context.SaveChangesAsync(); // 변경사항을 DB에 저장
                RemoveItemFromAllLists(item); // 모든 UI 목록에서 항목 제거
            }
        }

        // 모든 상태 목록을 주어진 정렬 기준(keySelector)에 따라 정렬하는 메서드
        public void SortAllLists(Func<TodoItem, object> keySelector)
        {
            SortList(NotStartedItems, keySelector);
            SortList(InProgressItems, keySelector);
        }

        // ObservableCollection을 정렬하는 private 헬퍼 메서드
        private void SortList(ObservableCollection<TodoItem> collection, Func<TodoItem, object> keySelector)
        {
            var sorted = collection.OrderBy(keySelector).ToList(); // LINQ를 사용해 정렬된 새 리스트 생성
            collection.Clear(); // 기존 컬렉션을 비움
            foreach (var item in sorted) // 정렬된 리스트의 항목들을 다시 컬렉션에 추가
            {
                collection.Add(item);
            }
        }

        // 할 일 항목을 상태(Status)에 따라 적절한 목록에 추가하는 헬퍼 메서드
        private void DistributeItem(TodoItem item)
        {
            switch (item.Status)
            {
                case WorkStatus.NotStarted:
                    NotStartedItems.Add(item);
                    break;
                case WorkStatus.InProgress:
                    InProgressItems.Add(item);
                    break;
                case WorkStatus.Completed:
                    CompletedItems.Add(item);
                    break;
            }
        }

        // 모든 목록에서 특정 항목을 제거하는 헬퍼 메서드
        private void RemoveItemFromAllLists(TodoItem item)
        {
            if (NotStartedItems.Contains(item)) NotStartedItems.Remove(item);
            if (InProgressItems.Contains(item)) InProgressItems.Remove(item);
            if (CompletedItems.Contains(item)) CompletedItems.Remove(item);
        }

        // INotifyPropertyChanged 구현을 위한 이벤트와 메서드
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        // 플랫폼 목록만 다시 로드하는 비동기 메서드 (플랫폼 관리 창이 닫힐 때 호출됨)
        public async Task ReloadPlatformsAsync()
        {
            var platforms = await _context.Platforms.OrderBy(p => p.Name).ToListAsync();
            AllPlatforms.Clear();
            foreach (var p in platforms)
            {
                AllPlatforms.Add(p);
            }
        }

        // 프로그램 시작 시 오늘 마감인 작업에 대한 요약 알림을 보내는 메서드
        public void SendStartupSummaryNotification()
        {
            if (!_settingsService.Settings.AreNotificationsEnabled) return; // 알림 설정이 꺼져있으면 중단

            // '시작 안 함'과 '진행 중' 목록에서 오늘이 마감일인 항목들을 필터링
            var itemsToday = NotStartedItems.Concat(InProgressItems)
                .Where(item => item.DueDate.HasValue && item.DueDate.Value.ToLocalTime().Date == DateTime.Today)
                .ToList();

            if (itemsToday.Any())
            {
                var summaryText = new System.Text.StringBuilder();
                summaryText.AppendLine($"오늘 마감 예정인 작업이 {itemsToday.Count}개 있습니다.");

                // 최대 4개의 항목만 요약에 포함
                foreach (var item in itemsToday.Take(4))
                {
                    summaryText.AppendLine($"- {item.Title}");
                }

                if (itemsToday.Count > 4)
                {
                    summaryText.AppendLine("...");
                }

                // Windows 알림 생성 및 표시
                var notification = new AppNotificationBuilder()
                    .AddText("📢 오늘의 할 일 목록")
                    .AddText(summaryText.ToString())
                    .BuildNotification();

                AppNotificationManager.Default.Show(notification);
            }
        }

        // 마감이 임박한(1시간 이내) 작업에 대한 알림을 확인하고 보내는 메서드
        public void CheckForImminentNotifications()
        {
            if (!_settingsService.Settings.AreNotificationsEnabled) return; // 알림 설정이 꺼져있으면 중단

            var now = DateTime.UtcNow;
            // 마감까지 1시간 미만으로 남은 항목들을 필터링
            var imminentItems = NotStartedItems.Concat(InProgressItems)
                .Where(item => item.DueDate.HasValue &&
                               item.DueDate.Value > now &&
                               (item.DueDate.Value - now).TotalHours < 1)
                .ToList();

            // 각 임박 항목에 대해 알림 생성 및 표시
            foreach (var item in imminentItems)
            {
                var notification = new AppNotificationBuilder()
                    .AddText("⏰ 마감 임박!")
                    .AddText($"'{item.Title}' 작업의 마감이 1시간 이내로 남았습니다.")
                    .BuildNotification();

                AppNotificationManager.Default.Show(notification);
            }
        }

        // 시작 시간이 된 작업을 자동으로 '진행 중' 상태로 변경하는 비동기 메서드
        public async Task AutoProgressTasksAsync()
        {
            var now = DateTime.UtcNow;
            // '시작 안 함' 목록에서 시작 시간이 현재 시간보다 이전인 항목들을 필터링
            var tasksToStart = NotStartedItems
                .Where(i => i.StartDate.HasValue && i.StartDate.Value <= now)
                .ToList();

            if (!tasksToStart.Any()) return; // 대상 작업이 없으면 중단

            foreach (var item in tasksToStart)
            {
                item.Status = WorkStatus.InProgress; // 상태를 '진행 중'으로 변경
                await UpdateItemAsync(item); // 변경사항을 DB와 UI에 반영
                await _kakaoService.SendMessageAsync($"[작업 시작] '{item.Title}' 작업이 시작되었습니다.");
            }
        }
    }
}