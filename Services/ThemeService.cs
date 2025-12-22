using System.Windows;
using System.Windows.Threading;

namespace MaoJi.Services
{
    /// <summary>
    /// 主题服务 - 管理应用程序主题切换
    /// </summary>
    public class ThemeService
    {
        private static readonly Lazy<ThemeService> _instance = new(() => new ThemeService());
        public static ThemeService Instance => _instance.Value;

        private bool _isDarkTheme;
        public bool IsDarkTheme => _isDarkTheme;

        public event EventHandler<bool>? ThemeChanged;

        private ThemeService() { }

        /// <summary>
        /// 应用主题
        /// </summary>
        public void ApplyTheme(bool isDark)
        {
            _isDarkTheme = isDark;
            
            var app = Application.Current;
            if (app == null) return;

            // 确保在 UI 线程上执行
            if (!app.Dispatcher.CheckAccess())
            {
                app.Dispatcher.Invoke(() => ApplyTheme(isDark));
                return;
            }

            var themePath = isDark ? "Themes/DarkTheme.xaml" : "Themes/LightTheme.xaml";

            if (app.Resources != null)
            {
                var mergedDictionaries = app.Resources.MergedDictionaries;
                
                // 查找并移除旧的主题字典（保留其他资源如转换器）
                ResourceDictionary? oldTheme = null;
                foreach (var dict in mergedDictionaries)
                {
                    if (dict.Source != null)
                    {
                        var sourceString = dict.Source.ToString();
                        if (sourceString.Contains("LightTheme") || sourceString.Contains("DarkTheme"))
                        {
                            oldTheme = dict;
                            break;
                        }
                    }
                }

                // 移除旧主题
                if (oldTheme != null)
                {
                    mergedDictionaries.Remove(oldTheme);
                }

                // 添加新主题（添加到第一个位置，确保主题资源优先）
                ResourceDictionary? newTheme = null;
                try
                {
                    // 尝试使用相对路径
                    newTheme = new ResourceDictionary
                    {
                        Source = new Uri(themePath, UriKind.Relative)
                    };
                    mergedDictionaries.Insert(0, newTheme);
                }
                catch (Exception)
                {
                    // 如果加载失败，尝试使用 pack URI
                    try
                    {
                        var assemblyName = System.Reflection.Assembly.GetExecutingAssembly().GetName().Name;
                        var packUri = $"pack://application:,,,/{assemblyName};component/{themePath}";
                        newTheme = new ResourceDictionary
                        {
                            Source = new Uri(packUri, UriKind.Absolute)
                        };
                        mergedDictionaries.Insert(0, newTheme);
                    }
                    catch (Exception)
                    {
                        // 如果还是失败，尝试使用绝对路径
                        try
                        {
                            var basePath = AppContext.BaseDirectory;
                            var absolutePath = System.IO.Path.Combine(basePath ?? "", themePath);
                            if (System.IO.File.Exists(absolutePath))
                            {
                                newTheme = new ResourceDictionary
                                {
                                    Source = new Uri(absolutePath, UriKind.Absolute)
                                };
                                mergedDictionaries.Insert(0, newTheme);
                            }
                        }
                        catch (Exception)
                        {
                            // 忽略错误
                        }
                    }
                }
            }

            ThemeChanged?.Invoke(this, isDark);
        }

        /// <summary>
        /// 切换主题
        /// </summary>
        public void ToggleTheme()
        {
            ApplyTheme(!_isDarkTheme);
            SettingsService.Instance.UpdateSettings(s => s.IsDarkTheme = _isDarkTheme);
        }
    }
}

