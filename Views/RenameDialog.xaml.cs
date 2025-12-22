using System.Windows;
using System.Windows.Input;

namespace MaoJi.Views
{
    public partial class RenameDialog : Window
    {
        public string? NewFileName { get; private set; }

        public RenameDialog(string currentName)
        {
            InitializeComponent();
            NewFileName = currentName;
            FileNameTextBox.Text = currentName;
            FileNameTextBox.SelectAll();
            FileNameTextBox.Focus();
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            NewFileName = FileNameTextBox.Text?.Trim();
            if (string.IsNullOrEmpty(NewFileName))
            {
                MessageBox.Show(
                    this,
                    "文件名不能为空",
                    "提示",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            // 检查文件名是否包含非法字符
            var invalidChars = System.IO.Path.GetInvalidFileNameChars();
            if (NewFileName.IndexOfAny(invalidChars) >= 0)
            {
                MessageBox.Show(
                    this,
                    "文件名包含非法字符",
                    "错误",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return;
            }

            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void FileNameTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                OKButton_Click(sender, e);
            }
            else if (e.Key == Key.Escape)
            {
                CancelButton_Click(sender, e);
            }
        }
    }
}

