using CommunityToolkit.Mvvm.ComponentModel;

namespace MaoJi.Models
{
    /// <summary>
    /// 标签页模型，表示一个打开的文档
    /// </summary>
    public partial class NoteTab : ObservableObject
    {
        [ObservableProperty]
        private string _title = "未命名";

        [ObservableProperty]
        private string _content = string.Empty;

        [ObservableProperty]
        private string? _filePath;

        [ObservableProperty]
        private bool _isModified;

        [ObservableProperty]
        private bool _isSelected;

        [ObservableProperty]
        private int _caretIndex;

        [ObservableProperty]
        private int _lineNumber = 1;

        [ObservableProperty]
        private int _columnNumber = 1;

        /// <summary>
        /// 获取显示的标题（带修改标记）
        /// </summary>
        public string DisplayTitle => IsModified ? $"{Title} *" : Title;

        /// <summary>
        /// 是否为新文件（未保存过）
        /// </summary>
        public bool IsNewFile => string.IsNullOrEmpty(FilePath);

        partial void OnTitleChanged(string value)
        {
            OnPropertyChanged(nameof(DisplayTitle));
        }

        partial void OnIsModifiedChanged(bool value)
        {
            OnPropertyChanged(nameof(DisplayTitle));
        }

        partial void OnContentChanged(string value)
        {
            if (!_isInitializing)
            {
                IsModified = true;
            }
        }

        private bool _isInitializing;

        /// <summary>
        /// 初始化内容（不触发修改标记）
        /// </summary>
        public void InitializeContent(string content)
        {
            _isInitializing = true;
            Content = content;
            IsModified = false;
            _isInitializing = false;
        }
    }
}

