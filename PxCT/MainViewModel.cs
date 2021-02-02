namespace PxCT
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Drawing;
    using System.IO;
    using System.Linq;
    using System.Net.Http;
    using System.Runtime.CompilerServices;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Input;
    using System.Windows.Interop;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;
    using Color = System.Drawing.Color;
    using Point = System.Drawing.Point;

    internal class MainViewModel : INotifyPropertyChanged
    {
        #region Fields

        private readonly List<Template> _templates = new();

        private int[,] _canvas;

        private ImageSource _compareImage;

        #endregion

        public MainViewModel()
        {
            RefreshCommand = new DelegateCommand(o => ExecuteRefresh());
        }

        #region Delegates & Events

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        #region Properties

        public ImageSource CompareImage
        {
            get => _compareImage;
            set
            {
                _compareImage = value;
                OnPropertyChanged();
            }
        }

        #region Commands

        public ICommand RefreshCommand { get; }

        #endregion

        #endregion

        #region Methods

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #region Commands

        private async void ExecuteRefresh()
        {
            LoadTemplates();

            // TODO: Determine needed chunk area
            //var chunkTL = new Point(0, 0);
            //var chunkBR = new Point(0, 0);

            await LoadCanvasAsync();

            // DEBUG
            // DrawCanvas();

            var test = _templates.First();

            var offsetX = 448;
            var offsetY = 448;

            MarkErrors(test, offsetX, offsetY);

            DrawTemplate();

            // 2. check one pixel

            // 3. load template folder

            // 4. compare one image to canvas

            // 5. grey scale image

            // 6. show damage/missing on image

            // 7. add refresh button

            // 8 refresh only specific chunks
        }

        #endregion

        private void DrawCanvas()
        {
            var width = _canvas.GetUpperBound(0);
            var height = _canvas.GetUpperBound(1);

            var bmp = new Bitmap(width, height);
            for (var x = 0; x < width; x++)
            {
                for (var y = 0; y < height; y++)
                {
                    var color = CanvasColor.ConvertIdToColor(_canvas[x, y]);
                    bmp.SetPixel(x, y, color);
                }
            }

            // 448,448 of 0.0 chunk is 0:0

            // 960 x 960

            bmp.Save(@"D:\canvas.bmp");
        }

        //private void DrawErrors(Bitmap bmp)
        //{
        //    var width = _templates.First().Pixels.GetUpperBound(0);
        //    var height = _templates.First().Pixels.GetUpperBound(1);

        //    for (var x = (width / 2) + 1; x < width; x++)
        //    {
        //        for (var y = 0; y < height; y++)
        //        {
        //            bmp.SetPixel(x, y, color);
        //        }
        //    }

        //    // 960 x 960

        //    bmp.Save(@"D:\errors.bmp");
        //}

        private void DrawTemplate()
        {
            var width = _templates.First().Pixels.GetUpperBound(0) + 1;
            var height = _templates.First().Pixels.GetUpperBound(1) + 1;
            var bmp = new Bitmap(width * 2, height);

            for (var x = 0; x < width; x++)
            {
                for (var y = 0; y < height; y++)
                {
                    var color = CanvasColor.ConvertIdToColor(_templates.First().Pixels[x, y]);
                    bmp.SetPixel(x, y, color);

                    var colorError = _templates.First().Errors[x, y] ? Color.Red : color.ToGrayScale();
                    bmp.SetPixel(x + width, y, colorError);
                }
            }

            CompareImage = Imaging.CreateBitmapSourceFromHBitmap(bmp.GetHbitmap(), IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
        }

        private async Task LoadCanvasAsync()
        {
            using var client = new HttpClient();
            var response = await client.GetStreamAsync("https://api.pixelcanvas.io/api/bigchunk/0.0.bmp");

            using var sr = new BinaryReader(response);
            var bytes = sr.ReadBytes(460800);

            var colorCodes = new List<int>();

            foreach (var b in bytes)
            {
                if (b > 0) { ; }

                var codeA = (b >> 4) & 15;
                var codeB = b & 15;
                colorCodes.Add(codeA);
                colorCodes.Add(codeB);
            }

            var width = 960;
            var height = 960;

            var bmp = new int[width, height];

            for (var horChunkId = 0; horChunkId < 15; horChunkId++)
            {
                for (var verChunkId = 0; verChunkId < 15; verChunkId++)
                {
                    for (var x = 0; x < 64; x++)
                    {
                        for (var y = 0; y < 64; y++)
                        {
                            try
                            {
                                var pixelIndex = ((horChunkId + (verChunkId * 15)) * 4096) + x + (y * 64);
                                bmp[(horChunkId * 64) + x, (verChunkId * 64) + y] = colorCodes[pixelIndex];
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine(e);
                                throw;
                            }
                        }
                    }
                }
            }

            _canvas = bmp;
        }

        private void LoadTemplates()
        {
            var filename = @"templates\LittleFlutter_-182_359.png";
            var filenameChunks = filename.Split('_');

            var bmp = new Bitmap(filename);

            var template = new Template
            {
                Filename = filename,
                Pixels = new int[bmp.Width, bmp.Height],
                Errors = new bool[bmp.Width, bmp.Height],
                Position = new Point(int.Parse(filenameChunks[1]), int.Parse(filenameChunks[2].Split('.')[0]))
            };

            for (var x = 0; x < bmp.Width; x++)
            {
                for (var y = 0; y < bmp.Height; y++)
                {
                    var px = bmp.GetPixel(x, y);
                    template.Pixels[x, y] = px.ToColorId();
                }
            }

            _templates.Add(template);
        }

        private void MarkErrors(Template test, int offsetX, int offsetY)
        {
            for (var x = 0; x < test.Pixels.GetUpperBound(0); x++)
            {
                for (var y = 0; y < test.Pixels.GetUpperBound(1); y++)
                {
                    var targetColorId = test.Pixels[x, y];
                    var currentColorId = _canvas[x + test.Position.X + offsetX, y + test.Position.Y + offsetY];
                    var hasError = (targetColorId > -1) && (targetColorId != currentColorId);
                    test.Errors[x, y] = hasError;
                }
            }
        }

        #endregion
    }
}