using System.Diagnostics;
using System.Net.Http;
using Newtonsoft.Json;
using Microsoft.Extensions.Configuration;

// 서비스 관련 클래스를 포함하는 네임스페이스
namespace Todo.Services
{
    // 카카오 API 연동을 담당하는 서비스 클래스
    public class KakaoService
    {
        // 앱 설정을 읽고 쓰기 위한 서비스
        private readonly SettingsService _settingsService;
        // 카카오 REST API 키
        private readonly string _apiKey;
        // 카카오 OAuth 인증 후 리디렉션될 URI
        private readonly string _redirectUri;
        // HTTP 요청을 보내기 위한 정적 HttpClient 인스턴스 (성능 및 리소스 관리를 위해 정적으로 사용)
        private static readonly HttpClient _httpClient = new HttpClient();

        // KakaoService 생성자. 의존성 주입을 통해 필요한 서비스와 설정을 받음
        public KakaoService(SettingsService settingsService, IConfiguration configuration)
        {
            _settingsService = settingsService;
            // appsettings.json에서 카카오 API 키와 리디렉션 URI를 로드
            _apiKey = configuration["KakaoConfig:ApiKey"]!;
            _redirectUri = configuration["KakaoConfig:RedirectUri"]!;
        }

        // 사용자가 카카오 로그인을 위해 접속해야 할 인증 URL을 생성하는 메서드
        public string GetAuthenticationUrl()
        {
            return $"https://kauth.kakao.com/oauth/authorize?client_id={_apiKey}&redirect_uri={_redirectUri}&response_type=code";
        }

        // 사용자가 인증 후 받은 인증 코드(authCode)를 사용하여 액세스 토큰과 리프레시 토큰을 발급받는 메서드
        public async Task<bool> AuthorizeAsync(string authCode)
        {
            try
            {
                var tokenUrl = "https://kauth.kakao.com/oauth/token";
                // 토큰 발급에 필요한 파라미터들을 FormUrlEncodedContent 형식으로 구성
                var content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    { "grant_type", "authorization_code" }, // 인증 타입: 인증 코드 사용
                    { "client_id", _apiKey },
                    { "redirect_uri", _redirectUri },
                    { "code", authCode }
                });

                var response = await _httpClient.PostAsync(tokenUrl, content); // 카카오 서버에 POST 요청
                response.EnsureSuccessStatusCode(); // HTTP 응답이 성공(2xx)이 아니면 예외 발생

                var json = await response.Content.ReadAsStringAsync(); // 응답 본문을 문자열로 읽음
                var result = JsonConvert.DeserializeObject<KakaoTokenResponse>(json); // JSON을 KakaoTokenResponse 객체로 역직렬화

                // 토큰이 정상적으로 발급되지 않은 경우 실패 반환
                if (result?.AccessToken is null || result?.RefreshToken is null) return false;

                // 발급받은 토큰 정보들을 설정에 저장
                _settingsService.Settings.KakaoAccessToken = result.AccessToken;
                _settingsService.Settings.KakaoRefreshToken = result.RefreshToken;
                _settingsService.Settings.KakaoTokenExpiresAt = DateTime.UtcNow.AddSeconds(result.ExpiresIn);
                _settingsService.Save(); // 설정 변경사항 저장

                return true; // 성공 반환
            }
            catch { return false; } // 예외 발생 시 실패 반환
        }

        // 만료된 액세스 토큰을 리프레시 토큰을 사용해 갱신하는 메서드
        public async Task<bool> TryRefreshAccessTokenAsync()
        {
            // 저장된 리프레시 토큰이 없으면 갱신 불가
            if (string.IsNullOrEmpty(_settingsService.Settings.KakaoRefreshToken)) return false;

            try
            {
                var tokenUrl = "https://kauth.kakao.com/oauth/token";
                var content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    { "grant_type", "refresh_token" }, // 인증 타입: 리프레시 토큰 사용
                    { "client_id", _apiKey },
                    { "refresh_token", _settingsService.Settings.KakaoRefreshToken }
                });

                var response = await _httpClient.PostAsync(tokenUrl, content);

                // 응답이 실패하면 (예: 리프레시 토큰 만료) 로그아웃 처리 후 실패 반환
                if (!response.IsSuccessStatusCode)
                {
                    await LogoutAsync();
                    return false;
                }

                var json = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<KakaoTokenResponse>(json);

                // 새 액세스 토큰이 없으면 실패 반환
                if (result?.AccessToken is null) return false;

                // 새로 발급받은 액세스 토큰과 만료 시간 저장 (만료 5분 전으로 설정하여 안정성 확보)
                _settingsService.Settings.KakaoAccessToken = result.AccessToken;
                _settingsService.Settings.KakaoTokenExpiresAt = DateTime.UtcNow.AddSeconds(result.ExpiresIn - 300);

                // 응답에 새 리프레시 토큰이 포함된 경우 (리프레시 토큰 만료가 임박한 경우) 함께 갱신
                if (result.RefreshToken != null)
                {
                    _settingsService.Settings.KakaoRefreshToken = result.RefreshToken;
                }
                _settingsService.Save();

                return true;
            }
            catch { return false; }
        }

        // 카카오 로그아웃 처리 메서드
        public async Task LogoutAsync()
        {
            // 로컬에 액세스 토큰이 저장되어 있는 경우에만 카카오 서버에 로그아웃 요청
            if (!string.IsNullOrEmpty(_settingsService.Settings.KakaoAccessToken))
            {
                try
                {
                    var logoutUrl = "https://kapi.kakao.com/v1/user/logout";
                    // HTTP 요청 헤더에 Bearer 토큰 인증 정보 추가
                    _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _settingsService.Settings.KakaoAccessToken);
                    await _httpClient.PostAsync(logoutUrl, null);
                }
                catch { /* 서버와의 통신 실패는 무시하고 로컬 데이터만 초기화 */ }
            }

            // 로컬에 저장된 모든 카카오 관련 토큰 정보 및 만료 시간을 초기화
            _settingsService.Settings.KakaoAccessToken = null;
            _settingsService.Settings.KakaoRefreshToken = null;
            _settingsService.Settings.KakaoTokenExpiresAt = DateTime.MinValue;
            _settingsService.Save();
        }

        // '나에게 보내기' API를 사용하여 카카오톡 메시지를 전송하는 메서드
        public async Task SendMessageAsync(string message)
        {
            // 액세스 토큰이 없으면(로그인 상태가 아니면) 메시지 전송 불가
            if (string.IsNullOrEmpty(_settingsService.Settings.KakaoAccessToken)) return;

            // 액세스 토큰이 만료되었으면 갱신 시도
            if (DateTime.UtcNow >= _settingsService.Settings.KakaoTokenExpiresAt)
            {
                bool refreshed = await TryRefreshAccessTokenAsync();
                if (!refreshed) return; // 갱신 실패 시 메시지 전송 불가
            }

            try
            {
                var messageUrl = "https://kapi.kakao.com/v2/api/talk/memo/default/send";
                // 카카오 메시지 템플릿 객체 생성
                var template = new
                {
                    object_type = "text",
                    text = message,
                    link = new { web_url = "", mobile_web_url = "" }
                };
                // 템플릿 객체를 Form 데이터로 변환
                var content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    { "template_object", JsonConvert.SerializeObject(template) }
                });

                // HTTP 요청 헤더에 Bearer 토큰 인증 정보 추가
                _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _settingsService.Settings.KakaoAccessToken);
                var response = await _httpClient.PostAsync(messageUrl, content);
                response.EnsureSuccessStatusCode(); // HTTP 응답이 성공이 아니면 예외 발생
            }
            catch (Exception ex)
            {
                // 메시지 전송 실패 시 디버그 창에 로그 출력
                Debug.WriteLine($"카카오 메시지 전송 실패: {ex.Message}");
            }
        }
    }
}