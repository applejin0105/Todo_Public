// JSON 직렬화/역직렬화를 위한 Newtonsoft.Json 라이브러리
using Newtonsoft.Json;

// 카카오 토큰 API의 응답 데이터를 담기 위한 모델 클래스
public class KakaoTokenResponse
{
    // JSON 응답의 "access_token" 필드와 매핑되는 속성
    [JsonProperty("access_token")]
    public string? AccessToken { get; set; }

    // JSON 응답의 "refresh_token" 필드와 매핑되는 속성
    [JsonProperty("refresh_token")]
    public string? RefreshToken { get; set; }

    // 액세스 토큰의 만료 시간(초 단위)을 나타내는 "expires_in" 필드와 매핑되는 속성
    [JsonProperty("expires_in")]
    public int ExpiresIn { get; set; }
}