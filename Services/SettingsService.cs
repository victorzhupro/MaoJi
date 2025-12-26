using System.IO;
using System.Text.Json;
using MaoJi.Models;

namespace MaoJi.Services
{
    /// <summary>
    /// 设置服务 - 管理应用程序设置的持久化
    /// </summary>
    public class SettingsService
    {
        private static readonly Lazy<SettingsService> _instance = new(() => new SettingsService());
        public static SettingsService Instance => _instance.Value;

        private readonly string _settingsPath;
        private AppSettings _currentSettings;

        public AppSettings CurrentSettings => _currentSettings;

        private SettingsService()
        {
            var appDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "MaoJi");
            
            Directory.CreateDirectory(appDataPath);
            _settingsPath = Path.Combine(appDataPath, "settings.json");
            _currentSettings = new AppSettings();
        }

        /// <summary>
        /// 加载设置
        /// </summary>
        public AppSettings LoadSettings()
        {
            try
            {
                if (File.Exists(_settingsPath))
                {
                    var json = File.ReadAllText(_settingsPath);
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    _currentSettings = JsonSerializer.Deserialize<AppSettings>(json, options) ?? new AppSettings();
                }
            }
            catch (Exception)
            {
                _currentSettings = new AppSettings();
            }

            return _currentSettings;
        }

        /// <summary>
        /// 保存设置 (Async)
        /// </summary>
        public async Task SaveSettingsAsync()
        {
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                var json = JsonSerializer.Serialize(_currentSettings, options);
                await File.WriteAllTextAsync(_settingsPath, json);
            }
            catch (Exception)
            {
                // 忽略保存错误
            }
        }

        /// <summary>
        /// 保存设置
        /// </summary>
        public void SaveSettings()
        {
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                var json = JsonSerializer.Serialize(_currentSettings, options);
                File.WriteAllText(_settingsPath, json);
            }
            catch (Exception)
            {
                // 忽略保存错误
            }
        }

        /// <summary>
        /// 更新设置 (Async)
        /// </summary>
        public async Task UpdateSettingsAsync(Action<AppSettings> updateAction)
        {
            updateAction(_currentSettings);
            await SaveSettingsAsync();
        }

        /// <summary>
        /// 更新设置
        /// </summary>
        public void UpdateSettings(Action<AppSettings> updateAction)
        {
            updateAction(_currentSettings);
            SaveSettings();
        }
    }
}

