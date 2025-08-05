using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System.IO;

// 데이터 접근 관련 클래스를 포함하는 네임스페이스
namespace Todo.Data
{
    // EF Core의 디자인 타임 도구(예: 'dotnet ef migrations add')가 DbContext 인스턴스를 생성하는 방법을 제공하는 팩토리 클래스.
    // 이 클래스는 애플리케이션 실행 시점이 아닌, 개발 및 마이그레이션 단계에서만 사용됨.
    public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
    {
        // IDesignTimeDbContextFactory 인터페이스의 유일한 메서드. DbContext 인스턴스를 생성하여 반환.
        public AppDbContext CreateDbContext(string[] args)
        {
            // ConfigurationBuilder를 사용하여 appsettings.json 파일에서 설정을 로드
            IConfigurationRoot configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory()) // 기본 경로를 현재 디렉터리로 설정
                .AddJsonFile("appsettings.json") // 설정 파일 지정
                .Build(); // 구성 빌드

            // DbContext 옵션을 구성하기 위한 빌더 생성
            var builder = new DbContextOptionsBuilder<AppDbContext>();
            // appsettings.json에서 "DefaultConnection"이라는 이름의 연결 문자열을 가져옴
            var connectionString = configuration.GetConnectionString("DefaultConnection");

            // PostgreSQL 데이터베이스 공급자(Npgsql)를 사용하도록 설정하고 연결 문자열을 전달
            builder.UseNpgsql(connectionString);

            // 구성된 옵션을 사용하여 AppDbContext의 새 인스턴스를 생성하여 반환
            return new AppDbContext(builder.Options);
        }
    }
}