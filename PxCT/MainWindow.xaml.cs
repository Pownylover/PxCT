namespace PxCT
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.IO;
    using System.Net.Http;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Input;
    using Point = System.Drawing.Point;

    /// <summary>Interaction logic for MainWindow.xaml</summary>
    public partial class MainWindow : Window
    {
        #region Fields

        private int[,] canvas;

        #endregion

        public MainWindow()
        {
            this.InitializeComponent();

            this.RefreshCommand = new DelegateCommand(o => this.ExecuteRefresh());
            this.DataContext = this;
        }

        #region Properties

        public ICommand RefreshCommand { get; }

        #endregion

        #region Methods

        private static Color GetColor(int colorCode)
        {
            return colorCode switch
            {
                0 => CanvasColor.White,
                1 => CanvasColor.Mercury,
                2 => CanvasColor.Gray,
                3 => CanvasColor.MineShaft,
                4 => CanvasColor.CarnationPink,
                5 => CanvasColor.Red,
                6 => CanvasColor.Tangerine,
                7 => CanvasColor.CapePalliser,
                8 => CanvasColor.Turbo,
                9 => CanvasColor.Conifer,
                10 => CanvasColor.Green,
                11 => CanvasColor.RobinsEggBlue,
                12 => CanvasColor.Lochmara,
                13 => CanvasColor.Blue,
                14 => CanvasColor.Lavender,
                15 => CanvasColor.FreshEggplant,
                _ => throw new NotImplementedException()
            };
        }

        /// <summary>
        /// </summary>
        /// <remarks>Chunk IDs from top-left to bottom-right</remarks>
        /// <param name="chunkId"></param>
        private void AddChunk(int subChunkId, ICollection<int> colors)
        {
        }

        private async void ExecuteRefresh()
        {
            // TODO: Determine needed chunk area
            var chunkTL = new Point(0, 0);
            var chunkBR = new Point(0, 0);

            this.canvas = await this.GetCanvasAsync();

            // DEBUG
            var bmp = new Bitmap(960, 960);
            for (var x = 0; x < this.canvas.GetUpperBound(0); x++)
            {
                for (var y = 0; y < this.canvas.GetUpperBound(1); y++)
                {
                    var color = GetColor(this.canvas[x, y]);
                    bmp.SetPixel(x, y, color);
                }
            }

            bmp.Save(@"D:\test.bmp");

            // 2. check one pixel

            // 3. load template folder

            // 4. compare one image to canvas

            // 5. grey scale image

            // 6. show damage/missing on image

            // 7. add refresh button
        }

        private async Task<int[,]> GetCanvasAsync()
        {
            using var client = new HttpClient();
            var response = await client.GetStreamAsync("https://api.pixelcanvas.io/api/bigchunk/0.0.bmp");

            using var sr = new BinaryReader(response);
            var bytes = sr.ReadBytes(460800);

            var colorCodes = new List<int>();

            foreach (var b in bytes)
            {
                if (b > 0)
                {
                    ;
                }

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

                            //GetColor(bmp, (horChunkId * 64) + x, (verChunkId * 64) + y, colorCodes[pixelIndex]);
                        }
                    }
                }
            }

            return bmp;
        }

        #endregion
    }
}