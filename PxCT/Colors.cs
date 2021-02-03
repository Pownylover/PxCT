namespace PxCT
{
    using System;
    using System.Drawing;

    public static class CanvasColor
    {
        #region Properties

        // Source of color names: http://people.csail.mit.edu/jaffer/Color/resenecolours.txt

        public static Color Blue { get; } = Color.FromArgb(0, 0, 234);

        public static Color CapePalliser { get; } = Color.FromArgb(160, 106, 66);

        public static Color CarnationPink { get; } = Color.FromArgb(255, 167, 209);

        public static Color Conifer { get; } = Color.FromArgb(148, 224, 68);

        public static Color FreshEggplant { get; } = Color.FromArgb(130, 0, 128);

        public static Color Gray { get; } = Color.FromArgb(136, 136, 136);

        public static Color Green { get; } = Color.FromArgb(2, 190, 1);

        public static Color Lavender { get; } = Color.FromArgb(207, 110, 228);

        public static Color Lochmara { get; } = Color.FromArgb(0, 131, 199);

        public static Color Mercury { get; } = Color.FromArgb(228, 228, 228);

        public static Color Mineshaft { get; } = Color.FromArgb(34, 34, 34);

        public static Color Red { get; } = Color.FromArgb(229, 0, 0);

        public static Color RobinsEggBlue { get; } = Color.FromArgb(0, 211, 221);

        public static Color Tangerine { get; } = Color.FromArgb(229, 149, 0);

        public static Color Turbo { get; } = Color.FromArgb(229, 217, 0);

        public static Color White { get; } = Color.FromArgb(255, 255, 255);

        #endregion

        #region Methods

        public static Color ConvertIdToColor(int colorCode)
        {
            return colorCode switch
            {
                -1 => Color.Transparent,
                0 => White,
                1 => Mercury,
                2 => Gray,
                3 => Mineshaft,
                4 => CarnationPink,
                5 => Red,
                6 => Tangerine,
                7 => CapePalliser,
                8 => Turbo,
                9 => Conifer,
                10 => Green,
                11 => RobinsEggBlue,
                12 => Lochmara,
                13 => Blue,
                14 => Lavender,
                15 => FreshEggplant,
                _ => throw new NotImplementedException()
            };
        }

        public static int ToColorId(this Color color)
        {
            if (color.A == 0) { return -1; }

            if (color == White) { return 0; }

            if (color == Mercury) { return 1; }

            if (color == Gray) { return 2; }

            if (color == Mineshaft) { return 3; }

            if (color == CarnationPink) { return 4; }

            if (color == Red) { return 5; }

            if (color == Tangerine) { return 6; }

            if (color == CapePalliser) { return 7; }

            if (color == Turbo) { return 8; }

            if (color == Conifer) { return 9; }

            if (color == Green) { return 10; }

            if (color == RobinsEggBlue) { return 11; }

            if (color == Lochmara) { return 12; }

            if (color == Blue) { return 13; }

            if (color == Lavender) { return 14; }

            if (color == FreshEggplant) { return 15; }

            throw new ArgumentException("Unknown color");
        }

        #endregion
    }
}