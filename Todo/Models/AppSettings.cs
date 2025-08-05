namespace Todo.Models
{
    public class AppSettings
    {
        public string Theme { get; set; } = "Light";
        public bool IsAlwaysOnTop { get; set; } = false;
        public bool AreNotificationsEnabled { get; set; } = true;

        // 창 크기 및 위치 저장
        public double WindowHeight { get; set; } = 700;
        public double WindowWidth { get; set; } = 900;
        public double WindowTop { get; set; } = 100;
        public double WindowLeft { get; set; } = 100;

        public int LastSelectedTabIndex { get; set; } = 0; // 0: 미진행, 1: 진행, 2: 완료

        public string? KakaoAccessToken { get; set; }
        public string? KakaoRefreshToken { get; set; }
        public DateTime KakaoTokenExpiresAt { get; set; }
    }
}
