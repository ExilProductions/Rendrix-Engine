using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using System.Globalization;

namespace RendrixEngine.Rendering
{
    internal class ConsoleSurface : Control
    {
        private char[,] _buffer = new char[1, 1];
        private int _columns = 1;
        private int _rows = 1;
        private Typeface _typeface = new Typeface("Consolas");
        private double _fontSize = 16;

        public int ResolutionColumns => _columns;
        public int ResolutionRows => _rows;

        public void SetResolution(int columns, int rows)
        {
            _columns = columns;
            _rows = rows;
            _buffer = new char[rows, columns];

            for (int y = 0; y < rows; y++)
                for (int x = 0; x < columns; x++)
                    _buffer[y, x] = ' ';

            InvalidateVisual();
        }

        public void Clear(char c = ' ')
        {
            for (int y = 0; y < _rows; y++)
                for (int x = 0; x < _columns; x++)
                    _buffer[y, x] = c;

            InvalidateVisual();
        }

        public void PutChar(int x, int y, char c)
        {
            if (x < 0 || x >= _columns || y < 0 || y >= _rows) return;
            _buffer[y, x] = c;
        }

        public void PutString(int x, int y, string text)
        {
            if (y < 0 || y >= _rows) return;

            for (int i = 0; i < text.Length; i++)
            {
                int px = x + i;
                if (px >= 0 && px < _columns)
                    _buffer[y, px] = text[i];
            }
        }

        public void EndFrame()
        {
            InvalidateVisual();
        }

        public override void Render(DrawingContext context)
        {
            base.Render(context);

            if (_columns <= 0 || _rows <= 0) return;
            if (Bounds.Width <= 0 || Bounds.Height <= 0) return;

            double cellWidth = Bounds.Width / _columns;
            double cellHeight = Bounds.Height / _rows;
            _fontSize = System.Math.Min(cellWidth, cellHeight);

            var textBrush = Brushes.LawnGreen;

            context.FillRectangle(Brushes.Black, new Rect(Bounds.Size));

            for (int y = 0; y < _rows; y++)
            {
                for (int x = 0; x < _columns; x++)
                {
                    char c = _buffer[y, x];
                    if (c == ' ') continue;

                    var ft = new FormattedText(
                        c.ToString(),
                        CultureInfo.InvariantCulture,
                        FlowDirection.LeftToRight,
                        _typeface,
                        _fontSize,
                        textBrush);

                    context.DrawText(ft, new Point(x * cellWidth, y * cellHeight));
                }
            }
        }
    }
}
