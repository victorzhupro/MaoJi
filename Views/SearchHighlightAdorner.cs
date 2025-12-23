using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace MaoJi.Views
{
    public class SearchHighlightAdorner : Adorner
    {
        private readonly TextBox _textBox;
        private string _searchText = string.Empty;
        private bool _isCaseSensitive;
        private bool _isWholeWord;
        private readonly Brush _fallbackHighlightBrush;
        private readonly Pen _highlightPen;

        public SearchHighlightAdorner(TextBox adornedElement) : base(adornedElement)
        {
            _textBox = adornedElement;
            _fallbackHighlightBrush = new SolidColorBrush(Color.FromArgb(128, 255, 213, 79));
            _fallbackHighlightBrush.Freeze();
            _highlightPen = new Pen(Brushes.Transparent, 0);
            _highlightPen.Freeze();
            
            // 确保不阻挡鼠标事件
            IsHitTestVisible = false;
        }

        public void Update(string searchText, bool isCaseSensitive, bool isWholeWord)
        {
            _searchText = searchText;
            _isCaseSensitive = isCaseSensitive;
            _isWholeWord = isWholeWord;
            InvalidateVisual();
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            if (string.IsNullOrEmpty(_searchText) || string.IsNullOrEmpty(_textBox.Text))
                return;

            var resourceBrush = Application.Current.TryFindResource("SearchHighlightBrush") as Brush;
            var highlightBrush = resourceBrush ?? _fallbackHighlightBrush;

            var currentResourceBrush = Application.Current.TryFindResource("CurrentSearchHighlightBrush") as Brush;
            var currentHighlightBrush = currentResourceBrush ?? highlightBrush;

            // 裁剪绘制区域，防止绘制到文本框外部
            drawingContext.PushClip(new RectangleGeometry(new Rect(0, 0, _textBox.ActualWidth, _textBox.ActualHeight)));

            string text = _textBox.Text;
            StringComparison comparison = _isCaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
            int searchIndex = 0;
            int selectionStart = _textBox.SelectionStart;
            int selectionLength = _textBox.SelectionLength;

            // 限制最大匹配数量以防止性能问题
            int matchCount = 0;
            const int MaxMatches = 1000;

            while (matchCount < MaxMatches)
            {
                int index = text.IndexOf(_searchText, searchIndex, comparison);
                if (index < 0) break;
                int matchLength = _searchText.Length;
                if (_isWholeWord)
                {
                    if (!IsWholeWordMatch(text, index, matchLength))
                    {
                        searchIndex = index + matchLength;
                        continue;
                    }
                }
                
                bool isSelected = (index == selectionStart && matchLength == selectionLength);
                Brush brushToUse = isSelected ? currentHighlightBrush : highlightBrush;

                // 逐字符绘制高亮，这样可以正确处理换行
                for (int i = 0; i < matchLength; i++)
                {
                    int charIndex = index + i;
                    if (charIndex < 0 || charIndex >= text.Length) continue;
                    Rect rectLeading = _textBox.GetRectFromCharacterIndex(charIndex, false);
                    Rect rectTrailing = _textBox.GetRectFromCharacterIndex(charIndex, true);
                    Rect rect = Rect.Union(rectLeading, rectTrailing);
                    
                    if (rect != Rect.Empty)
                    {
                        // 稍微调整一下矩形大小，使其看起来更连贯
                        // rect.Inflate(0.5, 0.5); 
                        drawingContext.DrawRectangle(brushToUse, _highlightPen, rect);
                    }
                }

                searchIndex = index + matchLength;
                matchCount++;
            }

            drawingContext.Pop();
        }

        private static bool IsWordChar(char c)
        {
            return char.IsLetterOrDigit(c) || c == '_';
        }

        private static bool IsWholeWordMatch(string text, int index, int length)
        {
            int left = index - 1;
            int right = index + length;
            bool leftOk = left < 0 || !IsWordChar(text[left]);
            bool rightOk = right >= text.Length || !IsWordChar(text[right]);
            return leftOk && rightOk;
        }
    }
}
