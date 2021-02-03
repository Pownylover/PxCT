namespace PxCT
{
    using System.Drawing;
    using System.Windows.Media;

    internal class Template : BindableBase
    {
        #region Fields

        private int _errorCount;

        #endregion

        #region Properties

        public int ErrorCount
        {
            get => _errorCount;
            set
            {
                SetProperty(value, ref _errorCount);
                OnPropertyChanged(nameof(GoodPixelCount));
                OnPropertyChanged(nameof(HasErrors));
            }
        }

        public bool[,] Errors { get; set; }

        public string Filename { get; set; }

        public int GoodPixelCount => PixelCount - ErrorCount;

        public bool HasErrors => ErrorCount > 0;

        public ImageSource Image { get; set; }

        public string Name { get; set; }

        public int PixelCount { get; set; }

        public int[,] Pixels { get; set; }

        public Point Position { get; set; }

        #endregion
    }
}