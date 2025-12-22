using System.Collections.ObjectModel;
using System.Windows.Input;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MaoJi.Models;
using MaoJi.Services;

namespace MaoJi.ViewModels
{
    /// <summary>
    /// 主窗口视图模型
    /// </summary>
    public partial class MainViewModel : ObservableObject
    {
        private readonly AutoSaveService _autoSaveService;
        private int _newTabCounter = 1;

        #region 属性

        [ObservableProperty]
        private ObservableCollection<NoteTab> _tabs = new();

        [ObservableProperty]
        private NoteTab? _selectedTab;

        [ObservableProperty]
        private double _windowOpacity = 1.0;

        [ObservableProperty]
        private bool _isTopmost;

        [ObservableProperty]
        private bool _isDarkTheme;

        [ObservableProperty]
        private bool _isAutoSaveEnabled = true;

        [ObservableProperty]
        private string _statusText = "就绪";

        [ObservableProperty]
        private string _encodingText = "UTF-8";

        [ObservableProperty]
        private string _positionText = "行: 1  列: 1";

        [ObservableProperty]
        private bool _isFindReplaceVisible;

        [ObservableProperty]
        private string _findText = string.Empty;

        [ObservableProperty]
        private string _replaceText = string.Empty;

        [ObservableProperty]
        private bool _isCaseSensitive;

        [ObservableProperty]
        private bool _isWholeWord;

        #endregion

        public MainViewModel()
        {
            _autoSaveService = new AutoSaveService();
            
            // 从设置加载初始值
            var settings = SettingsService.Instance.CurrentSettings;
            WindowOpacity = settings.WindowOpacity;
            IsTopmost = settings.IsTopmost;
            IsDarkTheme = settings.IsDarkTheme;
            IsAutoSaveEnabled = settings.IsAutoSaveEnabled;

            // 初始化自动保存
            _autoSaveService.Initialize(() => Tabs);

            // 创建第一个标签页
            CreateNewTab();
        }

        #region 标签页管理

        /// <summary>
        /// 创建新标签页
        /// </summary>
        [RelayCommand]
        private void NewTab()
        {
            CreateNewTab();
        }

        private void CreateNewTab()
        {
            var tab = new NoteTab
            {
                Title = $"未命名{_newTabCounter++}"
            };
            Tabs.Add(tab);
            SelectedTab = tab;
        }

        private bool _wasTopmostBeforeDialog;

        /// <summary>
        /// 暂时禁用置顶（用于显示对话框前）
        /// </summary>
        private void SuspendTopmost()
        {
            _wasTopmostBeforeDialog = IsTopmost;
            if (IsTopmost)
            {
                IsTopmost = false;
            }
        }

        /// <summary>
        /// 恢复置顶（用于对话框关闭后）
        /// </summary>
        private void ResumeTopmost()
        {
            if (_wasTopmostBeforeDialog)
            {
                IsTopmost = true;
            }
        }

        /// <summary>
        /// 关闭标签页
        /// </summary>
        [RelayCommand]
        private async Task CloseTab(NoteTab? tab)
        {
            if (tab == null) return;

            // 检查是否需要保存
            var result = await FileService.Instance.PromptSaveIfModifiedAsync(
                tab, 
                beforeDialogShow: SuspendTopmost,
                afterDialogShow: ResumeTopmost);
            
            if (result == null) return; // 用户取消

            Tabs.Remove(tab);

            // 如果没有标签页了，创建一个新的
            if (Tabs.Count == 0)
            {
                CreateNewTab();
            }
            else if (SelectedTab == tab)
            {
                SelectedTab = Tabs[^1];
            }
        }

        #endregion

        #region 文件操作

        /// <summary>
        /// 打开文件
        /// </summary>
        [RelayCommand]
        private void OpenFile()
        {
            var tab = FileService.Instance.OpenFile(beforeDialogShow: SuspendTopmost, afterDialogShow: ResumeTopmost);
            if (tab != null)
            {
                Tabs.Add(tab);
                SelectedTab = tab;
                StatusText = $"已打开: {tab.FilePath}";
            }
        }

        /// <summary>
        /// 保存文件
        /// </summary>
        [RelayCommand]
        private async Task SaveFile()
        {
            if (SelectedTab == null) return;

            if (await FileService.Instance.SaveFileAsync(SelectedTab, beforeDialogShow: SuspendTopmost, afterDialogShow: ResumeTopmost))
            {
                StatusText = $"已保存: {SelectedTab.FilePath}";
            }
        }

        /// <summary>
        /// 另存为
        /// </summary>
        [RelayCommand]
        private async Task SaveFileAs()
        {
            if (SelectedTab == null) return;

            if (await FileService.Instance.SaveFileAsAsync(SelectedTab, beforeDialogShow: SuspendTopmost, afterDialogShow: ResumeTopmost))
            {
                StatusText = $"已保存: {SelectedTab.FilePath}";
            }
        }

        /// <summary>
        /// 保存所有文件
        /// </summary>
        [RelayCommand]
        private async Task SaveAllFiles()
        {
            foreach (var tab in Tabs)
            {
                if (tab.IsModified)
                {
                    await FileService.Instance.SaveFileAsync(tab, beforeDialogShow: SuspendTopmost, afterDialogShow: ResumeTopmost);
                }
            }
            StatusText = "已保存所有文件";
        }

        /// <summary>
        /// 重命名文件
        /// </summary>
        [RelayCommand]
        public async Task RenameFile(NoteTab? tab)
        {
            if (tab == null)
            {
                tab = SelectedTab;
            }

            if (tab == null) return;

            // 只有已保存的文件才能重命名
            if (string.IsNullOrEmpty(tab.FilePath))
            {
                StatusText = "未保存的文件无法重命名，请先保存";
                return;
            }

            try
            {
                var currentFileName = Path.GetFileName(tab.FilePath);
                var dialog = new Views.RenameDialog(currentFileName)
                {
                    Owner = System.Windows.Application.Current.MainWindow
                };

                SuspendTopmost();
                var result = dialog.ShowDialog();
                ResumeTopmost();

                if (result == true && !string.IsNullOrEmpty(dialog.NewFileName))
                {
                    var newFileName = dialog.NewFileName;
                    var directory = Path.GetDirectoryName(tab.FilePath);
                    if (string.IsNullOrEmpty(directory))
                    {
                        StatusText = "无法获取文件目录";
                        return;
                    }

                    var newFilePath = Path.Combine(directory, newFileName);

                    // 检查新文件名是否与当前文件名相同
                    if (newFilePath.Equals(tab.FilePath, StringComparison.OrdinalIgnoreCase))
                    {
                        StatusText = "文件名未更改";
                        return;
                    }

                    // 检查新文件是否已存在
                    if (File.Exists(newFilePath))
                    {
                        System.Windows.MessageBox.Show(
                            System.Windows.Application.Current.MainWindow,
                            "文件已存在，无法重命名",
                            "错误",
                            System.Windows.MessageBoxButton.OK,
                            System.Windows.MessageBoxImage.Warning);
                        return;
                    }

                    // 保存修改状态（重命名不应该改变文件的修改状态）
                    var wasModified = tab.IsModified;

                    // 如果文件已修改，先保存文件再重命名
                    if (wasModified)
                    {
                        // 先保存当前内容到原文件
                        if (!await FileService.Instance.SaveFileAsync(tab, beforeDialogShow: SuspendTopmost, afterDialogShow: ResumeTopmost))
                        {
                            StatusText = "保存文件失败，无法重命名";
                            return;
                        }
                    }

                    // 重命名文件
                    File.Move(tab.FilePath, newFilePath);

                    // 更新标签页信息
                    tab.FilePath = newFilePath;
                    tab.Title = newFileName;
                    // 如果之前已保存，则清除修改标记；否则保持修改状态
                    // 由于上面已经保存了，所以这里应该清除修改标记
                    tab.IsModified = false;

                    StatusText = $"已重命名为: {newFileName}";
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    System.Windows.Application.Current.MainWindow,
                    $"重命名失败：{ex.Message}",
                    "错误",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
                StatusText = "重命名失败";
            }
        }

        #endregion

        #region 窗口控制

        /// <summary>
        /// 切换置顶状态
        /// </summary>
        [RelayCommand]
        private void ToggleTopmost()
        {
            IsTopmost = !IsTopmost;
            SettingsService.Instance.UpdateSettings(s => s.IsTopmost = IsTopmost);
            StatusText = IsTopmost ? "已置顶" : "取消置顶";
        }

        /// <summary>
        /// 切换主题
        /// </summary>
        [RelayCommand]
        private void ToggleTheme()
        {
            IsDarkTheme = !IsDarkTheme;
            ThemeService.Instance.ApplyTheme(IsDarkTheme);
            SettingsService.Instance.UpdateSettings(s => s.IsDarkTheme = IsDarkTheme);
        }

        partial void OnWindowOpacityChanged(double value)
        {
            SettingsService.Instance.UpdateSettings(s => s.WindowOpacity = value);
        }

        #endregion

        #region 查找替换

        /// <summary>
        /// 显示/隐藏查找替换面板
        /// </summary>
        [RelayCommand]
        private void ToggleFindReplace()
        {
            IsFindReplaceVisible = !IsFindReplaceVisible;
        }

        /// <summary>
        /// 查找下一个
        /// </summary>
        [RelayCommand]
        private void FindNext()
        {
            if (SelectedTab == null || string.IsNullOrEmpty(FindText)) return;

            var content = SelectedTab.Content;
            var startIndex = SelectedTab.CaretIndex + 1;
            
            var comparison = IsCaseSensitive 
                ? StringComparison.Ordinal 
                : StringComparison.OrdinalIgnoreCase;

            var index = content.IndexOf(FindText, startIndex, comparison);
            
            // 如果没找到，从头开始搜索
            if (index == -1 && startIndex > 0)
            {
                index = content.IndexOf(FindText, 0, comparison);
            }

            if (index >= 0)
            {
                SelectedTab.CaretIndex = index;
                StatusText = $"找到: 位置 {index + 1}";
            }
            else
            {
                StatusText = "未找到匹配项";
            }
        }

        /// <summary>
        /// 查找上一个
        /// </summary>
        [RelayCommand]
        private void FindPrevious()
        {
            if (SelectedTab == null || string.IsNullOrEmpty(FindText)) return;

            var content = SelectedTab.Content;
            var startIndex = Math.Max(0, SelectedTab.CaretIndex - 1);
            
            var comparison = IsCaseSensitive 
                ? StringComparison.Ordinal 
                : StringComparison.OrdinalIgnoreCase;

            var index = content.LastIndexOf(FindText, startIndex, comparison);
            
            // 如果没找到，从末尾开始搜索
            if (index == -1 && startIndex < content.Length)
            {
                index = content.LastIndexOf(FindText, content.Length - 1, comparison);
            }

            if (index >= 0)
            {
                SelectedTab.CaretIndex = index;
                StatusText = $"找到: 位置 {index + 1}";
            }
            else
            {
                StatusText = "未找到匹配项";
            }
        }

        /// <summary>
        /// 替换
        /// </summary>
        [RelayCommand]
        private void Replace()
        {
            if (SelectedTab == null || string.IsNullOrEmpty(FindText)) return;

            var content = SelectedTab.Content;
            var comparison = IsCaseSensitive 
                ? StringComparison.Ordinal 
                : StringComparison.OrdinalIgnoreCase;

            var index = content.IndexOf(FindText, SelectedTab.CaretIndex, comparison);
            
            if (index >= 0)
            {
                SelectedTab.Content = content.Remove(index, FindText.Length).Insert(index, ReplaceText);
                SelectedTab.CaretIndex = index + ReplaceText.Length;
                StatusText = "已替换";
            }
            else
            {
                StatusText = "未找到匹配项";
            }
        }

        /// <summary>
        /// 全部替换
        /// </summary>
        [RelayCommand]
        private void ReplaceAll()
        {
            if (SelectedTab == null || string.IsNullOrEmpty(FindText)) return;

            var content = SelectedTab.Content;
            var comparison = IsCaseSensitive 
                ? StringComparison.Ordinal 
                : StringComparison.OrdinalIgnoreCase;

            var count = 0;
            var index = 0;
            
            while ((index = content.IndexOf(FindText, index, comparison)) >= 0)
            {
                content = content.Remove(index, FindText.Length).Insert(index, ReplaceText);
                index += ReplaceText.Length;
                count++;
            }

            if (count > 0)
            {
                SelectedTab.Content = content;
                StatusText = $"已替换 {count} 处";
            }
            else
            {
                StatusText = "未找到匹配项";
            }
        }

        #endregion

        #region 编辑操作

        /// <summary>
        /// 更新光标位置
        /// </summary>
        public void UpdateCaretPosition(int caretIndex, string content)
        {
            if (string.IsNullOrEmpty(content))
            {
                PositionText = "行: 1  列: 1";
                return;
            }

            var line = 1;
            var column = 1;
            
            for (int i = 0; i < caretIndex && i < content.Length; i++)
            {
                if (content[i] == '\n')
                {
                    line++;
                    column = 1;
                }
                else
                {
                    column++;
                }
            }

            PositionText = $"行: {line}  列: {column}";
        }

        #endregion

        /// <summary>
        /// 检查是否可以关闭窗口（处理未保存的文件）
        /// </summary>
        /// <returns>如果可以关闭返回 true，否则返回 false</returns>
        public async Task<bool> CanCloseAsync()
        {
            Cleanup();
            
            // 检查所有未保存的文件
            foreach (var tab in Tabs.ToList())
            {
                var result = await FileService.Instance.PromptSaveIfModifiedAsync(
                    tab,
                    beforeDialogShow: SuspendTopmost,
                    afterDialogShow: ResumeTopmost);

                if (result == null)
                {
                    // 用户取消关闭
                    return false; 
                }
            }
            
            return true; // 可以关闭
        }

        /// <summary>
        /// 检查是否有未保存的更改
        /// </summary>
        public bool HasUnsavedChanges => Tabs.Any(t => t.IsModified);

        /// <summary>
        /// 清理资源（如停止自动保存）
        /// </summary>
        public void Cleanup()
        {
            _autoSaveService.Stop();
        }
    }
}
