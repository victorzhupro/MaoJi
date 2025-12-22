using System.IO;
using System.Text;
using Microsoft.Win32;
using MaoJi.Models;

namespace MaoJi.Services
{
    /// <summary>
    /// 文件服务 - 处理文件的打开和保存
    /// </summary>
    public class FileService
    {
        private static readonly Lazy<FileService> _instance = new(() => new FileService());
        public static FileService Instance => _instance.Value;

        private FileService() { }

        /// <summary>
        /// 打开文件对话框
        /// </summary>
        public NoteTab? OpenFile(Action? beforeDialogShow = null, Action? afterDialogShow = null)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "文本文件 (*.txt)|*.txt|所有文件 (*.*)|*.*",
                Title = "打开文件"
            };

            var mainWindow = System.Windows.Application.Current.MainWindow;

            beforeDialogShow?.Invoke();
            var result = dialog.ShowDialog(mainWindow);
            afterDialogShow?.Invoke();

            if (result == true)
            {
                return LoadFile(dialog.FileName);
            }

            return null;
        }

        /// <summary>
        /// 从路径加载文件
        /// </summary>
        public NoteTab? LoadFile(string filePath)
        {
            try
            {
                var content = File.ReadAllText(filePath, Encoding.UTF8);
                var tab = new NoteTab
                {
                    Title = Path.GetFileName(filePath),
                    FilePath = filePath
                };
                tab.InitializeContent(content);
                return tab;
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    System.Windows.Application.Current.MainWindow,
                    $"无法打开文件：{ex.Message}",
                    "错误",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
                return null;
            }
        }

        /// <summary>
        /// 保存文件 (Async)
        /// </summary>
        public async Task<bool> SaveFileAsync(NoteTab tab, Action? beforeDialogShow = null, Action? afterDialogShow = null)
        {
            if (string.IsNullOrEmpty(tab.FilePath))
            {
                return await SaveFileAsAsync(tab, beforeDialogShow, afterDialogShow);
            }

            return await SaveToPathAsync(tab, tab.FilePath);
        }

        /// <summary>
        /// 另存为 (Async)
        /// </summary>
        public async Task<bool> SaveFileAsAsync(NoteTab tab, Action? beforeDialogShow = null, Action? afterDialogShow = null)
        {
            var dialog = new SaveFileDialog
            {
                Filter = "文本文件 (*.txt)|*.txt|所有文件 (*.*)|*.*",
                Title = "保存文件",
                FileName = tab.Title.Replace(" *", "")
            };

            var mainWindow = System.Windows.Application.Current.MainWindow;

            beforeDialogShow?.Invoke();
            var result = dialog.ShowDialog(mainWindow);
            afterDialogShow?.Invoke();

            if (result == true)
            {
                return await SaveToPathAsync(tab, dialog.FileName);
            }

            return false;
        }

        /// <summary>
        /// 保存到指定路径 (Async with Atomic Save)
        /// </summary>
        private async Task<bool> SaveToPathAsync(NoteTab tab, string filePath)
        {
            try
            {
                // Atomic save: Write to temp file first, then move
                var tempFilePath = filePath + ".tmp";
                
                await File.WriteAllTextAsync(tempFilePath, tab.Content, Encoding.UTF8);

                if (File.Exists(filePath))
                {
                    File.Move(tempFilePath, filePath, true); // overwrite
                }
                else
                {
                    File.Move(tempFilePath, filePath);
                }

                tab.FilePath = filePath;
                // 更新标题，移除可能的修改标记
                var fileName = Path.GetFileName(filePath);
                tab.Title = fileName;
                tab.IsModified = false;
                return true;
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    System.Windows.Application.Current.MainWindow,
                    $"无法保存文件：{ex.Message}",
                    "错误",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
                return false;
            }
        }

        /// <summary>
        /// 检查是否需要保存并提示用户 (Async)
        /// </summary>
        public async Task<bool?> PromptSaveIfModifiedAsync(NoteTab tab, Action? beforeDialogShow = null, Action? afterDialogShow = null)
        {
            if (!tab.IsModified)
                return true;

            beforeDialogShow?.Invoke();
            var result = System.Windows.MessageBox.Show(
                System.Windows.Application.Current.MainWindow,
                $"文件 \"{tab.Title}\" 已修改，是否保存？",
                "保存确认",
                System.Windows.MessageBoxButton.YesNoCancel,
                System.Windows.MessageBoxImage.Question);
            afterDialogShow?.Invoke();

            return result switch
            {
                System.Windows.MessageBoxResult.Yes => await SaveFileAsync(tab, beforeDialogShow, afterDialogShow),
                System.Windows.MessageBoxResult.No => true,
                _ => null // Cancel
            };
        }
    }
}
