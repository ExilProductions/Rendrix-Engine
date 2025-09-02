namespace RendrixEngine.Rendering
{
    public partial class ConsoleWindow : Window
    {
        public ConsoleWindow()
        {
            InitializeComponent();
            Surface.SetResolution(80, 25);
        }

        public void SetResolution(int cols, int rows) =>
            Surface.SetResolution(cols, rows);

        public void ResizeToResolution(double cellWidth = 12, double cellHeight = 18)
        {
            Width = (int)(Surface.ResolutionColumns * cellWidth);
            Height = (int)(Surface.ResolutionRows * cellHeight);
        }

        public void Clear(char c = ' ') => Surface.Clear(c);
        public void PutChar(int x, int y, char c) => Surface.PutChar(x, y, c);
        public void PutString(int x, int y, string text) => Surface.PutString(x, y, text);
        public void EndFrame() => Surface.EndFrame();
    }
}
