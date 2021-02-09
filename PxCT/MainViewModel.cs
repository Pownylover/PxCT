namespace PxCT
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Drawing;
    using System.IO;
    using System.Linq;
    using System.Net.Http;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Input;
    using System.Windows.Media;
    using Color = System.Drawing.Color;
    using Point = System.Drawing.Point;

    internal class MainViewModel : BindableBase
    {
        #region Fields

        private Canvas _canvas;

        private ImageSource _compareImage;

        private Template _selectedTemplate;

        #endregion

        public MainViewModel()
        {
            // RefreshCommand = new DelegateCommand(o => ExecuteRefresh());
            Initialize();
        }

        #region Properties

        public ImageSource CompareImage
        {
            get => _compareImage;
            private set => SetProperty(value, ref _compareImage);
        }

        public Template SelectedTemplate
        {
            get => _selectedTemplate;
            set
            {
                SetProperty(value, ref _selectedTemplate);
                DrawComparison();
                (RefreshTemplateCommand as DelegateCommand).RaiseCanExecuteChanged();
            }
        }

        public ObservableCollection<Template> Templates { get; } = new();

        #region Commands

        public ICommand RefreshTemplateCommand { get; private set; }

        #endregion

        #endregion

        public ICommand RefreshCommand { get; }

        #endregion

        #endregion

        #region Methods

        #region Commands

        /// <summary>Reloads the big chunks occupied from the selected template.</summary>
        private async void ExecuteRefreshTemplate(object obj)
        {
            // get area
            var topLeft = new Point(SelectedTemplate.Area.X, SelectedTemplate.Area.Y);
            var bottomRight = new Point(topLeft.X + SelectedTemplate.Area.Width, topLeft.Y + SelectedTemplate.Area.Width);

            // convert to big chunk coordinates
            topLeft.X = (topLeft.X - ZeroOffset) / BigChunkSize;
            topLeft.Y = (topLeft.Y - ZeroOffset) / BigChunkSize;
            bottomRight.X = (bottomRight.X + ZeroOffset) / BigChunkSize;
            bottomRight.Y = (bottomRight.Y + ZeroOffset) / BigChunkSize;

            // load big chunks
            for (var x = topLeft.X; x <= bottomRight.X; x++)
            {
                for (var y = topLeft.Y; x <= bottomRight.Y; x++) { await LoadBigChunkAsync(new Point(x, y)); }
            }

            DrawComparison();
        }

        #endregion

        [Obsolete("Used only for debugging")]
        private void DrawCanvas()
        {
            var width = _canvas.Pixels.GetUpperBound(0) + 1;
            var height = _canvas.Pixels.GetUpperBound(1) + 1;

            var bmp = new Bitmap(width, height);
            for (var x = 0; x < width; x++)
            {
                for (var y = 0; y < height; y++)
                {
                    var color = CanvasColor.ConvertIdToColor(_canvas.Pixels[x, y]);
                    bmp.SetPixel(x, y, color);
                }
            }

            bmp.Save(@"D:\canvas.bmp");
        }

        /// <summary>Draws the template image next to the error highlighted, gray scaled image.</summary>
        private void DrawComparison()
        {
            var width = SelectedTemplate.Pixels.GetUpperBound(0) + 1;
            var height = SelectedTemplate.Pixels.GetUpperBound(1) + 1;
            var bmp = new Bitmap(width * 2, height);

            for (var x = 0; x < width; x++)
            {
                for (var y = 0; y < height; y++)
                {
                    var color = CanvasColor.ConvertIdToColor(SelectedTemplate.Pixels[x, y]);
                    bmp.SetPixel(x, y, color);

                    var colorError = SelectedTemplate.Errors[x, y] ? Color.Red : color.ToGrayScale();
                    bmp.SetPixel(x + width, y, colorError);
                }
            }

            CompareImage = bmp.ToImageSource();
        }

        private void FindTemplateErrors()
        {
            foreach (var template in Templates) { MarkErrors(template); }
        }

        private async void Initialize()
        {
            InitializeCommands();
            LoadTemplates();
            //await LoadCanvasAsync();
            //FindTemplateErrors();
        }

        private void InitializeCommands()
        {
            RefreshTemplateCommand = new DelegateCommand(ExecuteRefreshTemplate, o => SelectedTemplate != null);
        }

        private async Task LoadBigChunkAsync(Point coordinates)
        {
            using var client = new HttpClient();

            // the coordinates represent the small chunk positions
            var chunkUrl = $"{ChunkBaseUrl}/{coordinates.X * SmallChunksInBigChunk}.{coordinates.Y * SmallChunksInBigChunk}.bmp";
            var response = await client.GetStreamAsync(chunkUrl);

            using var sr = new BinaryReader(response);
            var bytes = sr.ReadBytes(BigChunkPixels / 2);

            // two color codes are encoded in one byte, each four bit long
            var colorCodes = new List<int>();
            foreach (var b in bytes)
            {
                var codeA = (b >> 4) & 15;
                var codeB = b & 15;
                colorCodes.Add(codeA);
                colorCodes.Add(codeB);
            }

            // big chunks contain 15x15 small chunks
            for (var smallChunkX = 0; smallChunkX < SmallChunksInBigChunk; smallChunkX++)
            {
                for (var smallChunkY = 0; smallChunkY < SmallChunksInBigChunk; smallChunkY++)
                {
                    // small chunks contain 64x64 pixels
                    for (var x = 0; x < SmallChunkSize; x++)
                    {
                        for (var y = 0; y < SmallChunkSize; y++)
                        {
                            var pixelIndex = ((smallChunkX + (smallChunkY * SmallChunksInBigChunk)) * SmallChunkPixels) + x + (y * SmallChunkSize);
                            var canvasX = (smallChunkX * SmallChunkSize) + x + (coordinates.X * BigChunkSize) + _canvas.ChunkOffset.X;
                            var canvasY = (smallChunkY * SmallChunkSize) + y + (coordinates.Y * BigChunkSize) + _canvas.ChunkOffset.Y;
                            _canvas.Pixels[canvasX, canvasY] = colorCodes[pixelIndex];
                        }
                    }
                }
            }
        }

        private async Task LoadCanvasAsync()
        {
            // todo: only load chunks containing templates
            // todo: currently a square area is loaded instead

            // constants
            const int bigChunkSize = 960;
            const int bigChunkPixels = bigChunkSize * bigChunkSize;
            const int zeroOffset = 448;
            const string chunkBaseUrl = "https://api.pixelcanvas.io/api/bigchunk";
            const int smallChunkSize = 64;
            const int smallChunkPixels = smallChunkSize * smallChunkSize;
            const int smallChunksInBigChunk = 15;

            // calculate needed chunks
            var topLeft = new Point(0, 0);
            var bottomRight = new Point(0, 0);

            foreach (var template in Templates)
            {
                topLeft.X = Math.Min(topLeft.X, template.Position.X);
                topLeft.Y = Math.Min(topLeft.Y, template.Position.Y);
                bottomRight.X = Math.Max(bottomRight.X, template.Position.X + template.Pixels.GetUpperBound(0));
                bottomRight.Y = Math.Max(bottomRight.Y, template.Position.Y + template.Pixels.GetUpperBound(1));
            }

            var chunkLeft = (topLeft.X - zeroOffset) / bigChunkSize;
            var chunkRight = (bottomRight.X + zeroOffset) / bigChunkSize;
            var chunkTop = (topLeft.Y - zeroOffset) / bigChunkSize;
            var chunkBottom = (bottomRight.Y + zeroOffset) / bigChunkSize;

            _canvas = CreateEmptyCanvas(chunkTopLeft, chunkBottomRight);

            // The canvas is created from big chunks
            for (var bigChunkX = chunkLeft; bigChunkX <= chunkRight; bigChunkX++)
            {
                for (var bigChunkY = chunkTop; bigChunkY <= chunkBottom; bigChunkY++)
                {
                    // check if chunk is actually needed
                    var bigChunkPos = new Point((bigChunkX * BigChunkSize) - ZeroOffset, (bigChunkY * BigChunkSize) - ZeroOffset);
                    var isBigChunkNeeded = Templates.Any(template => IsTemplateInChunk(template, bigChunkPos));
                    if (isBigChunkNeeded) { await LoadBigChunkAsync(new Point(bigChunkX, bigChunkY)); }
                }
            }
        }

        private void LoadTemplates()
        {
            var templateFiles = Directory.GetFiles("templates", "*.png");
            Templates.Clear();

            foreach (var templateFile in templateFiles)
            {
                try
                {
                    var filenameChunks = templateFile.Split('_');
                    if (filenameChunks.Length != 3) { throw new ArgumentException("Invalid Filename"); }

                    var bmp = new Bitmap(templateFile);

                    var template = new Template
                    {
                        Errors = new bool[bmp.Width, bmp.Height],
                        Filename = templateFile,
                        Image = bmp.ToImageSource(),
                        Name = filenameChunks[0].Split('\\').Last(),
                        Pixels = new int[bmp.Width, bmp.Height],
                        PixelCount = bmp.Width * bmp.Height,
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

                    Templates.Add(template);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Template {templateFile} could not be imported.\r\n\r\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void MarkErrors(Template template)
        {
            // base offset from 0:0 is 448 pixels in both directions
            var zeroOffsetX = 448 + _canvas.ChunkOffset.X;
            var zeroOffsetY = 448 + _canvas.ChunkOffset.Y;
            var errorCount = 0;

            for (var x = 0; x < template.Pixels.GetUpperBound(0); x++)
            {
                for (var y = 0; y < template.Pixels.GetUpperBound(1); y++)
                {
                    var targetColorId = template.Pixels[x, y];
                    var currentColorId = _canvas.Pixels[x + template.Position.X + zeroOffsetX, y + template.Position.Y + zeroOffsetY];
                    var hasError = (targetColorId > -1) && (targetColorId != currentColorId);
                    template.Errors[x, y] = hasError;
                    errorCount += hasError ? 1 : 0;
                }
            }

            template.ErrorCount = errorCount;
        }

        #endregion
    }
}