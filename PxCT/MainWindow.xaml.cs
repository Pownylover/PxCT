namespace PxCT
{
    using System.Windows;
    using System.Windows.Input;
    using System.Windows.Media;

    /// <summary>Interaction logic for MainWindow.xaml</summary>
    public partial class MainWindow : Window
    {
        #region Fields

        private Point _translateStart;

        #endregion

        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainViewModel();
        }

        #region Methods

        private void ComparisonView_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _translateStart = e.GetPosition(this);
            ComparisonView.CaptureMouse();
        }

        private void ComparisonView_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            ComparisonView.ReleaseMouseCapture();
        }

        private void ComparisonView_MouseMove(object sender, MouseEventArgs e)
        {
            if (ComparisonView.IsMouseCaptured)
            {
                var offset = _translateStart - e.GetPosition(this);
                _translateStart = e.GetPosition(this);

                var matrix = ComparisonView.RenderTransform.Value;
                matrix.Translate(offset.X * -1, offset.Y * -1);

                ComparisonView.RenderTransform = new MatrixTransform(matrix);
            }
        }

        private void ComparisonView_OnMouseWheel(object sender, MouseWheelEventArgs e)
        {
            var cursorPos = e.MouseDevice.GetPosition(ComparisonView);
            var matrix = ComparisonView.RenderTransform.Value;
            var scaleFactor = e.Delta > 0 ? 1.1 : 0.9;

            matrix.ScaleAtPrepend(scaleFactor, scaleFactor, cursorPos.X, cursorPos.Y);
            ComparisonView.RenderTransform = new MatrixTransform(matrix);
        }

        #endregion
    }
}