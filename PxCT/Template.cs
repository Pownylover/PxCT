namespace PxCT
{
    using System.Drawing;

    internal class Template
    {
        #region Properties

        public bool[,] Errors { get; set; }

        public string Filename { get; set; }

        public int[,] Pixels { get; set; }

        public Point Position { get; set; }

        #endregion
    }
}