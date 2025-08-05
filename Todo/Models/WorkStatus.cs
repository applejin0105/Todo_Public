// 모델(데이터 구조) 관련 클래스를 포함하는 네임스페이스
namespace Todo.Models
{
    // 할 일 항목(TodoItem)의 진행 상태를 나타내는 열거형(Enumeration)
    public enum WorkStatus
    {
        // 작업이 아직 시작되지 않은 상태
        NotStarted,
        // 작업이 현재 진행 중인 상태
        InProgress,
        // 작업이 완료된 상태
        Completed
    }
}