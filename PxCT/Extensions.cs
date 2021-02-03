namespace PxCT
{
    using System;
    using System.Drawing;
    using System.Windows;
    using System.Windows.Interop;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;
    using Color = System.Drawing.Color;

    public static class Extensions
    {
        #region Methods

        public static Color ToGrayScale(this Color color)
        {
            var gamma = (int)((color.R * 0.2126) + (color.G * 0.7152) + (color.B * 0.0722));
            return Color.FromArgb(gamma, gamma, gamma);
        }

        public static ImageSource ToImageSource(this Bitmap img)
        {
            return Imaging.CreateBitmapSourceFromHBitmap(img.GetHbitmap(), IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
        }

        #endregion
    }
}