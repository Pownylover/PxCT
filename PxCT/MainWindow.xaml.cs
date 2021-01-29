namespace PxCT
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.IO;
    using System.Net.Http;
    using System.Windows;
    using Point = System.Drawing.Point;

    /// <summary>Interaction logic for MainWindow.xaml</summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            this.InitializeComponent();

            // TODO: Determine needed chunk area
            var chunkTL = new Point(0, 0);
            var chunkBR = new Point(0, 0);

            this.GetCanvas();

            // 2. check one pixel

            // 3. load template folder

            // 4. compare one image to canvas

            // 5. grey scale image

            // 6. show damage/missing on image

            // 7. add refresh button
        }

        #region Methods

        private static void GetColor(Bitmap bmp, int x, int y, int colorcode)
        {
            var color = colorcode switch
            {
                0 => Color.White,
                1 => Color.LightGray,
                2 => Color.Gray,
                3 => Color.Black,
                4 => Color.Pink,
                5 => Color.Red,
                6 => Color.Orange,
                7 => Color.Brown,
                8 => Color.Yellow,
                9 => Color.GreenYellow,
                10 => Color.Green,
                11 => Color.DeepSkyBlue,
                12 => Color.SteelBlue,
                13 => Color.DarkBlue,
                14 => Color.MediumPurple,
                15 => Color.Purple,
                _ => throw new NotImplementedException()
            };

            bmp.SetPixel(x, y, color);
        }

        /// <summary>
        /// </summary>
        /// <remarks>Chunk IDs from top-left to bottom-right</remarks>
        /// <param name="chunkId"></param>
        private void AddChunk(int subChunkId, ICollection<int> colors)
        {
        }

        private async void GetCanvas()
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

            var bmp = new Bitmap(width, height);

            for (var horChunkId = 0; horChunkId < 15; horChunkId++)
            {
                for (var verChunkId = 0; verChunkId < 15; verChunkId++)
                {
                    for (var x = 0; x < 64; x++)
                    {
                        for (var y = 0; y < 64; y++)
                        {
                            var pixelIndex = ((horChunkId + (verChunkId * 15)) * 4096) + x + (y * 64);
                            GetColor(bmp, (horChunkId * 64) + x, (verChunkId * 64) + y, colorCodes[pixelIndex]);
                        }
                    }
                }
            }

            bmp.Save(@"D:\test.bmp");
        }

        #endregion
    }
}