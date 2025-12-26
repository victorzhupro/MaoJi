using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.IO;
using MaoJi.Models;
using MaoJi.Services;
using MaoJi.ViewModels;
using MaoJi.Views;

namespace MaoJi
{
    public partial class MainWindow : Window
    {
        private MainViewModel ViewModel => (MainViewModel)DataContext;
        private DispatcherTimer? _topmostTimer;
        private WindowInteropHelper? _windowHelper;
        private SearchHighlightAdorner? _searchAdorner;

        public MainWindow()
        {
            InitializeComponent();
            
            // 设置窗口图标（用于任务栏显示）
            SetWindowIcon();
            
            // 加载窗口位置和大小
            var settings = SettingsService.Instance.CurrentSettings;
            if (settings.WindowWidth > 0)
            {
                Width = settings.WindowWidth;
                Height = settings.WindowHeight;
                Left = settings.WindowLeft;
                Top = settings.WindowTop;
            }

            // 监听 ViewModel 的 IsTopmost 变化，使用 Win32 API 强制置顶
            Loaded += MainWindow_Loaded;
            Activated += MainWindow_Activated;
            Deactivated += MainWindow_Deactivated;
            SourceInitialized += MainWindow_SourceInitialized;
        }

        /// <summary>
        /// 设置窗口图标（用于任务栏显示）
        /// </summary>
        private void SetWindowIcon()
        {
            try
            {
                // 尝试从资源加载图标
                var iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "cat_icon.ico");
                if (File.Exists(iconPath))
                {
                    var iconUri = new Uri(iconPath, UriKind.Absolute);
                    Icon = new BitmapImage(iconUri);
                    return;
                }
            }
            catch
            {
                // 如果资源文件不存在，创建动态图标
            }

            // 动态创建图标（使用纯 WPF 方法）
            try
            {
                // 创建一个 32x32 的图标
                var drawingVisual = new DrawingVisual();
                using (var drawingContext = drawingVisual.RenderOpen())
                {
                    // 设置背景（透明）
                    drawingContext.DrawRectangle(Brushes.Transparent, null, new Rect(0, 0, 32, 32));
                    
                    // 绘制猫咪图标
                    var geometry = Geometry.Parse("M12,8L10.67,8.09C9.81,7.07 7.4,4.5 5,4.5C5,4.5 3.03,7.46 4.96,11.41C4.41,12.24 4.07,12.67 4,13.66L2.07,13.95L2.28,14.93L4.04,14.67L4.18,15.38L2.61,16.32L3.08,17.21L4.53,16.32C5.68,18.76 8.59,20 12,20C15.41,20 18.32,18.76 19.47,16.32L20.92,17.21L21.39,16.32L19.82,15.38L19.96,14.67L21.72,14.93L21.93,13.95L20,13.66C19.93,12.67 19.59,12.24 19.04,11.41C20.97,7.46 19,4.5 19,4.5C16.6,4.5 14.19,7.07 13.33,8.09L12,8M9,11A1,1 0 0,1 10,12A1,1 0 0,1 9,13A1,1 0 0,1 8,12A1,1 0 0,1 9,11M15,11A1,1 0 0,1 16,12A1,1 0 0,1 15,13A1,1 0 0,1 14,12A1,1 0 0,1 15,11M11,14H13V15H11V14M12,17.5C10.06,17.5 8.5,17.33 8.5,17H15.5C15.5,17.33 13.94,17.5 12,17.5Z");
                    var transform = new ScaleTransform(32.0 / 24.0, 32.0 / 24.0);
                    var transformedGeometry = new GeometryGroup();
                    transformedGeometry.Children.Add(geometry);
                    transformedGeometry.Transform = transform;
                    
                    // 使用蓝色填充
                    drawingContext.DrawGeometry(new SolidColorBrush(Color.FromRgb(66, 133, 244)), null, transformedGeometry);
                }

                var rtb = new RenderTargetBitmap(32, 32, 96, 96, PixelFormats.Pbgra32);
                rtb.Render(drawingVisual);
                Icon = rtb;
            }
            catch
            {
                // 如果创建失败，使用默认图标（不设置）
            }
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            _windowHelper = new WindowInteropHelper(this);
            
            // 初始化搜索高亮
            InitializeSearchHighlights();
            
            if (DataContext is MainViewModel viewModel)
            {
                // 初始化置顶状态
                UpdateTopmostState(viewModel.IsTopmost);
                
                // 监听属性变化
                viewModel.PropertyChanged += ViewModel_PropertyChanged;
                viewModel.SelectionRequested += ViewModel_SelectionRequested;
                
                // 如果置顶，启动定时器定期检查
                if (viewModel.IsTopmost)
                {
                    StartTopmostTimer();
                }
            }
        }

        private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (sender is MainViewModel viewModel)
            {
                if (e.PropertyName == nameof(MainViewModel.IsTopmost))
                {
                    // 先停止定时器（避免冲突）
                    StopTopmostTimer();
                    
                    // 立即更新置顶状态
                    UpdateTopmostState(viewModel.IsTopmost);
                    
                    // 根据置顶状态启动或停止定时器
                    if (viewModel.IsTopmost)
                    {
                        // 立即启动定时器
                        StartTopmostTimer();
                    }
                }
                else if (e.PropertyName == nameof(MainViewModel.FindText) ||
                         e.PropertyName == nameof(MainViewModel.IsCaseSensitive) ||
                         e.PropertyName == nameof(MainViewModel.IsWholeWord) ||
                         e.PropertyName == nameof(MainViewModel.IsFindReplaceVisible))
                {
                    UpdateSearchHighlights();
                }
            }
        }

        private void MainWindow_SourceInitialized(object? sender, EventArgs e)
        {
            // 窗口句柄创建后，初始化置顶状态
            if (_windowHelper == null)
            {
                _windowHelper = new WindowInteropHelper(this);
            }
            
            if (DataContext is MainViewModel viewModel)
            {
                // 延迟一点确保句柄完全准备好
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    UpdateTopmostState(viewModel.IsTopmost);
                }), System.Windows.Threading.DispatcherPriority.Loaded);
            }
        }

        private void MainWindow_Activated(object? sender, EventArgs e)
        {
            // 窗口激活时，如果设置了置顶，确保窗口保持在最顶层
            if (DataContext is MainViewModel viewModel && viewModel.IsTopmost)
            {
                // 延迟一点确保窗口完全激活
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    UpdateTopmostState(true);
                }), System.Windows.Threading.DispatcherPriority.Loaded);
            }
        }

        private void MainWindow_Deactivated(object? sender, EventArgs e)
        {
            // 窗口失去焦点时，如果设置了置顶，确保窗口保持在最顶层
            if (DataContext is MainViewModel viewModel && viewModel.IsTopmost)
            {
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    UpdateTopmostState(true);
                }), System.Windows.Threading.DispatcherPriority.Background);
            }
        }

        /// <summary>
        /// 更新窗口置顶状态（使用 Win32 API 确保真正置顶）
        /// </summary>
        private void UpdateTopmostState(bool isTopmost)
        {
            // 确保在 UI 线程上执行
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.BeginInvoke(new Action(() => UpdateTopmostState(isTopmost)), 
                    System.Windows.Threading.DispatcherPriority.Normal);
                return;
            }

            // 先设置 WPF 的 Topmost 属性
            Topmost = isTopmost;
            
            // 获取窗口句柄
            if (_windowHelper == null)
            {
                _windowHelper = new WindowInteropHelper(this);
            }
            var hwnd = _windowHelper.Handle;
            
            // 如果句柄已创建，立即设置
            if (hwnd != IntPtr.Zero)
            {
                ForceSetTopmost(hwnd, isTopmost);
                
                // 如果是置顶，延迟再次确认（确保生效）
                if (isTopmost)
                {
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        if (hwnd != IntPtr.Zero && DataContext is MainViewModel vm && vm.IsTopmost)
                        {
                            ForceSetTopmost(hwnd, true);
                        }
                    }), System.Windows.Threading.DispatcherPriority.Background);
                }
            }
            // 如果句柄未创建，会在 SourceInitialized 事件中处理
        }

        /// <summary>
        /// 启动置顶定时器（定期检查并强制置顶）
        /// </summary>
        private void StartTopmostTimer()
        {
            StopTopmostTimer();
            
            _topmostTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(100) // 每100ms检查一次（更频繁）
            };
            _topmostTimer.Tick += (s, e) =>
            {
                // 如果窗口被禁用（例如有模态对话框打开）或有模态循环运行，暂停强制置顶
                // 这避免了与系统对话框（如保存文件）或子窗口的 Z 序冲突
                if (!IsEnabled || ComponentDispatcher.IsThreadModal) return;

                if (DataContext is MainViewModel viewModel && viewModel.IsTopmost)
                {
                    var hwnd = _windowHelper?.Handle ?? IntPtr.Zero;
                    if (hwnd != IntPtr.Zero)
                    {
                        // 强制置顶（使用完整的标志）
                        var flags = SWP_NOMOVE | SWP_NOSIZE | SWP_SHOWWINDOW | SWP_FRAMECHANGED;
                        SetWindowPos(hwnd, HWND_TOPMOST, 0, 0, 0, 0, flags);
                        
                        // 同时确保扩展样式
                        var exStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
                        if ((exStyle & WS_EX_TOPMOST) == 0)
                        {
                            SetWindowLong(hwnd, GWL_EXSTYLE, exStyle | WS_EX_TOPMOST);
                            SetWindowPos(hwnd, HWND_TOPMOST, 0, 0, 0, 0, flags);
                        }
                    }
                }
            };
            _topmostTimer.Start();
        }

        /// <summary>
        /// 停止置顶定时器
        /// </summary>
        private void StopTopmostTimer()
        {
            if (_topmostTimer != null)
            {
                _topmostTimer.Stop();
                _topmostTimer = null;
            }
        }

        /// <summary>
        /// 强制设置窗口置顶（使用 Win32 API）
        /// </summary>
        private void ForceSetTopmost(IntPtr hwnd, bool isTopmost)
        {
            if (hwnd == IntPtr.Zero)
            {
                // 窗口句柄未准备好，延迟执行
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    var h = _windowHelper?.Handle ?? IntPtr.Zero;
                    if (h != IntPtr.Zero)
                    {
                        ForceSetTopmost(h, isTopmost);
                    }
                }), System.Windows.Threading.DispatcherPriority.Loaded);
                return;
            }

            try
            {
                var flags = SWP_NOMOVE | SWP_NOSIZE | SWP_SHOWWINDOW | SWP_FRAMECHANGED;
                
                if (isTopmost)
                {
                    // 置顶：先取消置顶，再设置置顶（确保生效）
                    SetWindowPos(hwnd, HWND_NOTOPMOST, 0, 0, 0, 0, flags);
                    
                    // 立即设置置顶
                    var result1 = SetWindowPos(hwnd, HWND_TOPMOST, 0, 0, 0, 0, flags);
                    
                    // 同时使用扩展样式（双重保险）
                    var exStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
                    SetWindowLong(hwnd, GWL_EXSTYLE, exStyle | WS_EX_TOPMOST);
                    
                    // 再次调用 SetWindowPos 确保生效
                    var result2 = SetWindowPos(hwnd, HWND_TOPMOST, 0, 0, 0, 0, flags);
                    
                    // 如果失败，尝试使用不同的标志
                    if (!result1 && !result2)
                    {
                        var flags2 = SWP_NOMOVE | SWP_NOSIZE | SWP_SHOWWINDOW;
                        SetWindowPos(hwnd, HWND_TOPMOST, 0, 0, 0, 0, flags2);
                    }
                }
                else
                {
                    // 取消置顶：先移除扩展样式
                    var exStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
                    if ((exStyle & WS_EX_TOPMOST) != 0)
                    {
                        SetWindowLong(hwnd, GWL_EXSTYLE, exStyle & ~WS_EX_TOPMOST);
                    }
                    
                    // 多次调用 SetWindowPos 确保取消置顶
                    SetWindowPos(hwnd, HWND_NOTOPMOST, 0, 0, 0, 0, flags);
                    SetWindowPos(hwnd, HWND_NOTOPMOST, 0, 0, 0, 0, flags);
                }
            }
            catch (Exception)
            {
                // 忽略错误，继续执行
            }
        }

        #region 窗口拖动和调整大小

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                MaximizeButton_Click(sender, e);
            }
            else
            {
                DragMove();
            }
        }

        private void ResizeGrip_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                var hwnd = new WindowInteropHelper(this).Handle;
                SendMessage(hwnd, 0x112, (IntPtr)0xF008, IntPtr.Zero);
            }
        }

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll", EntryPoint = "GetWindowLong", CharSet = CharSet.Auto)]
        private static extern int GetWindowLong32(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", EntryPoint = "GetWindowLongPtr", CharSet = CharSet.Auto)]
        private static extern IntPtr GetWindowLongPtr64(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", EntryPoint = "SetWindowLong", CharSet = CharSet.Auto)]
        private static extern int SetWindowLong32(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr", CharSet = CharSet.Auto)]
        private static extern IntPtr SetWindowLongPtr64(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        private static int GetWindowLong(IntPtr hWnd, int nIndex)
        {
            if (IntPtr.Size == 4)
                return GetWindowLong32(hWnd, nIndex);
            else
                return (int)GetWindowLongPtr64(hWnd, nIndex);
        }

        private static int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong)
        {
            if (IntPtr.Size == 4)
                return SetWindowLong32(hWnd, nIndex, dwNewLong);
            else
                return (int)SetWindowLongPtr64(hWnd, nIndex, new IntPtr(dwNewLong));
        }

        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_TOPMOST = 0x00000008;

        private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        private static readonly IntPtr HWND_NOTOPMOST = new IntPtr(-2);
        private const uint SWP_NOMOVE = 0x0002;
        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_SHOWWINDOW = 0x0040;
        private const uint SWP_NOACTIVATE = 0x0010;
        private const uint SWP_FRAMECHANGED = 0x0020;

        #endregion

        #region 窗口控制按钮

        private void TopmostButton_Click(object sender, RoutedEventArgs e)
        {
            // 直接在这里处理置顶切换，确保立即生效
            if (DataContext is MainViewModel viewModel)
            {
                var newValue = !viewModel.IsTopmost;
                viewModel.IsTopmost = newValue;
                
                // 确保窗口句柄已准备好
                if (_windowHelper == null)
                {
                    _windowHelper = new WindowInteropHelper(this);
                }
                
                // 立即更新窗口状态
                UpdateTopmostState(newValue);
                
                // 如果窗口句柄已准备好，立即强制设置
                var hwnd = _windowHelper.Handle;
                if (hwnd != IntPtr.Zero)
                {
                    ForceSetTopmost(hwnd, newValue);
                }
                
                // 根据状态启动或停止定时器
                if (newValue)
                {
                    StartTopmostTimer();
                }
                else
                {
                    StopTopmostTimer();
                }
                
                // 保存设置
                SettingsService.Instance.UpdateSettings(s => s.IsTopmost = newValue);
                viewModel.StatusText = newValue ? "已置顶" : "取消置顶";
            }
        }

        private void ThemeButton_Click(object sender, RoutedEventArgs e)
        {
            // 直接在这里处理主题切换，确保立即生效
            if (DataContext is MainViewModel viewModel)
            {
                var newValue = !viewModel.IsDarkTheme;
                viewModel.IsDarkTheme = newValue;
                
                // 立即应用主题
                ThemeService.Instance.ApplyTheme(newValue);
                
                // 保存设置
                SettingsService.Instance.UpdateSettings(s => s.IsDarkTheme = newValue);
                viewModel.StatusText = newValue ? "已切换到明亮主题" : "已切换到暗黑主题";
            }
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void MaximizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState == WindowState.Maximized 
                ? WindowState.Normal 
                : WindowState.Maximized;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        #endregion

        #region 标签页交互

        private void Tab_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is NoteTab tab)
            {
                // 检测双击
                if (e.ClickCount == 2)
                {
                    // 双击时触发重命名
                    ViewModel.RenameFileCommand.Execute(tab);
                    e.Handled = true;
                    return;
                }

                // 单击时选择标签
                // 取消选择其他标签
                foreach (var t in ViewModel.Tabs)
                {
                    t.IsSelected = false;
                }
                
                tab.IsSelected = true;
                ViewModel.SelectedTab = tab;
            }
        }

        private void ScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            var scrollViewer = sender as ScrollViewer;
            if (scrollViewer != null)
            {
                // 简单的水平滚动实现
                if (e.Delta > 0)
                {
                    scrollViewer.ScrollToHorizontalOffset(scrollViewer.HorizontalOffset - 40);
                }
                else
                {
                    scrollViewer.ScrollToHorizontalOffset(scrollViewer.HorizontalOffset + 40);
                }
                e.Handled = true;
            }
        }

        #endregion

        private void FindPanel_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if ((bool)e.NewValue)
            {
                // 聚焦到搜索框
                FindTextBox?.Focus();
                FindTextBox?.SelectAll();
            }
            else
            {
                // 聚焦回编辑器
                EditorTextBox?.Focus();
            }
        }

        #region 编辑器交互

        private void InitializeSearchHighlights()
        {
            if (EditorTextBox != null)
            {
                var layer = AdornerLayer.GetAdornerLayer(EditorTextBox);
                if (layer != null)
                {
                    _searchAdorner = new SearchHighlightAdorner(EditorTextBox);
                    layer.Add(_searchAdorner);
                }

                // 监听滚动事件以更新高亮位置
                EditorTextBox.AddHandler(ScrollViewer.ScrollChangedEvent, new ScrollChangedEventHandler(EditorTextBox_ScrollChanged));
                
                // 监听文本变化
                EditorTextBox.TextChanged += (s, e) => UpdateSearchHighlights();
            }
        }

        private void EditorTextBox_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            _searchAdorner?.InvalidateVisual();
        }

        private void UpdateSearchHighlights()
        {
            if (_searchAdorner == null || ViewModel == null) return;

            // 有查找文本时显示高亮（不再强制要求面板可见）
            if (!string.IsNullOrEmpty(ViewModel.FindText))
            {
                _searchAdorner.Visibility = Visibility.Visible;
                _searchAdorner.Update(ViewModel.FindText, ViewModel.IsCaseSensitive, ViewModel.IsWholeWord);
            }
            else
            {
                _searchAdorner.Visibility = Visibility.Collapsed;
            }
        }

        private void ViewModel_SelectionRequested(object? sender, (int Start, int Length) e)
        {
            if (EditorTextBox != null)
            {
                var textLen = EditorTextBox.Text?.Length ?? 0;
                var safeStart = Math.Max(0, Math.Min(e.Start, textLen));
                var safeLen = Math.Max(0, Math.Min(e.Length, Math.Max(0, textLen - safeStart)));
                EditorTextBox.Select(safeStart, safeLen);
                
                // 确保可见
                var lineIndex = EditorTextBox.GetLineIndexFromCharacterIndex(safeStart);
                EditorTextBox.ScrollToLine(lineIndex);
            }
        }

        private void EditorTextBox_SelectionChanged(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                var caretIndex = textBox.CaretIndex;
                var lineIndex = textBox.GetLineIndexFromCharacterIndex(caretIndex);
                var lineStartIndex = textBox.GetCharacterIndexFromLineIndex(lineIndex);
                var column = caretIndex - lineStartIndex + 1;
                
                ViewModel.UpdateCaretPosition(lineIndex + 1, column);
                
                if (ViewModel.SelectedTab != null)
                {
                    ViewModel.SelectedTab.CaretIndex = caretIndex;
                }
                
                // 更新搜索高亮（当前选中项变色）
                _searchAdorner?.InvalidateVisual();
            }
        }

        #endregion

        #region 窗口事件

        private bool _isClosingConfirmed = false;

        private async void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (_isClosingConfirmed) return;

            // 确保如果有延迟的绑定更新，立即提交
            EditorTextBox?.GetBindingExpression(TextBox.TextProperty)?.UpdateSource();

            // 1. 如果没有未保存的更改，直接关闭，不要设置 e.Cancel = true
            if (!ViewModel.HasUnsavedChanges)
            {
                // 需要取消关闭操作以等待异步保存完成，然后手动关闭
                e.Cancel = true;
                
                // 执行清理工作
                StopTopmostTimer();
                
                await SettingsService.Instance.UpdateSettingsAsync(s =>
                {
                    s.WindowWidth = Width;
                    s.WindowHeight = Height;
                    s.WindowLeft = Left;
                    s.WindowTop = Top;
                });
                
                ViewModel.Cleanup();
                
                _isClosingConfirmed = true;
                Close();
                return;
            }

            // 2. 如果有未保存的更改，取消关闭并进行异步检查
            e.Cancel = true;

            // 停止定时器
            StopTopmostTimer();
            
            // 保存窗口位置和大小
            await SettingsService.Instance.UpdateSettingsAsync(s =>
            {
                s.WindowWidth = Width;
                s.WindowHeight = Height;
                s.WindowLeft = Left;
                s.WindowTop = Top;
            });

            // 检查是否可以关闭（异步）
            if (await ViewModel.CanCloseAsync())
            {
                _isClosingConfirmed = true;
                Close();
            }
        }

        #endregion
    }
}
