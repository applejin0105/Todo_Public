// 모델(데이터 구조) 관련 클래스를 포함하는 네임스페이스
namespace Todo.Models
{
    // 애플리케이션의 모든 설정을 저장하기 위한 데이터 클래스 (POCO)
    public class AppSettings
    {
        // UI 테마 설정 ("Light" 또는 "Dark"). 기본값은 "Light".
        public string Theme { get; set; } = "Light";
        // '항상 위에 표시' 기능 활성화 여부. 기본값은 false.
        public bool IsAlwaysOnTop { get; set; } = false;
        // 알림 기능 전체 활성화 여부. 기본값은 true.
        public bool AreNotificationsEnabled { get; set; } = true;

        // --- 창 크기 및 위치 저장 ---
        // 마지막으로 사용된 창의 높이. 기본값은 700.
        public double WindowHeight { get; set; } = 700;
        // 마지막으로 사용된 창의 너비. 기본값은 900.
        public double WindowWidth { get; set; } = 900;
        // 마지막으로 사용된 창의 Y 좌표. 기본값은 100.
        public double WindowTop { get; set; } = 100;
        // 마지막으로 사용된 창의 X 좌표. 기본값은 100.
        public double WindowLeft { get; set; } = 100;

        // 마지막으로 선택했던 탭의 인덱스. 기본값은 0 (미진행).
        public int LastSelectedTabIndex { get; set; } = 0;

        // --- 카카오 API 관련 설정 ---
        // 카카오 API 액세스 토큰. null일 수 있음.
        public string? KakaoAccessToken { get; set; }
        // 카카오 API 리프레시 토큰. null일 수 있음.
        public string? KakaoRefreshToken { get; set; }
        // 카카오 액세스 토큰의 만료 시각 (UTC 기준).
        public DateTime KakaoTokenExpiresAt { get; set; }
    }
}