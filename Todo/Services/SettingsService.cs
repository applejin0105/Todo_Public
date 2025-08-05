using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows;
using Todo.Models;
using Windows.ApplicationModel;

namespace Todo.Services
{
    public class SettingsService : INotifyPropertyChanged
    {
        private static readonly string AppDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Todo");
        private const string SettingsFile = "settings.json";
        private static readonly string SettingsFilePath = Path.Combine(AppDataFolder, SettingsFile);

        public event PropertyChangedEventHandler? PropertyChanged;
        public void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        public AppSettings Settings { get; private set; }

        public SettingsService()
        {
            Directory.CreateDirectory(AppDataFolder);
            Settings = Load();
        }

        private AppSettings Load()
        {
            try
            {
                if (File.Exists(SettingsFilePath))
                {
                    var json = File.ReadAllText(SettingsFilePath);
                    return JsonConvert.DeserializeObject<AppSettings>(json) ?? new AppSettings();
                }
            }
            catch (Exception) { }
            return new AppSettings();
        }

        public void Save()
        {
            try
            {
                var json = JsonConvert.SerializeObject(Settings, Formatting.Indented);
                File.WriteAllText(SettingsFilePath, json);
            }
            catch (Exception) { }
        }

        public async Task SetStartupAsync(bool isEnabled)
        {
            try
            {
                var task = await StartupTask.GetAsync("StartupTask");

                if (isEnabled && task.State == StartupTaskState.Disabled)
                {
                    var result = await task.RequestEnableAsync();
                    if (result != StartupTaskState.Enabled)
                        throw new Exception("자동 시작 활성화 실패");
                }
                else if (!isEnabled && task.State == StartupTaskState.Enabled)
                {
                    task.Disable();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"시작 프로그램 설정 오류:\n{ex.Message}");
            }
        }


        public bool IsStartupEnabled()
        {
            try
            {
                var task = StartupTask.GetAsync("StartupTask").GetAwaiter().GetResult();
                return task.State == StartupTaskState.Enabled;
            }
            catch
            {
                return false;
            }
        }

    }
}
