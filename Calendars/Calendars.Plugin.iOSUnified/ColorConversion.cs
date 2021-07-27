using System.Linq;
using System.Text;

using CGColor = CoreGraphics.CGColor;

#nullable enable

namespace Plugin.Calendars
{
    /// <summary>
    /// iOS color conversion helpers
    /// </summary>
    internal static class ColorConversion
    {
        /// <summary>
        /// Creates a CGColor from a hex color string
        /// </summary>
        /// <param name="hexColor">Color string, in the hex form "#AARRGGBB"</param>
        /// <returns>Corresponding CGColor, or null if conversion failed</returns>
        public static CGColor? ToCGColor(string hexColor)
        {
            var trimmed = hexColor.Trim('#', ' ');

            if (string.IsNullOrEmpty(trimmed))
            {
                return null;
            }

            if (int.TryParse(trimmed, System.Globalization.NumberStyles.HexNumber, System.Globalization.CultureInfo.CurrentCulture, out int intColor))
            {
                byte b = (byte)intColor;
                byte g = (byte)(intColor >> 8);
                byte r = (byte)(intColor >> 16);
                byte a = (byte)(trimmed.Length <= 6 ? 255 : intColor >> 24);

                // CGColor uses 0-1 instead of 0-255
                return new CGColor(r / 255f, g / 255f, b / 255f, a / 255f);
            }

            return null;
        }

        /// <summary>
        /// Creates a hex color string from a CGColor
        /// </summary>
        /// <param name="cgColor">The CGColor to stringify</param>
        /// <returns>Corresponding color string, in the hex form "#AARRGGBB"</returns>
        public static string ToHexColor(CGColor cgColor)
        {
            var builder = new StringBuilder("#");

            // CGColor components are RGBA, but we want ARGB
            var argb = cgColor.Components.Skip(3).Concat(cgColor.Components.Take(3)).ToList();

            foreach (var component in argb)
            {
                builder.AppendFormat("{0:X2}", (int)(component * 255));
            }

            return builder.ToString();
        }
    }
}