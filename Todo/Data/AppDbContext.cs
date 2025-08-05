using Microsoft.EntityFrameworkCore;

// 데이터 접근 관련 클래스를 포함하는 네임스페이스
namespace Todo.Data
{
    // 데이터베이스 테이블과 매핑될 모델 클래스가 있는 네임스페이스
    using Todo.Models;
    // Entity Framework Core의 DbContext를 상속받아 데이터베이스 세션을 나타내는 클래스.
    // 이 클래스를 통해 데이터베이스와 상호작용(조회, 추가, 수정, 삭제)함.
    public class AppDbContext : DbContext
    {
        // AppDbContext의 생성자.
        // 의존성 주입을 통해 데이터베이스 연결 문자열 등의 설정을 담고 있는 DbContextOptions를 받아서
        // 부모 클래스인 DbContext에 전달함.
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        // TodoItems 테이블에 접근하기 위한 DbSet.
        // 이를 통해 TodoItem 객체들을 조회, 추가, 수정, 삭제(CRUD)할 수 있음.
        public DbSet<TodoItem> TodoItems { get; set; }

        // Platforms 테이블에 접근하기 위한 DbSet.
        // 이를 통해 Platform 객체들을 조회, 추가, 수정, 삭제(CRUD)할 수 있음.
        public DbSet<Platform> Platforms { get; set; }
    }
}