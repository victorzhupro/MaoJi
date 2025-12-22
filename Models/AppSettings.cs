namespace MaoJi.Models
{
    /// <summary>
    /// 应用程序设置
    /// </summary>
    public class AppSettings
    {
        /// <summary>
        /// 窗口透明度 (0.3 - 1.0)
        /// </summary>
        public double WindowOpacity { get; set; } = 1.0;

        /// <summary>
        /// 是否置顶
        /// </summary>
        public bool IsTopmost { get; set; } = false;

        /// <summary>
        /// 是否启用暗色主题
        /// </summary>
        public bool IsDarkTheme { get; set; } = false;

        /// <summary>
        /// 自动保存间隔（秒）
        /// </summary>
        public int AutoSaveIntervalSeconds { get; set; } = 30;

        /// <summary>
        /// 是否启用自动保存
        /// </summary>
        public bool IsAutoSaveEnabled { get; set; } = true;

        /// <summary>
        /// 字体大小
        /// </summary>
        public double FontSize { get; set; } = 14;

        /// <summary>
        /// 字体名称
        /// </summary>
        public string FontFamily { get; set; } = "Microsoft YaHei UI";

        /// <summary>
        /// 窗口宽度
        /// </summary>
        public double WindowWidth { get; set; } = 800;

        /// <summary>
        /// 窗口高度
        /// </summary>
        public double WindowHeight { get; set; } = 600;

        /// <summary>
        /// 窗口左边位置
        /// </summary>
        public double WindowLeft { get; set; } = 100;

        /// <summary>
        /// 窗口顶部位置
        /// </summary>
        public double WindowTop { get; set; } = 100;
    }
}

