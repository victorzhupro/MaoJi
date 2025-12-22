using System.IO;
using System.Text;
using System.Windows.Threading;
using MaoJi.Models;

namespace MaoJi.Services
{
    /// <summary>
    /// 自动保存服务
    /// </summary>
    public class AutoSaveService
    {
        private readonly DispatcherTimer _timer;
        private readonly string _autoSavePath;
        private Func<IEnumerable<NoteTab>>? _getTabsFunc;
        private bool _isEnabled;

        public bool IsEnabled
        {
            get => _isEnabled;
            set
            {
                _isEnabled = value;
                if (value)
                    _timer.Start();
                else
                    _timer.Stop();
            }
        }

        public int IntervalSeconds
        {
            get => (int)_timer.Interval.TotalSeconds;
            set => _timer.Interval = TimeSpan.FromSeconds(value);
        }

        public AutoSaveService()
        {
            _autoSavePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "MaoJi",
                "AutoSave");
            
            Directory.CreateDirectory(_autoSavePath);

            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(30)
            };
            _timer.Tick += OnAutoSaveTick;
        }

        /// <summary>
        /// 初始化自动保存服务
        /// </summary>
        public void Initialize(Func<IEnumerable<NoteTab>> getTabsFunc)
        {
            _getTabsFunc = getTabsFunc;
            
            var settings = SettingsService.Instance.CurrentSettings;
            IntervalSeconds = settings.AutoSaveIntervalSeconds;
            IsEnabled = settings.IsAutoSaveEnabled;
        }

        private async void OnAutoSaveTick(object? sender, EventArgs e)
        {
            if (_getTabsFunc == null) return;

            var tabs = _getTabsFunc();
            foreach (var tab in tabs)
            {
                if (tab.IsModified)
                {
                    await AutoSaveTabAsync(tab);
                }
            }
        }

        /// <summary>
        /// 自动保存单个标签页 (Async)
        /// </summary>
        private async Task AutoSaveTabAsync(NoteTab tab)
        {
            try
            {
                // 如果文件已有路径，直接保存到原位置
                if (!string.IsNullOrEmpty(tab.FilePath))
                {
                    // Atomic save logic
                    var filePath = tab.FilePath;
                    var tempFilePath = filePath + ".tmp";
                
                    await File.WriteAllTextAsync(tempFilePath, tab.Content, Encoding.UTF8);

                    if (File.Exists(filePath))
                    {
                        File.Move(tempFilePath, filePath, true);
                    }
                    else
                    {
                        File.Move(tempFilePath, filePath);
                    }

                    tab.IsModified = false;
                }
                else
                {
                    // 保存到自动保存目录
                    var fileName = $"autosave_{tab.GetHashCode()}_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
                    var savePath = Path.Combine(_autoSavePath, fileName);
                    await File.WriteAllTextAsync(savePath, tab.Content, Encoding.UTF8);
                }
            }
            catch (Exception)
            {
                // 忽略自动保存错误
            }
        }

        /// <summary>
        /// 清理旧的自动保存文件
        /// </summary>
        public void CleanupOldFiles(int keepDays = 7)
        {
            try
            {
                var cutoffDate = DateTime.Now.AddDays(-keepDays);
                var files = Directory.GetFiles(_autoSavePath, "autosave_*.txt");
                
                foreach (var file in files)
                {
                    var fileInfo = new FileInfo(file);
                    if (fileInfo.LastWriteTime < cutoffDate)
                    {
                        File.Delete(file);
                    }
                }
            }
            catch (Exception)
            {
                // 忽略清理错误
            }
        }

        /// <summary>
        /// 停止自动保存
        /// </summary>
        public void Stop()
        {
            _timer.Stop();
        }
    }
}
