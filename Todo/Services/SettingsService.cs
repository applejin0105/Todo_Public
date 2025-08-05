using Newtonsoft.Json;
using System.ComponentModel;
using System.IO;
using System.Windows;
using Todo.Models;
using Windows.ApplicationModel;

// 서비스 관련 클래스를 포함하는 네임스페이스
namespace Todo.Services
{
    // INotifyPropertyChanged: 속성 변경 시 UI에 알리는 기능을 제공하는 인터페이스
    public class SettingsService : INotifyPropertyChanged
    {
        // 애플리케이션 데이터를 저장할 폴더 경로 (%AppData%\Todo)
        private static readonly string AppDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Todo");
        // 설정 파일의 이름
        private const string SettingsFile = "settings.json";
        // 설정 파일의 전체 경로
        private static readonly string SettingsFilePath = Path.Combine(AppDataFolder, SettingsFile);

        // 속성 변경을 알리기 위한 이벤트
        public event PropertyChangedEventHandler? PropertyChanged;
        // 속성 변경 이벤트를 발생시키는 메서드
        public void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        // 애플리케이션의 모든 설정을 담고 있는 객체
        public AppSettings Settings { get; private set; }

        // SettingsService 생성자
        public SettingsService()
        {
            // 애플리케이션 데이터 폴더가 없으면 생성
            Directory.CreateDirectory(AppDataFolder);
            // 파일에서 설정을 로드하여 Settings 속성을 초기화
            Settings = Load();
        }

        // 파일에서 설정을 로드하는 비공개 메서드
        private AppSettings Load()
        {
            try
            {
                // 설정 파일이 존재하는지 확인
                if (File.Exists(SettingsFilePath))
                {
                    // 파일의 모든 텍스트(JSON)를 읽음
                    var json = File.ReadAllText(SettingsFilePath);
                    // JSON 문자열을 AppSettings 객체로 역직렬화. 실패 시 새 객체 생성
                    return JsonConvert.DeserializeObject<AppSettings>(json) ?? new AppSettings();
                }
            }
            catch (Exception) { /* 파일 읽기 또는 역직렬화 오류 시 무시하고 기본 설정으로 진행 */ }
            // 파일이 없거나 오류 발생 시 기본 설정을 담은 새 AppSettings 객체를 반환
            return new AppSettings();
        }

        // 현재 설정 상태를 파일에 저장하는 공개 메서드
        public void Save()
        {
            try
            {
                // Settings 객체를 읽기 좋은 형식(들여쓰기)의 JSON 문자열로 직렬화
                var json = JsonConvert.SerializeObject(Settings, Formatting.Indented);
                // JSON 문자열을 설정 파일에 씀 (기존 파일은 덮어씀)
                File.WriteAllText(SettingsFilePath, json);
            }
            catch (Exception) { /* 파일 쓰기 오류 시 무시 */ }
        }

        // '윈도우 시작 시 자동 실행' 설정을 변경하는 비동기 메서드
        public async Task SetStartupAsync(bool isEnabled)
        {
            try
            {
                // 앱 매니페스트에 정의된 StartupTask ID로 시작 작업 객체를 가져옴
                var task = await StartupTask.GetAsync("StartupTask");

                // 자동 실행을 활성화하려 하고, 현재 상태가 '비활성화'일 때
                if (isEnabled && task.State == StartupTaskState.Disabled)
                {
                    // 사용자에게 활성화를 요청. UAC 권한이 필요할 수 있음
                    var result = await task.RequestEnableAsync();
                    // 요청 후에도 활성화되지 않았다면 예외 발생
                    if (result != StartupTaskState.Enabled)
                        throw new Exception("자동 시작 활성화 실패");
                }
                // 자동 실행을 비활성화하려 하고, 현재 상태가 '활성화'일 때
                else if (!isEnabled && task.State == StartupTaskState.Enabled)
                {
                    // 자동 시작 작업을 비활성화
                    task.Disable();
                }
            }
            catch (Exception ex)
            {
                // 설정 과정에서 오류 발생 시 사용자에게 메시지 박스로 알림
                MessageBox.Show($"시작 프로그램 설정 오류:\n{ex.Message}");
            }
        }

        // '윈도우 시작 시 자동 실행'이 현재 활성화되어 있는지 확인하는 메서드
        public bool IsStartupEnabled()
        {
            try
            {
                // 시작 작업 객체를 동기적으로 가져와서 상태를 확인
                var task = StartupTask.GetAsync("StartupTask").GetAwaiter().GetResult();
                // 상태가 '활성화'이면 true 반환
                return task.State == StartupTaskState.Enabled;
            }
            catch
            {
                // 오류 발생 시 false 반환
                return false;
            }
        }
    }
}