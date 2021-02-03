namespace PxCT
{
    using System.Drawing;

    internal class Canvas
    {
        #region Properties

        /// <remarks>Used to convert pixelcanvas.io coordinates to absolute values.</remarks>
        public Point ChunkOffset { get; set; }

        public int[,] Pixels { get; set; }

        #endregion
    }
}