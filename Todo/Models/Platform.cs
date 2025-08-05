// 모델(데이터 구조) 관련 클래스를 포함하는 네임스페이스
namespace Todo.Models
{
    // 할 일 항목(TodoItem)이 속한 플랫폼(예: 업무, 개인, 학습)을 나타내는 데이터 모델 클래스
    // 이 클래스는 Entity Framework Core에 의해 데이터베이스의 테이블과 매핑될 가능성이 높음
    public class Platform
    {
        // 플랫폼의 고유 식별자. 데이터베이스의 기본 키(Primary Key)로 사용됨
        public int Id { get; set; }
        // 플랫폼의 이름. 기본값은 빈 문자열
        public string Name { get; set; } = string.Empty;
    }
}