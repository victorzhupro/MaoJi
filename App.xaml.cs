using System.Windows;
using MaoJi.Services;

namespace MaoJi
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            
            // 加载用户设置并应用主题
            var settings = SettingsService.Instance.LoadSettings();
            ThemeService.Instance.ApplyTheme(settings.IsDarkTheme);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            // 保存设置
            SettingsService.Instance.SaveSettings();
            base.OnExit(e);
        }
    }
}

