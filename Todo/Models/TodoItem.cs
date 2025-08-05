using System.ComponentModel;

// 모델(데이터 구조) 관련 클래스를 포함하는 네임스페이스
namespace Todo.Models
{
    // 하나의 할 일 항목을 나타내는 핵심 모델 클래스.
    // INotifyPropertyChanged 인터페이스를 구현하여 속성 값이 변경될 때 UI에 자동으로 알릴 수 있음.
    public class TodoItem : INotifyPropertyChanged
    {
        // 속성 변경을 알리기 위한 이벤트 핸들러
        public event PropertyChangedEventHandler? PropertyChanged;

        // PropertyChanged 이벤트를 안전하게 발생시키는 보호된(protected) 헬퍼 메서드
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // --- 속성(Property) 및 백업 필드(Backing Field) ---

        // Id 속성의 비공개 백업 필드
        private int _id;
        // 할 일 항목의 고유 식별자 (데이터베이스의 기본 키)
        public int Id
        {
            get => _id;
            set { _id = value; OnPropertyChanged(nameof(Id)); } // 값이 변경되면 UI에 알림
        }

        // Title 속성의 비공개 백업 필드
        private string _title = string.Empty;
        // 할 일 항목의 제목
        public string Title
        {
            get => _title;
            set { _title = value; OnPropertyChanged(nameof(Title)); }
        }

        // PlatformId 속성의 비공개 백업 필드
        private int? _platformId;
        // 연관된 Platform의 외래 키(Foreign Key). 플랫폼이 없을 수도 있으므로 nullable.
        public int? PlatformId
        {
            get => _platformId;
            set { _platformId = value; OnPropertyChanged(nameof(PlatformId)); }
        }

        // Platform 속성의 비공개 백업 필드
        private Platform? _platform;
        // 연관된 Platform 객체에 대한 참조 (네비게이션 속성). Entity Framework Core가 관계를 매핑하는 데 사용.
        public Platform? Platform
        {
            get => _platform;
            set { _platform = value; OnPropertyChanged(nameof(Platform)); }
        }

        // CreatedAt 속성의 비공개 백업 필드. 기본값은 현재 UTC 시간.
        private DateTime _createdAt = DateTime.UtcNow;
        // 할 일 항목이 생성된 시각
        public DateTime CreatedAt
        {
            get => _createdAt;
            set { _createdAt = value; OnPropertyChanged(nameof(CreatedAt)); }
        }

        // StartDate 속성의 비공개 백업 필드
        private DateTime? _startDate;
        // 작업 시작 예정일. 날짜가 지정되지 않을 수 있으므로 nullable.
        public DateTime? StartDate
        {
            get => _startDate;
            set { _startDate = value; OnPropertyChanged(nameof(StartDate)); }
        }

        // DueDate 속성의 비공개 백업 필드
        private DateTime? _dueDate;
        // 작업 마감 예정일. 날짜가 지정되지 않을 수 있으므로 nullable.
        public DateTime? DueDate
        {
            get => _dueDate;
            set { _dueDate = value; OnPropertyChanged(nameof(DueDate)); }
        }

        // Status 속성의 비공개 백업 필드. 기본값은 '시작 안 함'.
        private WorkStatus _status = WorkStatus.NotStarted;
        // 작업의 현재 진행 상태 (WorkStatus 열거형 사용)
        public WorkStatus Status
        {
            get => _status;
            set { _status = value; OnPropertyChanged(nameof(Status)); }
        }

        // CompletedDate 속성의 비공개 백업 필드
        private DateTime? _completedDate;
        // 작업이 완료된 시각. 완료되지 않았으면 null.
        public DateTime? CompletedDate
        {
            get => _completedDate;
            set { _completedDate = value; OnPropertyChanged(nameof(CompletedDate)); }
        }
    }
}