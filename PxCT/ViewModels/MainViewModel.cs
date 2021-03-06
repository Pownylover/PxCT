﻿namespace PxCT
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Drawing;
    using System.IO;
    using System.Linq;
    using System.Net.Http;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Media;
    using Color = System.Drawing.Color;
    using Point = System.Drawing.Point;

    internal class MainViewModel : BindableBase
    {
        private const int BigChunkSize = 960;

        private const int BigChunkPixels = BigChunkSize * BigChunkSize;

        private const int ZeroOffset = 448;

        private const string ChunkBaseUrl = "https://api.pixelcanvas.io/api/bigchunk";

        private const int SmallChunkSize = 64;

        private const int SmallChunkPixels = SmallChunkSize * SmallChunkSize;

        private const int SmallChunksInBigChunk = 15;

        #region Fields

        private Canvas _canvas;

        private ImageSource _compareImage;

        private Template _selectedTemplate;

        #endregion

        public MainViewModel()
        {
            Initialize();

            // todo: Show target color when hovering error pixels
            // todo: Search / Filter for templates
            // todo: Add refresh for chunks used in all/selected template
            // todo: Parallel loading of big chunks
            // todo: Make pixel grid toggleable
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
            }
        }

        public IEnumerable<Template> Templates { get; private set; }

        #endregion

        #region Methods

        private static Canvas CreateEmptyCanvas(Point chunkTopLeft, Point chunkBottomRight)
        {
            var offsetX = chunkTopLeft.X * BigChunkSize * -1;
            var offsetY = chunkTopLeft.Y * BigChunkSize * -1;
            var canvas = new Canvas
            {
                Pixels = new int[
                    BigChunkSize * (chunkTopLeft.X - chunkBottomRight.X - 1) * -1,
                    BigChunkSize * (chunkTopLeft.Y - chunkBottomRight.Y - 1) * -1],
                ChunkOffset = new Point(offsetX, offsetY)
            };
            return canvas;
        }

        /// <summary>Checks if at least one corner of a template is inside the chunk.</summary>
        /// <param name="template">The template to check.</param>
        /// <param name="chunkPos">The big chunk coordinates.</param>
        /// <returns>True if at least on corner is inside the big chunk, otherwise false.</returns>
        private static bool IsTemplateInChunk(Template template, Point chunkPos)
        {
            var isTopLeftIn = (template.Area.X >= chunkPos.X)
                              && (template.Area.X < chunkPos.X + BigChunkSize)
                              && (template.Area.Y >= chunkPos.Y)
                              && (template.Area.Y < chunkPos.Y + BigChunkSize);
            if (isTopLeftIn) { return true; }

            var isTopRightIn = (template.Area.X + template.Area.Width >= chunkPos.X)
                               && (template.Area.X + template.Area.Width < chunkPos.X + BigChunkSize)
                               && (template.Area.Y >= chunkPos.Y)
                               && (template.Area.Y < chunkPos.Y + BigChunkSize);
            if (isTopRightIn) { return true; }

            var isBottomLeftIn = (template.Area.X >= chunkPos.X)
                                 && (template.Area.X < chunkPos.X + BigChunkSize)
                                 && (template.Area.Y + template.Area.Height >= chunkPos.Y)
                                 && (template.Area.Y + template.Area.Height < chunkPos.Y + BigChunkSize);
            if (isBottomLeftIn) { return true; }

            var isBottomRightIn = (template.Area.X + template.Area.Width >= chunkPos.X)
                                  && (template.Area.X + template.Area.Width < chunkPos.X + BigChunkSize)
                                  && (template.Area.Y + template.Area.Height >= chunkPos.Y)
                                  && (template.Area.Y + template.Area.Height < chunkPos.Y + BigChunkSize);
            if (isBottomRightIn) { return true; }

            return false;
        }

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

            var isHorizontal = width * 9 <= height * 16;

            var bmp = new Bitmap(isHorizontal ? width * 2 : width, isHorizontal ? height : height * 2);

            for (var x = 0; x < width; x++)
            {
                for (var y = 0; y < height; y++)
                {
                    var color = CanvasColor.ConvertIdToColor(SelectedTemplate.Pixels[x, y]);
                    bmp.SetPixel(x, y, color);

                    var colorError = SelectedTemplate.Errors[x, y] ? Color.Red : color.ToGrayScale();
                    bmp.SetPixel(isHorizontal ? x + width : x, isHorizontal ? y : y + height, colorError);
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
            LoadTemplates();
            await LoadCanvasAsync();
            FindTemplateErrors();
        }

        private async Task LoadCanvasAsync()
        {
            // calculate needed chunks
            var topLeft = new Point(0, 0);
            var bottomRight = new Point(0, 0);

            foreach (var template in Templates)
            {
                topLeft.X = Math.Min(topLeft.X, template.Area.X);
                topLeft.Y = Math.Min(topLeft.Y, template.Area.Y);
                bottomRight.X = Math.Max(bottomRight.X, template.Area.X + template.Pixels.GetUpperBound(0));
                bottomRight.Y = Math.Max(bottomRight.Y, template.Area.Y + template.Pixels.GetUpperBound(1));
            }

            var chunkTopLeft = new Point((topLeft.X - ZeroOffset) / BigChunkSize, (topLeft.Y - ZeroOffset) / BigChunkSize);
            var chunkBottomRight = new Point((bottomRight.X + ZeroOffset) / BigChunkSize, (bottomRight.Y + ZeroOffset) / BigChunkSize);

            var canvas = CreateEmptyCanvas(chunkTopLeft, chunkBottomRight);

            using var client = new HttpClient();

            // The canvas is created from big chunks
            for (var bigChunkX = chunkTopLeft.X; bigChunkX <= chunkBottomRight.X; bigChunkX++)
            {
                for (var bigChunkY = chunkTopLeft.Y; bigChunkY <= chunkBottomRight.Y; bigChunkY++)
                {
                    // check if chunk is actually needed
                    var bigChunkPos = new Point((bigChunkX * BigChunkSize) - ZeroOffset, (bigChunkY * BigChunkSize) - ZeroOffset);
                    var isBigChunkNeeded = Templates.Any(template => IsTemplateInChunk(template, bigChunkPos));
                    if (!isBigChunkNeeded) { continue; }

                    // the coordinates represent the small chunk positions
                    var chunkUrl = $"{ChunkBaseUrl}/{bigChunkX * SmallChunksInBigChunk}.{bigChunkY * SmallChunksInBigChunk}.bmp";
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
                                    var canvasX = (smallChunkX * SmallChunkSize) + x + (bigChunkX * BigChunkSize) + canvas.ChunkOffset.X;
                                    var canvasY = (smallChunkY * SmallChunkSize) + y + (bigChunkY * BigChunkSize) + canvas.ChunkOffset.Y;
                                    canvas.Pixels[canvasX, canvasY] = colorCodes[pixelIndex];
                                }
                            }
                        }
                    }
                }
            }

            _canvas = canvas;
        }

        private void LoadTemplates()
        {
            var templateFiles = Directory.GetFiles("templates", "*.png");
            var templates = new ConcurrentBag<Template>();

            Parallel.ForEach(templateFiles, templateFile =>
            {
                try
                {
                    var filenameChunks = templateFile.Split('_');
                    if (filenameChunks.Length != 3) { throw new ArgumentException("Invalid Filename"); }

                    var bmp = new Bitmap(templateFile);
                    var imageSource = bmp.ToImageSource();
                    imageSource.Freeze();

                    var template = new Template
                    {
                        Errors = new bool[bmp.Width, bmp.Height],
                        Filename = templateFile,
                        Image = imageSource,
                        Name = filenameChunks[0].Split('\\').Last().AddSpacesBeforeUppercase(),
                        Pixels = new int[bmp.Width, bmp.Height],
                        PixelCount = bmp.Width * bmp.Height,
                        Area = new Rectangle(
                            int.Parse(filenameChunks[1]),
                            int.Parse(filenameChunks[2].Split('.')[0]),
                            bmp.Width,
                            bmp.Height)
                    };

                    for (var x = 0; x < bmp.Width; x++)
                    {
                        for (var y = 0; y < bmp.Height; y++)
                        {
                            var px = bmp.GetPixel(x, y);
                            template.Pixels[x, y] = px.ToColorId();
                        }
                    }

                    templates.Add(template);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Template {templateFile} could not be imported.\r\n\r\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            });

            Templates = new ObservableCollection<Template>(templates.OrderBy(o => o.Name));
        }

        private void MarkErrors(Template template)
        {
            // base offset from 0:0 is 448 pixels in both directions
            var zeroOffset = new Point(ZeroOffset + _canvas.ChunkOffset.X, ZeroOffset + _canvas.ChunkOffset.Y);
            var errorCount = 0;

            for (var x = 0; x <= template.Pixels.GetUpperBound(0); x++)
            {
                for (var y = 0; y <= template.Pixels.GetUpperBound(1); y++)
                {
                    var targetColorId = template.Pixels[x, y];
                    var currentColorId = _canvas.Pixels[x + template.Area.X + zeroOffset.X, y + template.Area.Y + zeroOffset.Y];
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