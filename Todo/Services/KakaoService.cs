using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Newtonsoft.Json;
using Microsoft.Extensions.Configuration;

namespace Todo.Services
{
    public class KakaoService
    {
        private readonly SettingsService _settingsService;
        private readonly string _apiKey;
        private readonly string _redirectUri;
        private static readonly HttpClient _httpClient = new HttpClient();

        public KakaoService(SettingsService settingsService, IConfiguration configuration)
        {
            _settingsService = settingsService;
            _apiKey = configuration["KakaoConfig:ApiKey"]!;
            _redirectUri = configuration["KakaoConfig:RedirectUri"]!;
        }

        public string GetAuthenticationUrl()
        {
            return $"https://kauth.kakao.com/oauth/authorize?client_id={_apiKey}&redirect_uri={_redirectUri}&response_type=code";
        }

        public async Task<bool> AuthorizeAsync(string authCode)
        {
            try
            {
                var tokenUrl = "https://kauth.kakao.com/oauth/token";
                var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                { "grant_type", "authorization_code" },
                { "client_id", _apiKey },
                { "redirect_uri", _redirectUri },
                { "code", authCode }
            });

                var response = await _httpClient.PostAsync(tokenUrl, content);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<KakaoTokenResponse>(json);

                if (result?.AccessToken is null || result?.RefreshToken is null) return false;

                _settingsService.Settings.KakaoAccessToken = result.AccessToken;
                _settingsService.Settings.KakaoRefreshToken = result.RefreshToken;
                _settingsService.Settings.KakaoTokenExpiresAt = DateTime.UtcNow.AddSeconds(result.ExpiresIn);
                _settingsService.Save();

                return true;
            }
            catch { return false; }
        }

        public async Task<bool> TryRefreshAccessTokenAsync()
        {
            if (string.IsNullOrEmpty(_settingsService.Settings.KakaoRefreshToken)) return false;

            try
            {
                var tokenUrl = "https://kauth.kakao.com/oauth/token";
                var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "grant_type", "refresh_token" },
            { "client_id", _apiKey },
            { "refresh_token", _settingsService.Settings.KakaoRefreshToken }
        });

                var response = await _httpClient.PostAsync(tokenUrl, content);

                if (!response.IsSuccessStatusCode)
                {
                    await LogoutAsync();
                    return false;
                }

                var json = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<KakaoTokenResponse>(json);

                if (result?.AccessToken is null) return false;

                _settingsService.Settings.KakaoAccessToken = result.AccessToken;
                _settingsService.Settings.KakaoTokenExpiresAt = DateTime.UtcNow.AddSeconds(result.ExpiresIn - 300);

                if (result.RefreshToken != null)
                {
                    _settingsService.Settings.KakaoRefreshToken = result.RefreshToken;
                }
                _settingsService.Save();

                return true;
            }
            catch { return false; }
        }

        public async Task LogoutAsync()
        {
            if (!string.IsNullOrEmpty(_settingsService.Settings.KakaoAccessToken))
            {
                try
                {
                    var logoutUrl = "https://kapi.kakao.com/v1/user/logout";
                    _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _settingsService.Settings.KakaoAccessToken);
                    await _httpClient.PostAsync(logoutUrl, null);
                }
                catch { }
            }

            _settingsService.Settings.KakaoAccessToken = null;
            _settingsService.Settings.KakaoRefreshToken = null;
            _settingsService.Settings.KakaoTokenExpiresAt = DateTime.MinValue;
            _settingsService.Save();
        }

        public async Task SendMessageAsync(string message)
        {
            if (string.IsNullOrEmpty(_settingsService.Settings.KakaoAccessToken)) return;

            if (DateTime.UtcNow >= _settingsService.Settings.KakaoTokenExpiresAt)
            {
                bool refreshed = await TryRefreshAccessTokenAsync();
                if (!refreshed) return;
            }
            try
            {
                var messageUrl = "https://kapi.kakao.com/v2/api/talk/memo/default/send";
                var template = new
                {
                    object_type = "text",
                    text = message,
                    link = new { web_url = "", mobile_web_url = "" }
                };
                var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                { "template_object", JsonConvert.SerializeObject(template) }
            });

                _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _settingsService.Settings.KakaoAccessToken);
                var response = await _httpClient.PostAsync(messageUrl, content);
                response.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"카카오 메시지 전송 실패: {ex.Message}");
            }
        }
    }
}
