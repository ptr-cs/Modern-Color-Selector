﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ColorSelector
{
    public enum HslComponent
    {
        Hue = 0,
        Saturation = 1,
        Lightness = 2,
        Value = 3
    };

    public enum HsvComponent
    {
        Hue = 0,
        Saturation = 1,
        Value = 2,
    };

    public enum ColorModel
    {
        HSL = 0,
        HSV = 1
    };

    public class RawColor : IFormattable
    {
        public double A { get; set; } = 0;
        public double R { get; set; } = 0;
        public double G { get; set; } = 0;
        public double B { get; set; } = 0;
        public RawColor() { }
        public RawColor(double a, double r, double g, double b) 
        {
            A = a;
            R = r;
            G = g;
            B = b;
        }

        public string ToString(string? format, IFormatProvider? formatProvider)
        {
            return Color.FromArgb((byte)A, (byte)R, (byte)G, (byte)B).ToString();
        }
    }

    public class ArgbHexadecimalColorStringValidationRule : ValidationRule
    {
        public override ValidationResult Validate(object value, System.Globalization.CultureInfo cultureInfo)
        {
            return new ValidationResult(
                Regex.Match((string)value, "^#?(?:[0-9a-fA-F]{8})$").Success || Regex.Match((string)value, "^#?(?:[0-9a-fA-F]{6})$").Success,
                "String must match a valid RGB or ARGB Hexadecimal Color (ex. #FF001122)");
        }
    }

    public class ColorByteStringValidationRule : ValidationRule
    {
        public override ValidationResult Validate(object value, System.Globalization.CultureInfo cultureInfo)
        {
            return new ValidationResult(Double.TryParse((string)value, out _),
                String.Format("Value must be a number with an optional decimal space within the range [{0}, {1}]", Byte.MinValue, Byte.MaxValue));
        }
    }

    public class HueStringValidationRule : ValidationRule
    {
        public override ValidationResult Validate(object value, System.Globalization.CultureInfo cultureInfo)
        {
            return new ValidationResult(Regex.Match((string)value, "^(?:36[0]|3[0-5][0-9]|[12][0-9][0-9]|[1-9][0-9]|[0-9])$").Success,
                String.Format("Value must be an integer within the range [{0}, {1}]", 0, 360));
        }
    }

    public class SaturationStringValidationRule : ValidationRule
    {
        double val = -1;
        public override ValidationResult Validate(object value, System.Globalization.CultureInfo cultureInfo)
        {
            val = -1;
            return new ValidationResult(Double.TryParse((string)value, out val) && 0.0 <= val && val <= 100.0,
                String.Format("Value must be a number with an optional decimal space within the range [{0}, {1}]", 0.0, 100.0));
        }
    }

    public class ValueStringValidationRule : ValidationRule
    {
        double val = -1;
        public override ValidationResult Validate(object value, System.Globalization.CultureInfo cultureInfo)
        {
            val = -1;
            return new ValidationResult(Double.TryParse((string)value, out val) && 0.0 <= val && val <= 100.0,
                String.Format("Value must be a number with an optional decimal space within the range [{0}, {1}]", 0.0, 100.0));
        }
    }

    /// <summary>
    /// Scales and converts a double to a corresponding integral value (for display in UI Controls - 
    /// prohibiting floating-point values can simplify the color selection UX).
    /// </summary>
    [ValueConversion(typeof(double), typeof(int))]
    public class DoubleToScaledIntegerConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return System.Convert.ToInt32(System.Convert.ToDouble(value) * 100);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return System.Convert.ToDouble(System.Convert.ToInt32(value) / 100.0);
        }
    }

    /// <summary>
    /// Converts a double to a corresponding integral value (for display in UI Controls - 
    /// prohibiting floating-point values can simplify the color selection UX).
    /// </summary>
    [ValueConversion(typeof(double), typeof(int))]
    public class DoubleToIntegerConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return System.Convert.ToInt32(System.Convert.ToDouble(value));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return System.Convert.ToDouble(System.Convert.ToInt32(value));
        }
    }

    /// <summary>
    /// Converts a linear range into a Y-axis-symmetric range of absolute value transformed over the X-Axis 
    /// (resulting linear range post-conversion is a pyramid shape).
    /// </summary>
    [ValueConversion(typeof(double), typeof(double))]
    public class DoubleToReflectedAbsoluteValueDouble : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double inputVal = ((double)value) * 2.0;
            if (inputVal > 1)
                return 2.0 - inputVal;
            return inputVal;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return ((double)value);
        }
    }

    /// <summary>
    /// Converts a linear range into a boolean switch applied at 0.5, such that values less than the halfway
    /// are transformed by Math.Floor() and all other values are transformed by Math.Ceiling()
    /// </summary>
    [ValueConversion(typeof(double), typeof(double))]
    public class DoubleToBooleanSwitchDouble : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double inputVal = (double)value;
            if (inputVal < .5)
                return Math.Floor(.5);
            return Math.Ceiling(.5);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return ((double)value);
        }
    }

    /// <summary>
    /// Converts a double representing a percentage (0.0 to 1.0) to a double representing the same percentage
    /// of a given length value (such as a Control's ActualWidth or ActualHeight).
    /// </summary>
    [ValueConversion(typeof(double), typeof(double))]
    public class DoubleToLengthMultipliedDouble : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double inputVal = (double)value;
            double lengthParameter = (double)parameter;
            var result = lengthParameter * inputVal;
            if (result >= lengthParameter - 3)
                result = lengthParameter - 3;
            if (result < 0)
                result = 0;
            return result;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return ((double)value);
        }
    }

    /// <summary>
    /// Converts a double representing a percentage (0.0 to 1.0) to an inverted double representing the same percentage
    /// of a given length value (such as a Control's ActualWidth or ActualHeight).
    /// </summary>
    [ValueConversion(typeof(double), typeof(double))]
    public class DoubleToLengthMultipliedInvertedDouble : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double inputVal = (double)value;
            double lengthParameter = (double)parameter;
            var result = lengthParameter - (lengthParameter * inputVal);
            if (result >= lengthParameter - 3)
                result = lengthParameter - 3;
            if (result < 0)
                result = 0;
            return result;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return ((double)value);
        }
    }

    [ValueConversion(typeof(Point), typeof(Thickness))]
    public class DoubleToHslGuideLeftMargin : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double inputVal = (double)value;
            double lengthParameter = (double)parameter;
            var result = lengthParameter * inputVal;
            if (result >= lengthParameter - 3)
                result = lengthParameter - 3;
            if (result < 0)
                result = 0;
            return new Thickness(result, 0, 0, 0);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return ((Point)value);
        }
    }

    [ValueConversion(typeof(Point), typeof(Thickness))]
    public class DoubleToHslGuideTopMargin : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double inputVal = (double)value;
            double lengthParameter = (double)parameter;
            var result = lengthParameter - (lengthParameter * inputVal);
            if (result >= lengthParameter - 3)
                result = lengthParameter - 3;
            if (result < 0)
                result = 0;
            return new Thickness(0, result, 0, 0);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return ((Point)value);
        }
    }

    [ValueConversion(typeof(double), typeof(double))]
    public class DoubleToLengthMultipliedDoubleHue : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double inputVal = (double)value / 360.0;
            double lengthParameter = (double)parameter;
            var result = lengthParameter * inputVal;
            if (result >= lengthParameter - 3)
                result = lengthParameter - 3;
            if (result < 0)
                result = 0;
            return result;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return ((double)value);
        }
    }

    [ValueConversion(typeof(Point), typeof(Thickness))]
    public class DoubleToHslGuideLeftMarginHue : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double inputVal = (double)value / 360.0;
            double lengthParameter = (double)parameter;
            var result = lengthParameter * inputVal;
            if (result >= lengthParameter - 3)
                result = lengthParameter - 3;
            if (result < 0)
                result = 0;
            return new Thickness(result, 0, 0, 0);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return ((Point)value);
        }
    }

    [ValueConversion(typeof(HslComponent), typeof(bool))]
    public class HslComponentComparisonConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            HslComponent valueToConvert = (HslComponent)value;
            HslComponent compareValue = (HslComponent)parameter;

            return valueToConvert == compareValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return HslComponent.Hue; // Conversions back (not necessary) should default to Hue
        }
    }

    [ValueConversion(typeof(HslComponent), typeof(string))]
    public class HslComponentToAbbreviatedStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            HslComponent valueToConvert = (HslComponent)value;
            return valueToConvert.ToString()[..1];
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string valueToConvert = (string)value;
            var match = ((HslComponent[])Enum.GetValues(typeof(HslComponent))).Where(s => s.ToString()[..1] == valueToConvert).FirstOrDefault();
            return match;
        }
    }

    /// <summary>
    /// Converts full precision double into a rounded double (for UI display).
    /// Parameter is an optional integer that specifies number of decimal places to round to.
    /// </summary>
    [ValueConversion(typeof(double), typeof(double))]
    public class DoubleToTextBoxDouble : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double valueToConvert = (double)value;
            int decimalPlaces = (parameter == null) ? 1 : System.Convert.ToInt32(parameter);
            return Math.Round(valueToConvert, decimalPlaces);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double valueToConvert = System.Convert.ToDouble(value);
            return valueToConvert;
        }
    }

    [ValueConversion(typeof(double), typeof(double))]
    public class RotationAngleConverterY : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            int polarity = -1;
            return polarity * 90;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return System.Convert.ToDouble(value);
        }
    }

    [ValueConversion(typeof(double), typeof(double))]
    public class RotationAngleConverterX : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double angle = System.Convert.ToDouble(value);
            int polarity = -1;
            return polarity * angle;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return System.Convert.ToDouble(value);
        }
    }

    [ValueConversion(typeof(double), typeof(double))]
    public class RotationAngleConverterZ : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double angle = System.Convert.ToDouble(value);
            return (angle * 180) - 90.0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return System.Convert.ToDouble(value);
        }
    }

    public enum TemplatePart
    {
        PART_hexTextBox,
        PART_aTextBox,
        PART_rTextBox,
        PART_gTextBox,
        PART_bTextBox,
        PART_hTextBox,
        PART_sTextBox,
        PART_vTextBox,
        PART_aRangeBase,
        PART_rRangeBase,
        PART_gRangeBase,
        PART_bRangeBase,
        PART_hRangeBase,
        PART_sRangeBase,
        PART_vRangeBase,
        PART_hslRangeBase,
        PART_selectCustomColorButtonBase,
        PART_saveCustomColorButtonBase,
        PART_aIncrementButtonBase,
        PART_rIncrementButtonBase,
        PART_gIncrementButtonBase,
        PART_bIncrementButtonBase,
        PART_hIncrementButtonBase,
        PART_sIncrementButtonBase,
        PART_vIncrementButtonBase,
        PART_aDecrementButtonBase,
        PART_rDecrementButtonBase,
        PART_gDecrementButtonBase,
        PART_bDecrementButtonBase,
        PART_hDecrementButtonBase,
        PART_sDecrementButtonBase,
        PART_vDecrementButtonBase,
        PART_hslComponentAreaPanel,
        PART_hslComponentSelector,
        PART_colorModelSelector,
        PART_presetColorsSelector,
        PART_customColorsSelector,
        PART_hsl3dDisplayDecorator,
        PART_menuOpenButton,
        PART_menuCloseButton,
        PART_closeMenuDecorator,
        PART_colorModelsVisibilityToggleButton,
        PART_presetColorsVisibilityToggleButton,
        PART_display2dVisibilityToggleButton,
        PART_display3dVisibilityToggleButton,
        PART_componentsVisibilityToggleButton,
        PART_colorPreviewVisibilityToggleButton,
        PART_customColorsVisibilityToggleButton,
        PART_hexadecimalComponentVisibilityToggleButton,
        PART_alphaComponentVisibilityToggleButton,
        PART_rgbComponentVisibilityToggleButton,
        PART_hslvComponentVisibilityToggleButton,
    }

    [TemplatePart(Name = nameof(TemplatePart.PART_hexTextBox), Type = typeof(TextBox))]
    [TemplatePart(Name = nameof(TemplatePart.PART_aTextBox), Type = typeof(TextBox))]
    [TemplatePart(Name = nameof(TemplatePart.PART_rTextBox), Type = typeof(TextBox))]
    [TemplatePart(Name = nameof(TemplatePart.PART_gTextBox), Type = typeof(TextBox))]
    [TemplatePart(Name = nameof(TemplatePart.PART_bTextBox), Type = typeof(TextBox))]
    [TemplatePart(Name = nameof(TemplatePart.PART_hTextBox), Type = typeof(TextBox))]
    [TemplatePart(Name = nameof(TemplatePart.PART_sTextBox), Type = typeof(TextBox))]
    [TemplatePart(Name = nameof(TemplatePart.PART_vTextBox), Type = typeof(TextBox))]
    [TemplatePart(Name = nameof(TemplatePart.PART_aRangeBase), Type = typeof(RangeBase))]
    [TemplatePart(Name = nameof(TemplatePart.PART_rRangeBase), Type = typeof(RangeBase))]
    [TemplatePart(Name = nameof(TemplatePart.PART_gRangeBase), Type = typeof(RangeBase))]
    [TemplatePart(Name = nameof(TemplatePart.PART_bRangeBase), Type = typeof(RangeBase))]
    [TemplatePart(Name = nameof(TemplatePart.PART_hRangeBase), Type = typeof(RangeBase))]
    [TemplatePart(Name = nameof(TemplatePart.PART_sRangeBase), Type = typeof(RangeBase))]
    [TemplatePart(Name = nameof(TemplatePart.PART_vRangeBase), Type = typeof(RangeBase))]
    [TemplatePart(Name = nameof(TemplatePart.PART_hslRangeBase), Type = typeof(RangeBase))]
    [TemplatePart(Name = nameof(TemplatePart.PART_selectCustomColorButtonBase), Type = typeof(ButtonBase))]
    [TemplatePart(Name = nameof(TemplatePart.PART_saveCustomColorButtonBase), Type = typeof(ButtonBase))]
    [TemplatePart(Name = nameof(TemplatePart.PART_aIncrementButtonBase), Type = typeof(ButtonBase))]
    [TemplatePart(Name = nameof(TemplatePart.PART_rIncrementButtonBase), Type = typeof(ButtonBase))]
    [TemplatePart(Name = nameof(TemplatePart.PART_gIncrementButtonBase), Type = typeof(ButtonBase))]
    [TemplatePart(Name = nameof(TemplatePart.PART_bIncrementButtonBase), Type = typeof(ButtonBase))]
    [TemplatePart(Name = nameof(TemplatePart.PART_hIncrementButtonBase), Type = typeof(ButtonBase))]
    [TemplatePart(Name = nameof(TemplatePart.PART_sIncrementButtonBase), Type = typeof(ButtonBase))]
    [TemplatePart(Name = nameof(TemplatePart.PART_vIncrementButtonBase), Type = typeof(ButtonBase))]
    [TemplatePart(Name = nameof(TemplatePart.PART_aDecrementButtonBase), Type = typeof(ButtonBase))]
    [TemplatePart(Name = nameof(TemplatePart.PART_rDecrementButtonBase), Type = typeof(ButtonBase))]
    [TemplatePart(Name = nameof(TemplatePart.PART_gDecrementButtonBase), Type = typeof(ButtonBase))]
    [TemplatePart(Name = nameof(TemplatePart.PART_bDecrementButtonBase), Type = typeof(ButtonBase))]
    [TemplatePart(Name = nameof(TemplatePart.PART_hDecrementButtonBase), Type = typeof(ButtonBase))]
    [TemplatePart(Name = nameof(TemplatePart.PART_sDecrementButtonBase), Type = typeof(ButtonBase))]
    [TemplatePart(Name = nameof(TemplatePart.PART_vDecrementButtonBase), Type = typeof(ButtonBase))]
    [TemplatePart(Name = nameof(TemplatePart.PART_hslComponentAreaPanel), Type = typeof(Panel))]
    [TemplatePart(Name = nameof(TemplatePart.PART_hslComponentSelector), Type = typeof(Selector))]
    [TemplatePart(Name = nameof(TemplatePart.PART_colorModelSelector), Type = typeof(Selector))]
    [TemplatePart(Name = nameof(TemplatePart.PART_presetColorsSelector), Type = typeof(Selector))]
    [TemplatePart(Name = nameof(TemplatePart.PART_customColorsSelector), Type = typeof(Selector))]
    [TemplatePart(Name = nameof(TemplatePart.PART_hsl3dDisplayDecorator), Type = typeof(Decorator))]
    [TemplatePart(Name = nameof(TemplatePart.PART_menuOpenButton), Type = typeof(ButtonBase))]
    [TemplatePart(Name = nameof(TemplatePart.PART_menuCloseButton), Type = typeof(ButtonBase))]
    [TemplatePart(Name = nameof(TemplatePart.PART_closeMenuDecorator), Type = typeof(Decorator))]
    [TemplatePart(Name = nameof(TemplatePart.PART_colorModelsVisibilityToggleButton), Type = typeof(ToggleButton))]
    [TemplatePart(Name = nameof(TemplatePart.PART_presetColorsVisibilityToggleButton), Type = typeof(ToggleButton))]
    [TemplatePart(Name = nameof(TemplatePart.PART_display2dVisibilityToggleButton), Type = typeof(ToggleButton))]
    [TemplatePart(Name = nameof(TemplatePart.PART_display3dVisibilityToggleButton), Type = typeof(ToggleButton))]
    [TemplatePart(Name = nameof(TemplatePart.PART_componentsVisibilityToggleButton), Type = typeof(ToggleButton))]
    [TemplatePart(Name = nameof(TemplatePart.PART_colorPreviewVisibilityToggleButton), Type = typeof(ToggleButton))]
    [TemplatePart(Name = nameof(TemplatePart.PART_customColorsVisibilityToggleButton), Type = typeof(ToggleButton))]
    [TemplatePart(Name = nameof(TemplatePart.PART_hexadecimalComponentVisibilityToggleButton), Type = typeof(ToggleButton))]
    [TemplatePart(Name = nameof(TemplatePart.PART_alphaComponentVisibilityToggleButton), Type = typeof(ToggleButton))]
    [TemplatePart(Name = nameof(TemplatePart.PART_rgbComponentVisibilityToggleButton), Type = typeof(ToggleButton))]
    [TemplatePart(Name = nameof(TemplatePart.PART_hslvComponentVisibilityToggleButton), Type = typeof(ToggleButton))]

    public class ColorSelector : Control
    {
        // Rates of change and limits for different color components:
        public const int ARGB_ROC = 1;
        public const int ARGB_MIN = Byte.MinValue;
        public const int ARGB_MAX = Byte.MaxValue;

        public const int H_ROC = 1; // hue
        public const double SL_ROC = .01; // saturation and lightness
        public const int HSL_MIN = 0;
        public const int H_MAX = 360;
        public const int SL_MAX = 1;

        static ColorSelector()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ColorSelector), new FrameworkPropertyMetadata(typeof(ColorSelector)));
        }

        public static Color InterpolateColor(Color color1, Color color2, double fraction)
        {
            double byteToDouble = 1.0 / 255.0;
            fraction = Math.Min(fraction, 1.0);
            fraction = Math.Max(fraction, 0.0);

            double r1 = color1.R * byteToDouble;
            double g1 = color1.G * byteToDouble;
            double b1 = color1.B * byteToDouble;
            double a1 = color1.A * byteToDouble;

            double r2 = color2.R * byteToDouble;
            double g2 = color2.G * byteToDouble;
            double b2 = color2.B * byteToDouble;
            double a2 = color2.A * byteToDouble;

            double deltaRed = r2 - r1;
            double deltaGreen = g2 - g1;
            double deltaBlue = b2 - b1;
            double deltaAlpha = a2 - a1;

            double red = r1 + (deltaRed * fraction);
            double green = g1 + (deltaGreen * fraction);
            double blue = b1 + (deltaBlue * fraction);
            double alpha = a1 + (deltaAlpha * fraction);

            //red = Math.Min(red, 1.0);
            //red = Math.Max(red, 0.0);
            //green = Math.Min(green, 1.0);
            //green = Math.Max(green, 0.0);
            //blue = Math.Min(blue, 1.0);
            //blue = Math.Max(blue, 0.0);
            //alpha = Math.Min(alpha, 1.0);
            //alpha = Math.Max(alpha, 0.0);

            return Color.FromArgb((byte)(alpha * 255), (byte)(red * 255), (byte)(green * 255), (byte)(blue * 255));
        }

        public static Color BilinearInterpolateColor(Color color1, Color color2, Color color3, Color color4, double x, double y)
        {
            Color interpolatedColor1 = InterpolateColor(color1, color2, x);
            Color interpolatedColor2 = InterpolateColor(color3, color4, x);
            return InterpolateColor(interpolatedColor1, interpolatedColor2, y);
        }

        /// <summary>
        /// https://stackoverflow.com/questions/1546091/wpf-createbitmapsourcefromhbitmap-memory-leak
        /// </summary>
        /// <param name="hObject"></param>
        /// <returns></returns>
        [System.Runtime.InteropServices.DllImport("gdi32.dll")]
        private static extern bool DeleteObject(IntPtr hObject);

        public static ImageBrush CreateBilinearGradient(int w, int h, Color upperLeft, Color upperRight, Color lowerLeft, Color lowerRight)
        {
            BitmapSource? source;

            using (System.Drawing.Bitmap bmp = new(w, h))
            {
                System.Drawing.Graphics flagGraphics = System.Drawing.Graphics.FromImage(bmp);

                for (int x = 0; x < w; x++)
                {
                    for (int y = 0; y < h; y++)
                    {
                        double fracX = x / (w * 1.0);
                        double fracY = y / (h * 1.0);
                        Color color = BilinearInterpolateColor(upperLeft, upperRight, lowerLeft, lowerRight, fracX, fracY);
                        bmp.SetPixel(x, y, System.Drawing.Color.FromArgb(color.A, color.R, color.G, color.B));
                    }
                }

                IntPtr hBitmap = bmp.GetHbitmap();

                try
                {
                    source = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(hBitmap, IntPtr.Zero, Int32Rect.Empty, System.Windows.Media.Imaging.BitmapSizeOptions.FromEmptyOptions());
                }
                finally
                {
                    DeleteObject(hBitmap);
                }
            }

            return new ImageBrush() { ImageSource = source };
        }

        private TextBox? hextTextBox;
        private TextBox? HexTextBox
        {
            get { return hextTextBox;  }

            set
            {
                if (hextTextBox != null)
                {
                    hextTextBox.PreviewKeyDown -= new KeyEventHandler(TextBox_PreviewKeyDown);
                    BindingOperations.ClearBinding(hextTextBox, TextBox.TextProperty);
                }
                hextTextBox = value;

                if (hextTextBox != null)
                {
                    hextTextBox.MaxLength = 9; // Enforce Text length for ARGB values
                    Binding binding = new(nameof(HexValueString)) { Mode = BindingMode.TwoWay, Source = this, UpdateSourceTrigger = UpdateSourceTrigger.Default };
                    binding.ValidationRules.Add(new ArgbHexadecimalColorStringValidationRule());
                    hextTextBox.SetBinding(TextBox.TextProperty, binding);
                    hextTextBox.PreviewKeyDown += new KeyEventHandler(TextBox_PreviewKeyDown);
                }
            }
        }

        private TextBox? aTextBox;
        private TextBox? ATextBox
        {
            get { return aTextBox; }

            set
            {
                if (aTextBox != null)
                {
                    aTextBox.PreviewKeyDown -= new KeyEventHandler(TextBox_PreviewKeyDown);
                    aTextBox.GotKeyboardFocus -= new KeyboardFocusChangedEventHandler(TextBox_GotKeyboardFocus);
                    BindingOperations.ClearBinding(aTextBox, TextBox.TextProperty);
                }
                aTextBox = value;

                if (aTextBox != null)
                {
                    aTextBox.MaxLength = 5; // three integers, one decimal, one decimal place (ex. 123.4)
                    Binding binding = new(nameof(ATextBoxValue)) { Mode = BindingMode.TwoWay, Source = this, UpdateSourceTrigger = UpdateSourceTrigger.Default, Converter = new DoubleToTextBoxDouble() };
                    binding.ValidationRules.Add(new ColorByteStringValidationRule());
                    aTextBox.SetBinding(TextBox.TextProperty, binding);
                    aTextBox.GotKeyboardFocus += TextBox_GotKeyboardFocus;
                    aTextBox.PreviewKeyDown += TextBox_PreviewKeyDown;
                }
            }
        }

        private TextBox? rTextBox;
        private TextBox? RTextBox
        {
            get { return rTextBox; }

            set
            {
                if (rTextBox != null)
                {
                    rTextBox.PreviewKeyDown -= new KeyEventHandler(TextBox_PreviewKeyDown);
                    rTextBox.GotKeyboardFocus -= new KeyboardFocusChangedEventHandler(TextBox_GotKeyboardFocus);
                    BindingOperations.ClearBinding(rTextBox, TextBox.TextProperty);
                }
                rTextBox = value;

                if (rTextBox != null)
                {
                    rTextBox.MaxLength = 5; // three integers, one decimal, one decimal place (ex. 123.4)
                    Binding binding = new(nameof(RTextBoxValue)) { Mode = BindingMode.TwoWay, Source = this, UpdateSourceTrigger = UpdateSourceTrigger.Default, Converter = new DoubleToTextBoxDouble() };
                    binding.ValidationRules.Add(new ColorByteStringValidationRule());
                    rTextBox.SetBinding(TextBox.TextProperty, binding);
                    rTextBox.GotKeyboardFocus += TextBox_GotKeyboardFocus;
                    rTextBox.PreviewKeyDown += TextBox_PreviewKeyDown;
                }
            }
        }

        private TextBox? gTextBox;
        private TextBox? GTextBox
        {
            get { return gTextBox; }

            set
            {
                if (gTextBox != null)
                {
                    gTextBox.PreviewKeyDown -= new KeyEventHandler(TextBox_PreviewKeyDown);
                    gTextBox.GotKeyboardFocus -= new KeyboardFocusChangedEventHandler(TextBox_GotKeyboardFocus);
                    BindingOperations.ClearBinding(gTextBox, TextBox.TextProperty);
                }
                gTextBox = value;

                if (gTextBox != null)
                {
                    gTextBox.MaxLength = 5; // three integers, one decimal, one decimal place (ex. 123.4)
                    Binding binding = new(nameof(GTextBoxValue)) { Mode = BindingMode.TwoWay, Source = this, UpdateSourceTrigger = UpdateSourceTrigger.Default, Converter = new DoubleToTextBoxDouble() };
                    binding.ValidationRules.Add(new ColorByteStringValidationRule());
                    gTextBox.SetBinding(TextBox.TextProperty, binding);
                    gTextBox.GotKeyboardFocus += TextBox_GotKeyboardFocus;
                    gTextBox.PreviewKeyDown += TextBox_PreviewKeyDown;
                }
            }
        }

        private TextBox? bTextBox;
        private TextBox? BTextBox
        {
            get { return bTextBox; }

            set
            {
                if (bTextBox != null)
                {
                    bTextBox.PreviewKeyDown -= new KeyEventHandler(TextBox_PreviewKeyDown);
                    bTextBox.GotKeyboardFocus -= new KeyboardFocusChangedEventHandler(TextBox_GotKeyboardFocus);
                    BindingOperations.ClearBinding(bTextBox, TextBox.TextProperty);
                }
                bTextBox = value;

                if (bTextBox != null)
                {
                    bTextBox.MaxLength = 5; // three integers, one decimal, one decimal place (ex. 123.4)
                    Binding binding = new(nameof(BTextBoxValue)) { Mode = BindingMode.TwoWay, Source = this, UpdateSourceTrigger = UpdateSourceTrigger.Default, Converter = new DoubleToTextBoxDouble() };
                    binding.ValidationRules.Add(new ColorByteStringValidationRule());
                    bTextBox.SetBinding(TextBox.TextProperty, binding);
                    bTextBox.GotKeyboardFocus += TextBox_GotKeyboardFocus;
                    bTextBox.PreviewKeyDown += TextBox_PreviewKeyDown;
                }
            }
        }

        /// <summary>
        /// Automatically select a TextBox's text if focused via Keyboard navigation
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TextBox_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            TextBox tb = (TextBox)sender;
            tb.SelectAll();
        }

        private TextBox? hTextBox;
        private TextBox? HTextBox
        {
            get { return hTextBox; }

            set
            {
                if (hTextBox != null)
                {
                    hTextBox.PreviewKeyDown -= new KeyEventHandler(TextBox_PreviewKeyDown);
                    hTextBox.GotKeyboardFocus -= new KeyboardFocusChangedEventHandler(TextBox_GotKeyboardFocus);
                    BindingOperations.ClearBinding(hTextBox, TextBox.TextProperty);
                }
                hTextBox = value;

                if (hTextBox != null)
                {
                    hTextBox.MaxLength = 5; // Enforce maximum string length of possible angle values 0 - 360 degrees

                    Binding binding = new(nameof(HTextBoxValue)) { Mode = BindingMode.TwoWay, Source = this, UpdateSourceTrigger = UpdateSourceTrigger.Default, Converter = new DoubleToTextBoxDouble() };
                    binding.ValidationRules.Add(new HueStringValidationRule());
                    hTextBox.SetBinding(TextBox.TextProperty, binding);
                    hTextBox.GotKeyboardFocus += TextBox_GotKeyboardFocus;
                    hTextBox.PreviewKeyDown += TextBox_PreviewKeyDown;

                }
            }
        }

        private TextBox? sTextBox;
        private TextBox? STextBox
        {
            get { return sTextBox; }

            set
            {
                if (sTextBox != null)
                {
                    sTextBox.PreviewKeyDown -= new KeyEventHandler(TextBox_PreviewKeyDown);
                    sTextBox.GotKeyboardFocus -= new KeyboardFocusChangedEventHandler(TextBox_GotKeyboardFocus);
                    BindingOperations.ClearBinding(sTextBox, TextBox.TextProperty);
                }
                sTextBox = value;

                if (sTextBox != null)
                {
                    sTextBox.MaxLength = 5; // Enforce maximum string length of saturation upper bounds (100.0)

                    Binding binding = new(nameof(STextBoxValue)) { Mode = BindingMode.TwoWay, Source = this, UpdateSourceTrigger = UpdateSourceTrigger.Default, Converter = new DoubleToTextBoxDouble(), ConverterParameter = 2 };
                    binding.ValidationRules.Add(new SaturationStringValidationRule());
                    sTextBox.SetBinding(TextBox.TextProperty, binding);
                    sTextBox.GotKeyboardFocus += TextBox_GotKeyboardFocus;
                    sTextBox.PreviewKeyDown += TextBox_PreviewKeyDown;
                }
            }
        }

        private TextBox? vTextBox;
        private TextBox? VTextBox
        {
            get { return vTextBox; }

            set
            {
                if (vTextBox != null)
                {
                    vTextBox.PreviewKeyDown -= new KeyEventHandler(TextBox_PreviewKeyDown);
                    vTextBox.GotKeyboardFocus -= new KeyboardFocusChangedEventHandler(TextBox_GotKeyboardFocus);
                    BindingOperations.ClearBinding(vTextBox, TextBox.TextProperty);
                }
                vTextBox = value;

                if (vTextBox != null)
                {
                    vTextBox.MaxLength = 5; // Enforce maximum string length of lightness upper bounds (100.0)

                    Binding binding = new(nameof(VTextBoxValue)) { Mode = BindingMode.TwoWay, Source = this, UpdateSourceTrigger = UpdateSourceTrigger.Default, Converter = new DoubleToTextBoxDouble(), ConverterParameter = 2 };
                    binding.ValidationRules.Add(new ValueStringValidationRule());
                    vTextBox.SetBinding(TextBox.TextProperty, binding);
                    vTextBox.GotKeyboardFocus += TextBox_GotKeyboardFocus;
                    vTextBox.PreviewKeyDown += TextBox_PreviewKeyDown;
                }
            }
        }

        private RangeBase? aRangeBase;
        private RangeBase? ARangeBase
        {
            get { return aRangeBase; }

            set
            {
                if (aRangeBase != null)
                {
                    BindingOperations.ClearBinding(aRangeBase, RangeBase.ValueProperty);
                }
                aRangeBase = value;

                if (aRangeBase != null)
                {
                    aRangeBase.Maximum = Byte.MaxValue; // Enforce maximum for Byte values
                    aRangeBase.Minimum = Byte.MinValue; // Enforce minimum for Byte values
                    aRangeBase.SmallChange = ARGB_ROC;
                    aRangeBase.LargeChange = ARGB_ROC;

                    Binding binding = new(nameof(ARangeBaseValue)) { Mode = BindingMode.TwoWay, Source = this};
                    aRangeBase.SetBinding(RangeBase.ValueProperty, binding);

                    Color transparent = new() { A = 0, R = 0, G = 0, B = 0 };
                    LinearGradientBrush background = new() { GradientStops = new GradientStopCollection() { new GradientStop(transparent, 0), new GradientStop(Colors.Black, 1) }, EndPoint = new Point(1, 0) };
                    aRangeBase.Background = background;
                }
            }
        }

        readonly GradientStop rLowBoundGraientStop = new() { Offset = 0 };
        readonly GradientStop rHighBoundGradientStop = new() { Offset = 1 };
        private RangeBase? rRangeBase;
        private RangeBase? RRangeBase
        {
            get { return rRangeBase; }

            set
            {
                if (rRangeBase != null)
                {
                    BindingOperations.ClearBinding(rRangeBase, RangeBase.ValueProperty);
                    BindingOperations.ClearBinding(rLowBoundGraientStop, GradientStop.ColorProperty);
                    BindingOperations.ClearBinding(rHighBoundGradientStop, GradientStop.ColorProperty);
                }
                rRangeBase = value;

                if (rRangeBase != null)
                {
                    rRangeBase.Maximum = Byte.MaxValue; // Enforce maximum for Byte values
                    rRangeBase.Minimum = Byte.MinValue; // Enforce minimum for Byte values
                    rRangeBase.SmallChange = ARGB_ROC;
                    rRangeBase.LargeChange = ARGB_ROC;

                    Binding binding = new(nameof(RRangeBaseValue)) { Mode = BindingMode.TwoWay, Source = this };
                    rRangeBase.SetBinding(RangeBase.ValueProperty, binding);

                    Binding lowBoundBinding = new(nameof(RColorLowBound)) { Mode = BindingMode.OneWay, Source = this };
                    BindingOperations.SetBinding(rLowBoundGraientStop, GradientStop.ColorProperty, lowBoundBinding);
                    Binding highBoundBinding = new(nameof(RColorHighBound)) { Mode = BindingMode.OneWay, Source = this };
                    BindingOperations.SetBinding(rHighBoundGradientStop, GradientStop.ColorProperty, highBoundBinding);
                    LinearGradientBrush background = new() { GradientStops = new GradientStopCollection() { rLowBoundGraientStop, rHighBoundGradientStop }, EndPoint = new Point(1, 0) };
                    rRangeBase.Background = background;
                }
            }
        }

        readonly GradientStop gLowBoundGraientStop = new() { Offset = 0 };
        readonly GradientStop gHighBoundGradientStop = new() { Offset = 1 };
        private RangeBase? gRangeBase;
        private RangeBase? GRangeBase
        {
            get { return gRangeBase; }

            set
            {
                if (gRangeBase != null)
                {
                    BindingOperations.ClearBinding(gRangeBase, RangeBase.ValueProperty);
                    BindingOperations.ClearBinding(gLowBoundGraientStop, GradientStop.ColorProperty);
                    BindingOperations.ClearBinding(gHighBoundGradientStop, GradientStop.ColorProperty);
                }
                gRangeBase = value;

                if (gRangeBase != null)
                {
                    gRangeBase.Maximum = Byte.MaxValue; // Enforce maximum for Byte values
                    gRangeBase.Minimum = Byte.MinValue; // Enforce minimum for Byte values
                    gRangeBase.SmallChange = ARGB_ROC;
                    gRangeBase.LargeChange = ARGB_ROC;

                    Binding binding = new(nameof(GRangeBaseValue)) { Mode = BindingMode.TwoWay, Source = this };
                    gRangeBase.SetBinding(RangeBase.ValueProperty, binding);

                    Binding lowBoundBinding = new(nameof(GColorLowBound)) { Mode = BindingMode.OneWay, Source = this };
                    BindingOperations.SetBinding(gLowBoundGraientStop, GradientStop.ColorProperty, lowBoundBinding);
                    Binding highBoundBinding = new(nameof(GColorHighBound)) { Mode = BindingMode.OneWay, Source = this };
                    BindingOperations.SetBinding(gHighBoundGradientStop, GradientStop.ColorProperty, highBoundBinding);
                    LinearGradientBrush background = new() { GradientStops = new GradientStopCollection() { gLowBoundGraientStop, gHighBoundGradientStop }, EndPoint = new Point(1, 0) };
                    gRangeBase.Background = background;
                }
            }
        }

        readonly GradientStop bLowBoundGraientStop = new() { Offset = 0 };
        readonly GradientStop bHighBoundGradientStop = new() { Offset = 1 };
        private RangeBase? bRangeBase;
        private RangeBase? BRangeBase
        {
            get { return bRangeBase; }

            set
            {
                if (bRangeBase != null)
                {
                    BindingOperations.ClearBinding(bRangeBase, RangeBase.ValueProperty);
                    BindingOperations.ClearBinding(bLowBoundGraientStop, GradientStop.ColorProperty);
                    BindingOperations.ClearBinding(bHighBoundGradientStop, GradientStop.ColorProperty);
                }
                bRangeBase = value;

                if (bRangeBase != null)
                {
                    bRangeBase.Maximum = Byte.MaxValue; // Enforce maximum for Byte values
                    bRangeBase.Minimum = Byte.MinValue; // Enforce minimum for Byte values
                    bRangeBase.SmallChange = ARGB_ROC;
                    bRangeBase.LargeChange = ARGB_ROC;

                    Binding binding = new(nameof(BRangeBaseValue)) { Mode = BindingMode.TwoWay, Source = this };
                    bRangeBase.SetBinding(RangeBase.ValueProperty, binding);

                    Binding lowBoundBinding = new(nameof(BColorLowBound)) { Mode = BindingMode.OneWay, Source = this };
                    BindingOperations.SetBinding(bLowBoundGraientStop, GradientStop.ColorProperty, lowBoundBinding);
                    Binding highBoundBinding = new(nameof(BColorHighBound)) { Mode = BindingMode.OneWay, Source = this };
                    BindingOperations.SetBinding(bHighBoundGradientStop, GradientStop.ColorProperty, highBoundBinding);
                    LinearGradientBrush background = new() { GradientStops = new GradientStopCollection() { bLowBoundGraientStop, bHighBoundGradientStop }, EndPoint = new Point(1, 0) };
                    bRangeBase.Background = background;
                }
            }
        }

        readonly GradientStop hSector0GradientStop = new() { Offset = 0 };
        readonly GradientStop hSector1GradientStop = new() { Offset = 1.0 / 6 };
        readonly GradientStop hSector2GradientStop = new() { Offset = 1.0 / 6 * 2 };
        readonly GradientStop hSector3GradientStop = new() { Offset = 1.0 / 6 * 3 };
        readonly GradientStop hSector4GradientStop = new() { Offset = 1.0 / 6 * 4 };
        readonly GradientStop hSector5GradientStop = new() { Offset = 1.0 / 6 * 5 };
        readonly GradientStop hSector6GradientStop = new() { Offset = 1 };
        private RangeBase? hRangeBase;
        private RangeBase? HRangeBase
        {
            get { return hRangeBase; }

            set
            {
                if (hRangeBase != null)
                {
                    BindingOperations.ClearBinding(hRangeBase, RangeBase.ValueProperty);
                    BindingOperations.ClearBinding(hSector0GradientStop, GradientStop.ColorProperty);
                    BindingOperations.ClearBinding(hSector1GradientStop, GradientStop.ColorProperty);
                    BindingOperations.ClearBinding(hSector2GradientStop, GradientStop.ColorProperty);
                    BindingOperations.ClearBinding(hSector3GradientStop, GradientStop.ColorProperty);
                    BindingOperations.ClearBinding(hSector4GradientStop, GradientStop.ColorProperty);
                    BindingOperations.ClearBinding(hSector5GradientStop, GradientStop.ColorProperty);
                    BindingOperations.ClearBinding(hSector6GradientStop, GradientStop.ColorProperty);
                }
                hRangeBase = value;

                if (hRangeBase != null)
                {
                    hRangeBase.Maximum = H_MAX; // Enforce maximum for hue values
                    hRangeBase.Minimum = HSL_MIN; // Enforce minimum for hue values
                    hRangeBase.SmallChange = H_ROC;
                    hRangeBase.LargeChange = H_ROC;

                    Binding binding = new(nameof(HRangeBaseValue)) { Mode = BindingMode.TwoWay, Source = this };
                    hRangeBase.SetBinding(RangeBase.ValueProperty, binding);

                    Binding sector0Binding = new(nameof(HueSector0)) { Mode = BindingMode.OneWay, Source = this };
                    BindingOperations.SetBinding(hSector0GradientStop, GradientStop.ColorProperty, sector0Binding);
                    Binding sector1Binding = new(nameof(HueSector1)) { Mode = BindingMode.OneWay, Source = this };
                    BindingOperations.SetBinding(hSector1GradientStop, GradientStop.ColorProperty, sector1Binding);
                    Binding sector2Binding = new(nameof(HueSector2)) { Mode = BindingMode.OneWay, Source = this };
                    BindingOperations.SetBinding(hSector2GradientStop, GradientStop.ColorProperty, sector2Binding);
                    Binding sector3Binding = new(nameof(HueSector3)) { Mode = BindingMode.OneWay, Source = this };
                    BindingOperations.SetBinding(hSector3GradientStop, GradientStop.ColorProperty, sector3Binding);
                    Binding sector4Binding = new(nameof(HueSector4)) { Mode = BindingMode.OneWay, Source = this };
                    BindingOperations.SetBinding(hSector4GradientStop, GradientStop.ColorProperty, sector4Binding);
                    Binding sector5Binding = new(nameof(HueSector5)) { Mode = BindingMode.OneWay, Source = this };
                    BindingOperations.SetBinding(hSector5GradientStop, GradientStop.ColorProperty, sector5Binding);
                    Binding sector6Binding = new(nameof(HueSector0)) { Mode = BindingMode.OneWay, Source = this };
                    BindingOperations.SetBinding(hSector6GradientStop, GradientStop.ColorProperty, sector6Binding);

                    LinearGradientBrush background = new() { GradientStops = new GradientStopCollection() { hSector0GradientStop, hSector1GradientStop, hSector2GradientStop, hSector3GradientStop, hSector4GradientStop, hSector5GradientStop, hSector6GradientStop }, EndPoint = new Point(1,0) };
                    hRangeBase.Background = background;
                }
            }
        }

        readonly GradientStop sLowBoundGraientStop = new() { Offset = 0 };
        readonly GradientStop sHighBoundGradientStop = new() { Offset = 1 };
        private RangeBase? sRangeBase;
        private RangeBase? SRangeBase
        {
            get { return sRangeBase; }

            set
            {
                if (sRangeBase != null)
                {
                    BindingOperations.ClearBinding(sRangeBase, RangeBase.ValueProperty);
                    BindingOperations.ClearBinding(sLowBoundGraientStop, GradientStop.ColorProperty);
                    BindingOperations.ClearBinding(sHighBoundGradientStop, GradientStop.ColorProperty);
                }
                sRangeBase = value;

                if (sRangeBase != null)
                {
                    sRangeBase.Maximum = SL_MAX; // Enforce maximum for saturation values
                    sRangeBase.Minimum = HSL_MIN; // Enforce minimum for saturation values
                    sRangeBase.SmallChange = SL_ROC;
                    sRangeBase.LargeChange = SL_ROC;

                    Binding binding = new(nameof(SRangeBaseValue)) { Mode = BindingMode.TwoWay, Source = this };
                    sRangeBase.SetBinding(RangeBase.ValueProperty, binding);

                    Binding lowBoundBinding = new(nameof(SColorLowBound)) { Mode = BindingMode.OneWay, Source = this };
                    BindingOperations.SetBinding(sLowBoundGraientStop, GradientStop.ColorProperty, lowBoundBinding);
                    Binding highBoundBinding = new(nameof(SColorHighBound)) { Mode = BindingMode.OneWay, Source = this };
                    BindingOperations.SetBinding(sHighBoundGradientStop, GradientStop.ColorProperty, highBoundBinding);
                    LinearGradientBrush background = new() { GradientStops = new GradientStopCollection() { sLowBoundGraientStop, sHighBoundGradientStop }, EndPoint = new Point(1, 0) };
                    sRangeBase.Background = background;
                }
            }
        }

        readonly GradientStop vLowBoundGraientStop = new() { Offset = 0 };
        readonly GradientStop vHighBoundGradientStop = new() { Offset = 1 };
        private RangeBase? vRangeBase;
        private RangeBase? VRangeBase
        {
            get { return vRangeBase; }

            set
            {
                if (vRangeBase != null)
                {
                    BindingOperations.ClearBinding(vRangeBase, RangeBase.ValueProperty);
                    BindingOperations.ClearBinding(vLowBoundGraientStop, GradientStop.ColorProperty);
                    BindingOperations.ClearBinding(vHighBoundGradientStop, GradientStop.ColorProperty);
                }
                vRangeBase = value;

                if (vRangeBase != null)
                {
                    vRangeBase.Maximum = SL_MAX; // Enforce maximum for value values
                    vRangeBase.Minimum = HSL_MIN; // Enforce minimum for value values
                    vRangeBase.SmallChange = SL_ROC;
                    vRangeBase.LargeChange = SL_ROC;

                    Binding binding = new(nameof(VRangeBaseValue)) { Mode = BindingMode.TwoWay, Source = this };
                    vRangeBase.SetBinding(RangeBase.ValueProperty, binding);

                    Binding lowBoundBinding = new(nameof(VColorLowBound)) { Mode = BindingMode.OneWay, Source = this };
                    BindingOperations.SetBinding(vLowBoundGraientStop, GradientStop.ColorProperty, lowBoundBinding);
                    Binding highBoundBinding = new(nameof(VColorHighBound)) { Mode = BindingMode.OneWay, Source = this };
                    BindingOperations.SetBinding(vHighBoundGradientStop, GradientStop.ColorProperty, highBoundBinding);
                    LinearGradientBrush background = new() { GradientStops = new GradientStopCollection() { vLowBoundGraientStop, vHighBoundGradientStop }, EndPoint = new Point(1, 0) };
                    vRangeBase.Background = background;
                }
            }
        }

        private RangeBase? hslRangeBase;
        private RangeBase? HslRangeBase
        {
            get { return hslRangeBase; }

            set
            {
                if (hslRangeBase != null)
                {
                    BindingOperations.ClearBinding(hslRangeBase, RangeBase.ValueProperty);
                }
                hslRangeBase = value;

                if (hslRangeBase != null)
                {
                    hslRangeBase.SmallChange = SL_ROC;
                    hslRangeBase.LargeChange = SL_ROC;

                    Binding binding = new(nameof(HRangeBaseValue)) { Mode = BindingMode.TwoWay, Source = this };
                    hslRangeBase.SetBinding(RangeBase.ValueProperty, binding);
                }
            }
        }

        private ButtonBase? menuOpenButtonBase;
        private ButtonBase? MenuOpenButtonBase
        {
            get { return menuOpenButtonBase; }

            set
            {
                if (menuOpenButtonBase != null)
                {
                    menuOpenButtonBase.Click -= new RoutedEventHandler(MenuButtonBase_Click);
                }
                menuOpenButtonBase = value;

                if (menuOpenButtonBase != null)
                {
                    menuOpenButtonBase.Click += new RoutedEventHandler(MenuButtonBase_Click);
                }
            }
        }

        private ButtonBase? menuCloseButtonBase;
        private ButtonBase? MenuCloseButtonBase
        {
            get { return menuCloseButtonBase; }

            set
            {
                if (menuCloseButtonBase != null)
                {
                    menuCloseButtonBase.Click -= new RoutedEventHandler(MenuButtonBase_Click);
                }
                menuCloseButtonBase = value;

                if (menuCloseButtonBase != null)
                {
                    menuCloseButtonBase.Click += new RoutedEventHandler(MenuButtonBase_Click);
                }
            }
        }

        private Decorator? closeMenuDecorator;
        private Decorator? CloseMenuDecorator
        {
            get { return closeMenuDecorator; }

            set
            {
                if (closeMenuDecorator != null)
                {
                    closeMenuDecorator.PreviewMouseUp -= new MouseButtonEventHandler(CloseMenuDecorator_PreviewMouseUp);
                }
                closeMenuDecorator = value;

                if (closeMenuDecorator != null)
                {
                    closeMenuDecorator.PreviewMouseUp += new MouseButtonEventHandler(CloseMenuDecorator_PreviewMouseUp);
                }
            }
        }

        private ToggleButton? colorModelsVisibilityToggleButton;
        private ToggleButton? ColorModelsVisibilityToggleButton
        {
            get { return colorModelsVisibilityToggleButton; }

            set
            {
                if (colorModelsVisibilityToggleButton != null)
                {
                    colorModelsVisibilityToggleButton.Click -= new RoutedEventHandler(ColorModelsVisibilityToggleButton_Click);
                }
                colorModelsVisibilityToggleButton = value;

                if (colorModelsVisibilityToggleButton != null)
                {
                    colorModelsVisibilityToggleButton.Click += new RoutedEventHandler(ColorModelsVisibilityToggleButton_Click);
                }
            }
        }

        private void ColorModelsVisibilityToggleButton_Click(object sender, RoutedEventArgs e)
        {
            ColorModelsVisible = !ColorModelsVisible;
        }

        private ToggleButton? presetColorsVisibilityToggleButton;
        private ToggleButton? PresetColorsVisibilityToggleButton
        {
            get { return presetColorsVisibilityToggleButton; }

            set
            {
                if (presetColorsVisibilityToggleButton != null)
                {
                    presetColorsVisibilityToggleButton.Click -= new RoutedEventHandler(PresetColorsVisibilityToggleButton_Click);
                }
                presetColorsVisibilityToggleButton = value;

                if (presetColorsVisibilityToggleButton != null)
                {
                    presetColorsVisibilityToggleButton.Click += new RoutedEventHandler(PresetColorsVisibilityToggleButton_Click);
                }
            }
        }

        private void PresetColorsVisibilityToggleButton_Click(object sender, RoutedEventArgs e)
        {
            PresetColorsVisible = !PresetColorsVisible;
        }

        private ToggleButton? display2dVisibilityToggleButton;
        private ToggleButton? Display2dVisibilityToggleButton
        {
            get { return display2dVisibilityToggleButton; }

            set
            {
                if (display2dVisibilityToggleButton != null)
                {
                    display2dVisibilityToggleButton.Click -= new RoutedEventHandler(Display2dVisibilityToggleButton_Click);
                }
                display2dVisibilityToggleButton = value;

                if (display2dVisibilityToggleButton != null)
                {
                    display2dVisibilityToggleButton.Click += new RoutedEventHandler(Display2dVisibilityToggleButton_Click);
                }
            }
        }

        private void Display2dVisibilityToggleButton_Click(object sender, RoutedEventArgs e)
        {
            Display2dVisible = !Display2dVisible;
        }

        private ToggleButton? display3dVisibilityToggleButton;
        private ToggleButton? Display3dVisibilityToggleButton
        {
            get { return display3dVisibilityToggleButton; }

            set
            {
                if (display3dVisibilityToggleButton != null)
                {
                    display3dVisibilityToggleButton.Click -= new RoutedEventHandler(Display3dVisibilityToggleButton_Click);
                }
                display3dVisibilityToggleButton = value;

                if (display3dVisibilityToggleButton != null)
                {
                    display3dVisibilityToggleButton.Click += new RoutedEventHandler(Display3dVisibilityToggleButton_Click);
                }
            }
        }

        private void Display3dVisibilityToggleButton_Click(object sender, RoutedEventArgs e)
        {
            Display3dVisible = !Display3dVisible;
        }

        private ToggleButton? componentsVisibilityToggleButton;
        private ToggleButton? ComponentsVisibilityToggleButton
        {
            get { return componentsVisibilityToggleButton; }

            set
            {
                if (componentsVisibilityToggleButton != null)
                {
                    componentsVisibilityToggleButton.Click -= new RoutedEventHandler(ComponentsVisibilityToggleButton_Click);
                }
                componentsVisibilityToggleButton = value;

                if (componentsVisibilityToggleButton != null)
                {
                    componentsVisibilityToggleButton.Click += new RoutedEventHandler(ComponentsVisibilityToggleButton_Click);
                }
            }
        }

        private void ComponentsVisibilityToggleButton_Click(object sender, RoutedEventArgs e)
        {
            ComponentsVisible = !ComponentsVisible;
        }

        private ToggleButton? colorPreviewVisibilityToggleButton;
        private ToggleButton? ColorPreviewVisibilityToggleButton
        {
            get { return colorPreviewVisibilityToggleButton; }

            set
            {
                if (colorPreviewVisibilityToggleButton != null)
                {
                    colorPreviewVisibilityToggleButton.Click -= new RoutedEventHandler(ColorPreviewVisibilityToggleButton_Click);
                }
                colorPreviewVisibilityToggleButton = value;

                if (colorPreviewVisibilityToggleButton != null)
                {
                    colorPreviewVisibilityToggleButton.Click += new RoutedEventHandler(ColorPreviewVisibilityToggleButton_Click);
                }
            }
        }

        private void ColorPreviewVisibilityToggleButton_Click(object sender, RoutedEventArgs e)
        {
            ColorPreviewVisible = !ColorPreviewVisible;
        }

        private ToggleButton? customColorsVisibilityToggleButton;
        private ToggleButton? CustomColorsVisibilityToggleButton
        {
            get { return customColorsVisibilityToggleButton; }

            set
            {
                if (customColorsVisibilityToggleButton != null)
                {
                    customColorsVisibilityToggleButton.Click -= new RoutedEventHandler(CustomColorsVisibilityToggleButton_Click);
                }
                customColorsVisibilityToggleButton = value;

                if (customColorsVisibilityToggleButton != null)
                {
                    customColorsVisibilityToggleButton.Click += new RoutedEventHandler(CustomColorsVisibilityToggleButton_Click);
                }
            }
        }

        private void CustomColorsVisibilityToggleButton_Click(object sender, RoutedEventArgs e)
        {
            CustomColorsVisible = !CustomColorsVisible;
        }

        private ToggleButton? hexadecimalComponentVisibilityToggleButton;
        private ToggleButton? HexadecimalComponentVisibilityToggleButton
        {
            get { return hexadecimalComponentVisibilityToggleButton; }

            set
            {
                if (hexadecimalComponentVisibilityToggleButton != null)
                {
                    hexadecimalComponentVisibilityToggleButton.Click -= new RoutedEventHandler(HexadecimalComponentVisibilityToggleButton_Click);
                }
                hexadecimalComponentVisibilityToggleButton = value;

                if (hexadecimalComponentVisibilityToggleButton != null)
                {
                    hexadecimalComponentVisibilityToggleButton.Click += new RoutedEventHandler(HexadecimalComponentVisibilityToggleButton_Click);
                }
            }
        }

        private void HexadecimalComponentVisibilityToggleButton_Click(object sender, RoutedEventArgs e)
        {
            HexadecimalComponentVisible = !HexadecimalComponentVisible;
        }

        private ToggleButton? alphaComponentVisibilityToggleButton;
        private ToggleButton? AlphaComponentVisibilityToggleButton
        {
            get { return alphaComponentVisibilityToggleButton; }

            set
            {
                if (alphaComponentVisibilityToggleButton != null)
                {
                    alphaComponentVisibilityToggleButton.Click -= new RoutedEventHandler(AlphaComponentVisibilityToggleButton_Click);
                }
                alphaComponentVisibilityToggleButton = value;

                if (alphaComponentVisibilityToggleButton != null)
                {
                    alphaComponentVisibilityToggleButton.Click += new RoutedEventHandler(AlphaComponentVisibilityToggleButton_Click);
                }
            }
        }

        private void AlphaComponentVisibilityToggleButton_Click(object sender, RoutedEventArgs e)
        {
            AlphaComponentVisible = !AlphaComponentVisible;
        }

        private ToggleButton? rgbComponentVisibilityToggleButton;
        private ToggleButton? RgbComponentVisibilityToggleButton
        {
            get { return rgbComponentVisibilityToggleButton; }

            set
            {
                if (rgbComponentVisibilityToggleButton != null)
                {
                    rgbComponentVisibilityToggleButton.Click -= new RoutedEventHandler(RgbComponentVisibilityToggleButton_Click);
                }
                rgbComponentVisibilityToggleButton = value;

                if (rgbComponentVisibilityToggleButton != null)
                {
                    rgbComponentVisibilityToggleButton.Click += new RoutedEventHandler(RgbComponentVisibilityToggleButton_Click);
                }
            }
        }

        private void RgbComponentVisibilityToggleButton_Click(object sender, RoutedEventArgs e)
        {
            RgbComponentVisible = !RgbComponentVisible;
        }

        private ToggleButton? hslvComponentVisibilityToggleButton;
        private ToggleButton? HslvComponentVisibilityToggleButton
        {
            get { return hslvComponentVisibilityToggleButton; }

            set
            {
                if (hslvComponentVisibilityToggleButton != null)
                {
                    hslvComponentVisibilityToggleButton.Click -= new RoutedEventHandler(HslvComponentVisibilityToggleButton_Click);
                }
                hslvComponentVisibilityToggleButton = value;

                if (hslvComponentVisibilityToggleButton != null)
                {
                    hslvComponentVisibilityToggleButton.Click += new RoutedEventHandler(HslvComponentVisibilityToggleButton_Click);
                }
            }
        }

        private void HslvComponentVisibilityToggleButton_Click(object sender, RoutedEventArgs e)
        {
            HslvComponentVisible = !HslvComponentVisible;
        }

        private void CloseMenuDecorator_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            IsMenuOpen = false;
        }

        private void MenuButtonBase_Click(object sender, RoutedEventArgs e)
        {
            IsMenuOpen = !IsMenuOpen;
        }

        private ButtonBase? selectCustomColorButtonBase;
        private ButtonBase? SelectCustomColorButtonBase
        {
            get { return selectCustomColorButtonBase; }

            set
            {
                if (selectCustomColorButtonBase != null)
                {
                    selectCustomColorButtonBase.Click -= new RoutedEventHandler(SelectCustomColorButtonBase_Click);
                }
                selectCustomColorButtonBase = value;

                if (selectCustomColorButtonBase != null)
                {
                    selectCustomColorButtonBase.Click += new RoutedEventHandler(SelectCustomColorButtonBase_Click);
                }
            }
        }

        private ButtonBase? saveCustomColorButtonBase;
        private ButtonBase? SaveCustomColorButtonBase
        {
            get { return saveCustomColorButtonBase; }

            set
            {
                if (saveCustomColorButtonBase != null)
                {
                    saveCustomColorButtonBase.Click -= new RoutedEventHandler(SaveCustomColorButtonBase_Click);
                }
                saveCustomColorButtonBase = value;

                if (saveCustomColorButtonBase != null)
                {
                    saveCustomColorButtonBase.Click += new RoutedEventHandler(SaveCustomColorButtonBase_Click);
                }
            }
        }

        private ButtonBase? aIncrementButtonBase;
        private ButtonBase? AIncrementButtonBase
        {
            get { return aIncrementButtonBase; }

            set
            {
                if (aIncrementButtonBase != null)
                {
                    aIncrementButtonBase.Click -= new RoutedEventHandler(AIncrementButtonBase_Click);
                }
                aIncrementButtonBase = value;

                if (aIncrementButtonBase != null)
                {
                    aIncrementButtonBase.Click += AIncrementButtonBase_Click;
                }
            }
        }

        private ButtonBase? rIncrementButtonBase;
        private ButtonBase? RIncrementButtonBase
        {
            get { return rIncrementButtonBase; }

            set
            {
                if (rIncrementButtonBase != null)
                {
                    rIncrementButtonBase.Click -= new RoutedEventHandler(RIncrementButtonBase_Click);
                }
                rIncrementButtonBase = value;

                if (rIncrementButtonBase != null)
                {
                    rIncrementButtonBase.Click += RIncrementButtonBase_Click;
                }
            }
        }

        private ButtonBase? gIncrementButtonBase;
        private ButtonBase? GIncrementButtonBase
        {
            get { return gIncrementButtonBase; }

            set
            {
                if (gIncrementButtonBase != null)
                {
                    gIncrementButtonBase.Click -= new RoutedEventHandler(GIncrementButtonBase_Click);
                }
                gIncrementButtonBase = value;

                if (gIncrementButtonBase != null)
                {
                    gIncrementButtonBase.Click += GIncrementButtonBase_Click;
                }
            }
        }

        private ButtonBase? bIncrementButtonBase;
        private ButtonBase? BIncrementButtonBase
        {
            get { return bIncrementButtonBase; }

            set
            {
                if (bIncrementButtonBase != null)
                {
                    bIncrementButtonBase.Click -= new RoutedEventHandler(BIncrementButtonBase_Click);
                }
                bIncrementButtonBase = value;

                if (bIncrementButtonBase != null)
                {
                    bIncrementButtonBase.Click += BIncrementButtonBase_Click;
                }
            }
        }

        private ButtonBase? hIncrementButtonBase;
        private ButtonBase? HIncrementButtonBase
        {
            get { return hIncrementButtonBase; }

            set
            {
                if (hIncrementButtonBase != null)
                {
                    hIncrementButtonBase.Click -= new RoutedEventHandler(HIncrementButtonBase_Click);
                }
                hIncrementButtonBase = value;

                if (hIncrementButtonBase != null)
                {
                    hIncrementButtonBase.Click += HIncrementButtonBase_Click;
                }
            }
        }

        private ButtonBase? sIncrementButtonBase;
        private ButtonBase? SIncrementButtonBase
        {
            get { return sIncrementButtonBase; }

            set
            {
                if (sIncrementButtonBase != null)
                {
                    sIncrementButtonBase.Click -= new RoutedEventHandler(SIncrementButtonBase_Click);
                }
                sIncrementButtonBase = value;

                if (sIncrementButtonBase != null)
                {
                    sIncrementButtonBase.Click += SIncrementButtonBase_Click;
                }
            }
        }

        private ButtonBase? vIncrementButtonBase;
        private ButtonBase? VIncrementButtonBase
        {
            get { return vIncrementButtonBase; }

            set
            {
                if (vIncrementButtonBase != null)
                {
                    vIncrementButtonBase.Click -= new RoutedEventHandler(VIncrementButtonBase_Click);
                }
                vIncrementButtonBase = value;

                if (vIncrementButtonBase != null)
                {
                    vIncrementButtonBase.Click += VIncrementButtonBase_Click;
                }
            }
        }

        private ButtonBase? aDecrementButtonBase;
        private ButtonBase? ADecrementButtonBase
        {
            get { return aDecrementButtonBase; }

            set
            {
                if (aDecrementButtonBase != null)
                {
                    aDecrementButtonBase.Click -= new RoutedEventHandler(ADecrementButtonBase_Click);
                }
                aDecrementButtonBase = value;

                if (aDecrementButtonBase != null)
                {
                    aDecrementButtonBase.Click += ADecrementButtonBase_Click;
                }
            }
        }

        private ButtonBase? rDecrementButtonBase;
        private ButtonBase? RDecrementButtonBase
        {
            get { return rDecrementButtonBase; }

            set
            {
                if (rDecrementButtonBase != null)
                {
                    rDecrementButtonBase.Click -= new RoutedEventHandler(RDecrementButtonBase_Click);
                }
                rDecrementButtonBase = value;

                if (rDecrementButtonBase != null)
                {
                    rDecrementButtonBase.Click += RDecrementButtonBase_Click;
                }
            }
        }

        private ButtonBase? gDecrementButtonBase;
        private ButtonBase? GDecrementButtonBase
        {
            get { return gDecrementButtonBase; }

            set
            {
                if (gDecrementButtonBase != null)
                {
                    gDecrementButtonBase.Click -= new RoutedEventHandler(GDecrementButtonBase_Click);
                }
                gDecrementButtonBase = value;

                if (gDecrementButtonBase != null)
                {
                    gDecrementButtonBase.Click += GDecrementButtonBase_Click;
                }
            }
        }

        private ButtonBase? bDecrementButtonBase;
        private ButtonBase? BDecrementButtonBase
        {
            get { return bDecrementButtonBase; }

            set
            {
                if (bDecrementButtonBase != null)
                {
                    bDecrementButtonBase.Click -= new RoutedEventHandler(BDecrementButtonBase_Click);
                }
                bDecrementButtonBase = value;

                if (bDecrementButtonBase != null)
                {
                    bDecrementButtonBase.Click += BDecrementButtonBase_Click;
                }
            }
        }

        private ButtonBase? hDecrementButtonBase;
        private ButtonBase? HDecrementButtonBase
        {
            get { return hDecrementButtonBase; }

            set
            {
                if (hDecrementButtonBase != null)
                {
                    hDecrementButtonBase.Click -= new RoutedEventHandler(HDecrementButtonBase_Click);
                }
                hDecrementButtonBase = value;

                if (hDecrementButtonBase != null)
                {
                    hDecrementButtonBase.Click += HDecrementButtonBase_Click;
                }
            }
        }

        private ButtonBase? sDecrementButtonBase;
        private ButtonBase? SDecrementButtonBase
        {
            get { return sDecrementButtonBase; }

            set
            {
                if (sDecrementButtonBase != null)
                {
                    sDecrementButtonBase.Click -= new RoutedEventHandler(SDecrementButtonBase_Click);
                }
                sDecrementButtonBase = value;

                if (sDecrementButtonBase != null)
                {
                    sDecrementButtonBase.Click += SDecrementButtonBase_Click;
                }
            }
        }

        private ButtonBase? vDecrementButtonBase;
        private ButtonBase? VDecrementButtonBase
        {
            get { return vDecrementButtonBase; }

            set
            {
                if (vDecrementButtonBase != null)
                {
                    vDecrementButtonBase.Click -= new RoutedEventHandler(VDecrementButtonBase_Click);
                }
                vDecrementButtonBase = value;

                if (vDecrementButtonBase != null)
                {
                    vDecrementButtonBase.Click += VDecrementButtonBase_Click;
                }
            }
        }

        DiffuseMaterial faceBrushDiffuseMaterial2 = new DiffuseMaterial()
        {
            Brush = CreateBilinearGradient(100, 100, Colors.Cyan, Colors.White, Colors.Lime, Colors.Yellow)
        };
        DiffuseMaterial faceBrushDiffuseMaterial6 = new DiffuseMaterial()
        {
            Brush = CreateBilinearGradient(100, 100, Colors.White, Colors.Magenta, Colors.Yellow, Colors.Red)
        };
        DiffuseMaterial faceBrushDiffuseMaterial1 = new DiffuseMaterial()
        {
            Brush = CreateBilinearGradient(100, 100, Colors.Blue, Colors.Magenta, Colors.Cyan, Colors.White)
        };
        DiffuseMaterial faceBrushDiffuseMaterial5 = new DiffuseMaterial()
        {
            Brush = CreateBilinearGradient(100, 100, Colors.Lime, Colors.Yellow, Colors.Black, Colors.Red)
        };
        DiffuseMaterial faceBrushDiffuseMaterial4 = new DiffuseMaterial()
        {
            Brush = CreateBilinearGradient(100, 100, Colors.Blue, Colors.Cyan, Colors.Black, Colors.Lime)
        };
        DiffuseMaterial faceBrushDiffuseMaterial3 = new DiffuseMaterial()
        {
            Brush = CreateBilinearGradient(100, 100, Colors.Black, Colors.Red, Colors.Blue, Colors.Magenta)
        };
        DiffuseMaterial faceBrushDiffuseMaterialDesaturated2 = new DiffuseMaterial()
        {
            Brush = CreateBilinearGradient(100, 100, Colors.Gray, Colors.White, Colors.Gray, Colors.Gray)
        };
        DiffuseMaterial faceBrushDiffuseMaterialDesaturated6 = new DiffuseMaterial()
        {
            Brush = CreateBilinearGradient(100, 100, Colors.White, Colors.Gray, Colors.Gray, Colors.Gray)
        };
        DiffuseMaterial faceBrushDiffuseMaterialDesaturated1 = new DiffuseMaterial()
        {
            Brush = CreateBilinearGradient(100, 100, Colors.Gray, Colors.Gray, Colors.Gray, Colors.White)
        };
        DiffuseMaterial faceBrushDiffuseMaterialDesaturated5 = new DiffuseMaterial()
        {
            Brush = CreateBilinearGradient(100, 100, Colors.Gray, Colors.Gray, Colors.Black, Colors.Gray)
        };
        DiffuseMaterial faceBrushDiffuseMaterialDesaturated4 = new DiffuseMaterial()
        {
            Brush = CreateBilinearGradient(100, 100, Colors.Gray, Colors.Gray, Colors.Black, Colors.Gray)
        };
        DiffuseMaterial faceBrushDiffuseMaterialDesaturated3 = new DiffuseMaterial()
        {
            Brush = CreateBilinearGradient(100, 100, Colors.Black, Colors.Gray, Colors.Gray, Colors.Gray)
        };

        readonly AxisAngleRotation3D modelVisual3dRotationZ = new(new Vector3D(0, 1, 0), 0);
        readonly AxisAngleRotation3D modelVisual3dRotationY = new(new Vector3D(0, 1, 0), -90);
        readonly AxisAngleRotation3D modelVisual3dRotationX = new(new Vector3D(-1, 0, 0), 0);

        public ModelVisual3D Hsl3dDisplayModelVisual3DCone = new();
        public ModelVisual3D Hsl3dDisplayModelVisual3DCube = new();
        public Viewport3D Hsl3dDisplayViewport3D = new Viewport3D() { Height = 100, Width = 200, ClipToBounds = false };
        Viewbox viewbox = new Viewbox() { MaxHeight = 380 };
        private Decorator? hsl3dDisplayDecorator;
        private Decorator? Hsl3dDisplayDecorator
        {
            get { return hsl3dDisplayDecorator; }

            set
            {
                if (hsl3dDisplayDecorator != null)
                {
                    hsl3dDisplayDecorator.PreviewMouseUp -= new MouseButtonEventHandler(Hsl3dDisplayDecorator_PreviewMouseUp);
                    hsl3dDisplayDecorator.PreviewMouseDown -= new MouseButtonEventHandler(Hsl3dDisplayDecorator_PreviewMouseDown);
                    hsl3dDisplayDecorator.PreviewMouseWheel -= new MouseWheelEventHandler(Hsl3dDisplayDecorator_PreviewMouseWheel);
                    hsl3dDisplayDecorator.PreviewMouseMove -= new MouseEventHandler(Hsl3dDisplayDecorator_PreviewMouseMove);

                    // BindingOperations.ClearBinding(modelVisual3dRotationY, AxisAngleRotation3D.AngleProperty);
                    BindingOperations.ClearBinding(modelVisual3dRotationZ, AxisAngleRotation3D.AngleProperty);
                    BindingOperations.ClearBinding(modelVisual3dRotationX, AxisAngleRotation3D.AngleProperty);
                    BindingOperations.ClearBinding(faceBrushDiffuseMaterial1.Brush, Brush.OpacityProperty);
                    BindingOperations.ClearBinding(faceBrushDiffuseMaterial2.Brush, Brush.OpacityProperty);
                    BindingOperations.ClearBinding(faceBrushDiffuseMaterial3.Brush, Brush.OpacityProperty);
                    BindingOperations.ClearBinding(faceBrushDiffuseMaterial4.Brush, Brush.OpacityProperty);
                    BindingOperations.ClearBinding(faceBrushDiffuseMaterial5.Brush, Brush.OpacityProperty);
                    BindingOperations.ClearBinding(faceBrushDiffuseMaterial6.Brush, Brush.OpacityProperty);
                }
                hsl3dDisplayDecorator = value;

                if (hsl3dDisplayDecorator != null)
                {
                    //Binding angleBindingY = new(nameof(H)) { Mode = BindingMode.OneWay, Source = this, Converter = new RotationAngleConverterY() };
                    //BindingOperations.SetBinding(modelVisual3dRotationY, AxisAngleRotation3D.AngleProperty, angleBindingY);
                    Binding angleBindingX = new(nameof(H)) { Mode = BindingMode.OneWay, Source = this, Converter = new RotationAngleConverterX() };
                    BindingOperations.SetBinding(modelVisual3dRotationX, AxisAngleRotation3D.AngleProperty, angleBindingX);
                    Binding angleBindingZ = new(nameof(V)) { Mode = BindingMode.OneWay, Source = this, Converter = new RotationAngleConverterZ() };
                    BindingOperations.SetBinding(modelVisual3dRotationZ, AxisAngleRotation3D.AngleProperty, angleBindingZ);

                    Binding brushOpacityBinding1 = new(nameof(S)) { Mode = BindingMode.OneWay, Source = this };
                    BindingOperations.SetBinding(faceBrushDiffuseMaterial1.Brush, Brush.OpacityProperty, brushOpacityBinding1);
                    Binding brushOpacityBinding2 = new(nameof(S)) { Mode = BindingMode.OneWay, Source = this };
                    BindingOperations.SetBinding(faceBrushDiffuseMaterial2.Brush, Brush.OpacityProperty, brushOpacityBinding2);
                    Binding brushOpacityBinding3 = new(nameof(S)) { Mode = BindingMode.OneWay, Source = this };
                    BindingOperations.SetBinding(faceBrushDiffuseMaterial3.Brush, Brush.OpacityProperty, brushOpacityBinding3);
                    Binding brushOpacityBinding4 = new(nameof(S)) { Mode = BindingMode.OneWay, Source = this };
                    BindingOperations.SetBinding(faceBrushDiffuseMaterial4.Brush, Brush.OpacityProperty, brushOpacityBinding4);
                    Binding brushOpacityBinding5 = new(nameof(S)) { Mode = BindingMode.OneWay, Source = this };
                    BindingOperations.SetBinding(faceBrushDiffuseMaterial5.Brush, Brush.OpacityProperty, brushOpacityBinding5);
                    Binding brushOpacityBinding6 = new(nameof(S)) { Mode = BindingMode.OneWay, Source = this };
                    BindingOperations.SetBinding(faceBrushDiffuseMaterial6.Brush, Brush.OpacityProperty, brushOpacityBinding6);

                    Hsl3dDisplayViewport3D.Camera = new OrthographicCamera(new Point3D(0, 0, -1), new Vector3D(0, 0, 1), new Vector3D(0, 1, 0), 1.6)
                    {
                        NearPlaneDistance = double.NegativeInfinity,
                        FarPlaneDistance = double.PositiveInfinity,
                    };

                    Hsl3dDisplayModelVisual3DCube.Content = GenerateRgbCubeModel3DGroup();
                    Hsl3dDisplayModelVisual3DCone.Content = GenerateRgbConeModel3DGroup();

                    var transform3DGroup = new Transform3DGroup();
                    transform3DGroup.Children.Add(new RotateTransform3D(modelVisual3dRotationY));
                    transform3DGroup.Children.Add(new RotateTransform3D(modelVisual3dRotationX));
                    transform3DGroup.Children.Add(new RotateTransform3D(modelVisual3dRotationZ));
                    Hsl3dDisplayModelVisual3DCube.Transform = transform3DGroup;

                    Hsl3dDisplayViewport3D.Children.Add(Hsl3dDisplayModelVisual3DCube);
                    viewbox.Child = Hsl3dDisplayViewport3D;
                    hsl3dDisplayDecorator.Child = viewbox;

                    hsl3dDisplayDecorator.Cursor = Cursors.Hand;
                    hsl3dDisplayDecorator.PreviewMouseMove += new MouseEventHandler(Hsl3dDisplayDecorator_PreviewMouseMove);
                    hsl3dDisplayDecorator.PreviewMouseWheel += new MouseWheelEventHandler(Hsl3dDisplayDecorator_PreviewMouseWheel);
                    hsl3dDisplayDecorator.PreviewMouseDown += new MouseButtonEventHandler(Hsl3dDisplayDecorator_PreviewMouseDown);
                    hsl3dDisplayDecorator.PreviewMouseUp += new MouseButtonEventHandler(Hsl3dDisplayDecorator_PreviewMouseUp);
                }
            }
        }

        public Model3DGroup GenerateRgbConeModel3DGroup()
        {
            Model3DGroup model3DGroup = new();
            model3DGroup.Transform = new Transform3DGroup()
            {
                Children = {
                    new ScaleTransform3D(new Vector3D(-.33, -.33, -.33)),
                    new TranslateTransform3D(new Vector3D(0,.1,0)),
                    new RotateTransform3D()
                    {
                        Rotation = new AxisAngleRotation3D(new Vector3D(-1, 0, 0), 120)
                    }
                }
            };

            MaterialGroup group = new MaterialGroup();
            group.Children.Add(new DiffuseMaterial(new RadialGradientBrush(new GradientStopCollection() { new GradientStop(Colors.White, 0), new GradientStop(Colors.Gray, 1) })));

            GeometryModel3D meshId0Geometry = new GeometryModel3D()
            {
                Material = group,
                Geometry = new MeshGeometry3D()
                {

                    Positions = new Point3DCollection(new List<Point3D>()
        {
        new Point3D(0.980785, 0.19509, -1),
        new Point3D(0.19509, -0.980785, -1),
        new Point3D(-0.980785, -0.19509, -1),
        new Point3D(-0.19509, 0.980785, -1),
        new Point3D(0, 1, -1),
        new Point3D(0.19509, 0.980785, -1),
        new Point3D(0.382683, 0.92388, -1),
        new Point3D(0.55557, 0.83147, -1),
        new Point3D(0.707107, 0.707107, -1),
        new Point3D(0.83147, 0.55557, -1),
        new Point3D(0.92388, 0.382683, -1),
        new Point3D(1, 0, -1),
        new Point3D(0.980785, -0.19509, -1),
        new Point3D(0.92388, -0.382683, -1),
        new Point3D(0.83147, -0.55557, -1),
        new Point3D(0.707107, -0.707107, -1),
        new Point3D(0.55557, -0.83147, -1),
        new Point3D(0.382683, -0.92388, -1),
        new Point3D(0, -1, -1),
        new Point3D(-0.19509, -0.980785, -1),
        new Point3D(-0.382683, -0.92388, -1),
        new Point3D(-0.55557, -0.831469, -1),
        new Point3D(-0.707107, -0.707107, -1),
        new Point3D(-0.831469, -0.55557, -1),
        new Point3D(-0.92388, -0.382684, -1),
        new Point3D(-1, 0, -1),
        new Point3D(-0.980785, 0.19509, -1),
        new Point3D(-0.92388, 0.382684, -1),
        new Point3D(-0.83147, 0.55557, -1),
        new Point3D(-0.707107, 0.707107, -1),
        new Point3D(-0.55557, 0.83147, -1),
        new Point3D(-0.382683, 0.92388, -1),

        }),

                    Normals = new Vector3DCollection(new List<Vector3D>()
        {
        new Vector3D(0, 0, -1),
        new Vector3D(0, 0, -1),
        new Vector3D(0, 0, -1),
        new Vector3D(0, 0, -1),
        new Vector3D(0, 0, -1),
        new Vector3D(0, 0, -1),
        new Vector3D(0, 0, -1),
        new Vector3D(0, 0, -1),
        new Vector3D(0, 0, -1),
        new Vector3D(0, 0, -1),
        new Vector3D(0, 0, -1),
        new Vector3D(0, 0, -1),
        new Vector3D(0, 0, -1),
        new Vector3D(0, 0, -1),
        new Vector3D(0, 0, -1),
        new Vector3D(0, 0, -1),
        new Vector3D(0, 0, -1),
        new Vector3D(0, 0, -1),
        new Vector3D(0, 0, -1),
        new Vector3D(0, 0, -1),
        new Vector3D(0, 0, -1),
        new Vector3D(0, 0, -1),
        new Vector3D(0, 0, -1),
        new Vector3D(0, 0, -1),
        new Vector3D(0, 0, -1),
        new Vector3D(0, 0, -1),
        new Vector3D(0, 0, -1),
        new Vector3D(0, 0, -1),
        new Vector3D(0, 0, -1),
        new Vector3D(0, 0, -1),
        new Vector3D(0, 0, -1),
        new Vector3D(0, 0, -1),

        }),

                    TextureCoordinates = new PointCollection(new List<Point>()
        {
        new Point(0.985389, 0.296822),
        new Point(0.796822, 0.0146115),
        new Point(0.514612, 0.203178),
        new Point(0.703178, 0.485388),
        new Point(0.75, 0.49),
        new Point(0.796822, 0.485388),
        new Point(0.841844, 0.471731),
        new Point(0.883337, 0.449553),
        new Point(0.919706, 0.419706),
        new Point(0.949553, 0.383337),
        new Point(0.971731, 0.341844),
        new Point(0.99, 0.25),
        new Point(0.985389, 0.203178),
        new Point(0.971731, 0.158156),
        new Point(0.949553, 0.116663),
        new Point(0.919706, 0.0802944),
        new Point(0.883337, 0.0504473),
        new Point(0.841844, 0.0282689),
        new Point(0.75, 0.00999999),
        new Point(0.703178, 0.0146115),
        new Point(0.658156, 0.0282689),
        new Point(0.616663, 0.0504473),
        new Point(0.580294, 0.0802944),
        new Point(0.550447, 0.116663),
        new Point(0.528269, 0.158156),
        new Point(0.51, 0.25),
        new Point(0.514612, 0.296822),
        new Point(0.528269, 0.341844),
        new Point(0.550447, 0.383337),
        new Point(0.580294, 0.419706),
        new Point(0.616663, 0.449553),
        new Point(0.658156, 0.471731),

        }),

                    TriangleIndices = new Int32Collection(new List<int>()
        {
        0, 1, 2, 9, 10, 0, 0, 11, 12, 3, 5, 0, 5, 7, 0, 7, 9, 0, 0, 12, 14, 14, 16, 0, 16, 1, 0, 3, 0, 2, 7, 8, 9, 5, 6, 7, 3, 4, 5, 31, 3, 28, 2, 28, 3, 30, 31, 28, 27, 28, 2, 28, 29, 30, 26, 27, 2, 2, 25, 26, 23, 24, 2, 1, 19, 2, 19, 21, 2, 21, 23, 2, 21, 22, 23, 19, 20, 21, 1, 18, 19, 16, 17, 1, 14, 15, 16, 12, 13, 14,
        }),
                }
            };
            GeometryModel3D meshId1Geometry = new GeometryModel3D()
            {
                Material = new DiffuseMaterial(new LinearGradientBrush(new GradientStopCollection() { new GradientStop(Colors.Gray, 0), new GradientStop(Colors.Black, .5) }) { EndPoint = new Point(0,1) }),
                Geometry = new MeshGeometry3D()
                {

                    Positions = new Point3DCollection(new List<Point3D>()
        {
        new Point3D(0, 1, -1),
        new Point3D(0, 0, 1),
        new Point3D(0.19509, 0.980785, -1),
        new Point3D(0.19509, 0.980785, -1),
        new Point3D(0, 0, 1),
        new Point3D(0.382683, 0.92388, -1),
        new Point3D(0.382683, 0.92388, -1),
        new Point3D(0, 0, 1),
        new Point3D(0.55557, 0.83147, -1),
        new Point3D(0.55557, 0.83147, -1),
        new Point3D(0, 0, 1),
        new Point3D(0.707107, 0.707107, -1),
        new Point3D(0.707107, 0.707107, -1),
        new Point3D(0, 0, 1),
        new Point3D(0.83147, 0.55557, -1),
        new Point3D(0.83147, 0.55557, -1),
        new Point3D(0, 0, 1),
        new Point3D(0.92388, 0.382683, -1),
        new Point3D(0.92388, 0.382683, -1),
        new Point3D(0, 0, 1),
        new Point3D(0.980785, 0.19509, -1),
        new Point3D(0.980785, 0.19509, -1),
        new Point3D(0, 0, 1),
        new Point3D(1, 0, -1),
        new Point3D(1, 0, -1),
        new Point3D(0, 0, 1),
        new Point3D(0.980785, -0.19509, -1),
        new Point3D(0.980785, -0.19509, -1),
        new Point3D(0, 0, 1),
        new Point3D(0.92388, -0.382683, -1),
        new Point3D(0.92388, -0.382683, -1),
        new Point3D(0, 0, 1),
        new Point3D(0.83147, -0.55557, -1),
        new Point3D(0.83147, -0.55557, -1),
        new Point3D(0, 0, 1),
        new Point3D(0.707107, -0.707107, -1),
        new Point3D(0.707107, -0.707107, -1),
        new Point3D(0, 0, 1),
        new Point3D(0.55557, -0.83147, -1),
        new Point3D(0.55557, -0.83147, -1),
        new Point3D(0, 0, 1),
        new Point3D(0.382683, -0.92388, -1),
        new Point3D(0.382683, -0.92388, -1),
        new Point3D(0, 0, 1),
        new Point3D(0.19509, -0.980785, -1),
        new Point3D(0.19509, -0.980785, -1),
        new Point3D(0, 0, 1),
        new Point3D(0, -1, -1),
        new Point3D(0, -1, -1),
        new Point3D(0, 0, 1),
        new Point3D(-0.19509, -0.980785, -1),
        new Point3D(-0.19509, -0.980785, -1),
        new Point3D(0, 0, 1),
        new Point3D(-0.382683, -0.92388, -1),
        new Point3D(-0.382683, -0.92388, -1),
        new Point3D(0, 0, 1),
        new Point3D(-0.55557, -0.831469, -1),
        new Point3D(-0.55557, -0.831469, -1),
        new Point3D(0, 0, 1),
        new Point3D(-0.707107, -0.707107, -1),
        new Point3D(-0.707107, -0.707107, -1),
        new Point3D(0, 0, 1),
        new Point3D(-0.831469, -0.55557, -1),
        new Point3D(-0.831469, -0.55557, -1),
        new Point3D(0, 0, 1),
        new Point3D(-0.92388, -0.382684, -1),
        new Point3D(-0.92388, -0.382684, -1),
        new Point3D(0, 0, 1),
        new Point3D(-0.980785, -0.19509, -1),
        new Point3D(-0.980785, -0.19509, -1),
        new Point3D(0, 0, 1),
        new Point3D(-1, 0, -1),
        new Point3D(-1, 0, -1),
        new Point3D(0, 0, 1),
        new Point3D(-0.980785, 0.19509, -1),
        new Point3D(-0.980785, 0.19509, -1),
        new Point3D(0, 0, 1),
        new Point3D(-0.92388, 0.382684, -1),
        new Point3D(-0.92388, 0.382684, -1),
        new Point3D(0, 0, 1),
        new Point3D(-0.83147, 0.55557, -1),
        new Point3D(-0.83147, 0.55557, -1),
        new Point3D(0, 0, 1),
        new Point3D(-0.707107, 0.707107, -1),
        new Point3D(-0.707107, 0.707107, -1),
        new Point3D(0, 0, 1),
        new Point3D(-0.55557, 0.83147, -1),
        new Point3D(-0.55557, 0.83147, -1),
        new Point3D(0, 0, 1),
        new Point3D(-0.382683, 0.92388, -1),
        new Point3D(-0.382683, 0.92388, -1),
        new Point3D(0, 0, 1),
        new Point3D(-0.19509, 0.980785, -1),
        new Point3D(-0.19509, 0.980785, -1),
        new Point3D(0, 0, 1),
        new Point3D(0, 1, -1),

        }),

                    Normals = new Vector3DCollection(new List<Vector3D>()
        {
        new Vector3D(0.0877537, 0.890977, 0.445488),
        new Vector3D(0.0877537, 0.890977, 0.445488),
        new Vector3D(0.0877537, 0.890977, 0.445488),
        new Vector3D(0.259888, 0.856737, 0.445488),
        new Vector3D(0.259888, 0.856737, 0.445488),
        new Vector3D(0.259888, 0.856737, 0.445488),
        new Vector3D(0.422036, 0.789573, 0.445489),
        new Vector3D(0.422036, 0.789573, 0.445489),
        new Vector3D(0.422036, 0.789573, 0.445489),
        new Vector3D(0.567965, 0.692067, 0.445488),
        new Vector3D(0.567965, 0.692067, 0.445488),
        new Vector3D(0.567965, 0.692067, 0.445488),
        new Vector3D(0.692067, 0.567965, 0.445488),
        new Vector3D(0.692067, 0.567965, 0.445488),
        new Vector3D(0.692067, 0.567965, 0.445488),
        new Vector3D(0.789573, 0.422036, 0.445488),
        new Vector3D(0.789573, 0.422036, 0.445488),
        new Vector3D(0.789573, 0.422036, 0.445488),
        new Vector3D(0.856737, 0.259888, 0.445488),
        new Vector3D(0.856737, 0.259888, 0.445488),
        new Vector3D(0.856737, 0.259888, 0.445488),
        new Vector3D(0.890977, 0.0877537, 0.445488),
        new Vector3D(0.890977, 0.0877537, 0.445488),
        new Vector3D(0.890977, 0.0877537, 0.445488),
        new Vector3D(0.890977, -0.0877537, 0.445488),
        new Vector3D(0.890977, -0.0877537, 0.445488),
        new Vector3D(0.890977, -0.0877537, 0.445488),
        new Vector3D(0.856737, -0.259888, 0.445488),
        new Vector3D(0.856737, -0.259888, 0.445488),
        new Vector3D(0.856737, -0.259888, 0.445488),
        new Vector3D(0.789573, -0.422036, 0.445488),
        new Vector3D(0.789573, -0.422036, 0.445488),
        new Vector3D(0.789573, -0.422036, 0.445488),
        new Vector3D(0.692067, -0.567965, 0.445488),
        new Vector3D(0.692067, -0.567965, 0.445488),
        new Vector3D(0.692067, -0.567965, 0.445488),
        new Vector3D(0.567965, -0.692067, 0.445488),
        new Vector3D(0.567965, -0.692067, 0.445488),
        new Vector3D(0.567965, -0.692067, 0.445488),
        new Vector3D(0.422036, -0.789573, 0.445488),
        new Vector3D(0.422036, -0.789573, 0.445488),
        new Vector3D(0.422036, -0.789573, 0.445488),
        new Vector3D(0.259888, -0.856737, 0.445488),
        new Vector3D(0.259888, -0.856737, 0.445488),
        new Vector3D(0.259888, -0.856737, 0.445488),
        new Vector3D(0.0877533, -0.890977, 0.445488),
        new Vector3D(0.0877533, -0.890977, 0.445488),
        new Vector3D(0.0877533, -0.890977, 0.445488),
        new Vector3D(-0.0877535, -0.890977, 0.445488),
        new Vector3D(-0.0877535, -0.890977, 0.445488),
        new Vector3D(-0.0877535, -0.890977, 0.445488),
        new Vector3D(-0.259888, -0.856737, 0.445488),
        new Vector3D(-0.259888, -0.856737, 0.445488),
        new Vector3D(-0.259888, -0.856737, 0.445488),
        new Vector3D(-0.422036, -0.789573, 0.445488),
        new Vector3D(-0.422036, -0.789573, 0.445488),
        new Vector3D(-0.422036, -0.789573, 0.445488),
        new Vector3D(-0.567965, -0.692067, 0.445488),
        new Vector3D(-0.567965, -0.692067, 0.445488),
        new Vector3D(-0.567965, -0.692067, 0.445488),
        new Vector3D(-0.692067, -0.567965, 0.445488),
        new Vector3D(-0.692067, -0.567965, 0.445488),
        new Vector3D(-0.692067, -0.567965, 0.445488),
        new Vector3D(-0.789573, -0.422036, 0.445488),
        new Vector3D(-0.789573, -0.422036, 0.445488),
        new Vector3D(-0.789573, -0.422036, 0.445488),
        new Vector3D(-0.856737, -0.259888, 0.445488),
        new Vector3D(-0.856737, -0.259888, 0.445488),
        new Vector3D(-0.856737, -0.259888, 0.445488),
        new Vector3D(-0.890977, -0.0877537, 0.445488),
        new Vector3D(-0.890977, -0.0877537, 0.445488),
        new Vector3D(-0.890977, -0.0877537, 0.445488),
        new Vector3D(-0.890977, 0.0877537, 0.445488),
        new Vector3D(-0.890977, 0.0877537, 0.445488),
        new Vector3D(-0.890977, 0.0877537, 0.445488),
        new Vector3D(-0.856737, 0.259888, 0.445488),
        new Vector3D(-0.856737, 0.259888, 0.445488),
        new Vector3D(-0.856737, 0.259888, 0.445488),
        new Vector3D(-0.789573, 0.422036, 0.445488),
        new Vector3D(-0.789573, 0.422036, 0.445488),
        new Vector3D(-0.789573, 0.422036, 0.445488),
        new Vector3D(-0.692067, 0.567964, 0.445488),
        new Vector3D(-0.692067, 0.567964, 0.445488),
        new Vector3D(-0.692067, 0.567964, 0.445488),
        new Vector3D(-0.567965, 0.692067, 0.445489),
        new Vector3D(-0.567965, 0.692067, 0.445489),
        new Vector3D(-0.567965, 0.692067, 0.445489),
        new Vector3D(-0.422036, 0.789573, 0.445488),
        new Vector3D(-0.422036, 0.789573, 0.445488),
        new Vector3D(-0.422036, 0.789573, 0.445488),
        new Vector3D(-0.259888, 0.856737, 0.445488),
        new Vector3D(-0.259888, 0.856737, 0.445488),
        new Vector3D(-0.259888, 0.856737, 0.445488),
        new Vector3D(-0.0877534, 0.890977, 0.445488),
        new Vector3D(-0.0877534, 0.890977, 0.445488),
        new Vector3D(-0.0877534, 0.890977, 0.445488),

        }),

                    TextureCoordinates = new PointCollection(new List<Point>()
        {
        new Point(0.25, 0.49),
        new Point(0.25, 0.25),
        new Point(0.296822, 0.485388),
        new Point(0.296822, 0.485388),
        new Point(0.25, 0.25),
        new Point(0.341844, 0.471731),
        new Point(0.341844, 0.471731),
        new Point(0.25, 0.25),
        new Point(0.383337, 0.449553),
        new Point(0.383337, 0.449553),
        new Point(0.25, 0.25),
        new Point(0.419706, 0.419706),
        new Point(0.419706, 0.419706),
        new Point(0.25, 0.25),
        new Point(0.449553, 0.383337),
        new Point(0.449553, 0.383337),
        new Point(0.25, 0.25),
        new Point(0.471731, 0.341844),
        new Point(0.471731, 0.341844),
        new Point(0.25, 0.25),
        new Point(0.485388, 0.296822),
        new Point(0.485388, 0.296822),
        new Point(0.25, 0.25),
        new Point(0.49, 0.25),
        new Point(0.49, 0.25),
        new Point(0.25, 0.25),
        new Point(0.485388, 0.203178),
        new Point(0.485388, 0.203178),
        new Point(0.25, 0.25),
        new Point(0.471731, 0.158156),
        new Point(0.471731, 0.158156),
        new Point(0.25, 0.25),
        new Point(0.449553, 0.116663),
        new Point(0.449553, 0.116663),
        new Point(0.25, 0.25),
        new Point(0.419706, 0.0802944),
        new Point(0.419706, 0.0802944),
        new Point(0.25, 0.25),
        new Point(0.383337, 0.0504473),
        new Point(0.383337, 0.0504473),
        new Point(0.25, 0.25),
        new Point(0.341844, 0.0282689),
        new Point(0.341844, 0.0282689),
        new Point(0.25, 0.25),
        new Point(0.296822, 0.0146115),
        new Point(0.296822, 0.0146115),
        new Point(0.25, 0.25),
        new Point(0.25, 0.00999999),
        new Point(0.25, 0.00999999),
        new Point(0.25, 0.25),
        new Point(0.203178, 0.0146115),
        new Point(0.203178, 0.0146115),
        new Point(0.25, 0.25),
        new Point(0.158156, 0.0282689),
        new Point(0.158156, 0.0282689),
        new Point(0.25, 0.25),
        new Point(0.116663, 0.0504473),
        new Point(0.116663, 0.0504473),
        new Point(0.25, 0.25),
        new Point(0.0802944, 0.0802944),
        new Point(0.0802944, 0.0802944),
        new Point(0.25, 0.25),
        new Point(0.0504473, 0.116663),
        new Point(0.0504473, 0.116663),
        new Point(0.25, 0.25),
        new Point(0.0282689, 0.158156),
        new Point(0.0282689, 0.158156),
        new Point(0.25, 0.25),
        new Point(0.0146115, 0.203178),
        new Point(0.0146115, 0.203178),
        new Point(0.25, 0.25),
        new Point(0.00999999, 0.25),
        new Point(0.00999999, 0.25),
        new Point(0.25, 0.25),
        new Point(0.0146115, 0.296822),
        new Point(0.0146115, 0.296822),
        new Point(0.25, 0.25),
        new Point(0.0282689, 0.341844),
        new Point(0.0282689, 0.341844),
        new Point(0.25, 0.25),
        new Point(0.0504472, 0.383337),
        new Point(0.0504472, 0.383337),
        new Point(0.25, 0.25),
        new Point(0.0802943, 0.419706),
        new Point(0.0802943, 0.419706),
        new Point(0.25, 0.25),
        new Point(0.116663, 0.449553),
        new Point(0.116663, 0.449553),
        new Point(0.25, 0.25),
        new Point(0.158156, 0.471731),
        new Point(0.158156, 0.471731),
        new Point(0.25, 0.25),
        new Point(0.203178, 0.485388),
        new Point(0.203178, 0.485388),
        new Point(0.25, 0.25),
        new Point(0.25, 0.49),

        }),

                    TriangleIndices = new Int32Collection(new List<int>()
        {
        0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36, 37, 38, 39, 40, 41, 42, 43, 44, 45, 46, 47, 48, 49, 50, 51, 52, 53, 54, 55, 56, 57, 58, 59, 60, 61, 62, 63, 64, 65, 66, 67, 68, 69, 70, 71, 72, 73, 74, 75, 76, 77, 78, 79, 80, 81, 82, 83, 84, 85, 86, 87, 88, 89, 90, 91, 92, 93, 94, 95,
        }),
                }
            };

            model3DGroup.Children.Add(new AmbientLight());
            model3DGroup.Children.Add(meshId0Geometry);
            model3DGroup.Children.Add(meshId1Geometry);

            return model3DGroup;
        }

        public Model3DGroup GenerateRgbCubeModel3DGroup()
        {
            Model3DGroup model3DGroup = new();
            model3DGroup.Transform = new Transform3DGroup()
            {
                Children = {
                            new ScaleTransform3D(new Vector3D(-.25, -.25, -.25)),
                            new RotateTransform3D()
                            {
                                Rotation = new AxisAngleRotation3D(new Vector3D(-1, 0, 1), -90)
                            }
                        }
            };

            MaterialGroup materialGroup1 = new MaterialGroup();
            materialGroup1.Children.Add(faceBrushDiffuseMaterialDesaturated1);
            materialGroup1.Children.Add(faceBrushDiffuseMaterial1);
            GeometryModel3D face1Geometry = new()
            {
                Material = materialGroup1,
                Geometry = new MeshGeometry3D()
                {
                    Positions = new Point3DCollection(new List<Point3D>()
                            {
                                new Point3D(1, -1, 1),
                                new Point3D(1, 1, 1),
                                new Point3D(-1, 1, 1),
                                new Point3D(-1, -1, 1),
                            }),

                    Normals = new Vector3DCollection(new List<Vector3D>()
                            {
                                new Vector3D(0, 0, 1),
                                new Vector3D(0, 0, 1),
                                new Vector3D(0, 0, 1),
                                new Vector3D(0, 0, 1),
                            }),

                    TextureCoordinates = new PointCollection(new List<Point>()
                            {
                                new Point(0.375, 0.25),
                                new Point(0.625, 0.25),
                                new Point(0.625, 0.5),
                                new Point(0.375, 0.5),
                            }),

                    TriangleIndices = new Int32Collection(new List<int>()
                            {
                                0, 1, 2, 0, 2, 3,
                            }),
                }
            };
            MaterialGroup materialGroup2 = new MaterialGroup();
            materialGroup2.Children.Add(faceBrushDiffuseMaterialDesaturated2);
            materialGroup2.Children.Add(faceBrushDiffuseMaterial2);
            GeometryModel3D face2Geometry = new()
            {
                Material = materialGroup2,
                Geometry = new MeshGeometry3D()
                {
                    Positions = new Point3DCollection(new List<Point3D>()
                            {
                                new Point3D(-1, -1, 1),
                                new Point3D(-1, 1, 1),
                                new Point3D(-1, 1, -1),
                                new Point3D(-1, -1, -1),
                            }),

                    Normals = new Vector3DCollection(new List<Vector3D>()
                            {
                                new Vector3D(-1, 0, 0),
                                new Vector3D(-1, 0, 0),
                                new Vector3D(-1, 0, 0),
                                new Vector3D(-1, 0, 0),
                            }),

                    TextureCoordinates = new PointCollection(new List<Point>()
                            {
                                new Point(0.375, 0.5),
                                new Point(0.625, 0.5),
                                new Point(0.625, 0.75),
                                new Point(0.375, 0.75),
                            }),

                    TriangleIndices = new Int32Collection(new List<int>()
                            {
                                0, 1, 2, 0, 2, 3,
                            }),
                }
            };
            MaterialGroup materialGroup3 = new MaterialGroup();
            materialGroup3.Children.Add(faceBrushDiffuseMaterialDesaturated3);
            materialGroup3.Children.Add(faceBrushDiffuseMaterial3);
            GeometryModel3D face3Geometry = new()
            {
                Material = materialGroup3,
                Geometry = new MeshGeometry3D()
                {
                    Positions = new Point3DCollection(new List<Point3D>()
                            {
                                new Point3D(1, -1, -1),
                                new Point3D(1, 1, -1),
                                new Point3D(1, 1, 1),
                                new Point3D(1, -1, 1),
                            }),

                    Normals = new Vector3DCollection(new List<Vector3D>()
                            {
                                new Vector3D(1, 0, 0),
                                new Vector3D(1, 0, 0),
                                new Vector3D(1, 0, 0),
                                new Vector3D(1, 0, 0),
                            }),

                    TextureCoordinates = new PointCollection(new List<Point>()
                            {
                                new Point(0.375, 0),
                                new Point(0.625, 0),
                                new Point(0.625, 0.25),
                                new Point(0.375, 0.25),
                            }),

                    TriangleIndices = new Int32Collection(new List<int>()
                            {
                                0, 1, 2, 0, 2, 3,
                            }),
                }
            };
            MaterialGroup materialGroup4 = new MaterialGroup();
            materialGroup4.Children.Add(faceBrushDiffuseMaterialDesaturated4);
            materialGroup4.Children.Add(faceBrushDiffuseMaterial4);
            GeometryModel3D face4Geometry = new()
            {
                Material = materialGroup4,
                Geometry = new MeshGeometry3D()
                {
                    Positions = new Point3DCollection(new List<Point3D>()
                            {
                                new Point3D(1, -1, 1),
                                new Point3D(-1, -1, 1),
                                new Point3D(-1, -1, -1),
                                new Point3D(1, -1, -1),
                            }),

                    Normals = new Vector3DCollection(new List<Vector3D>()
                            {
                                new Vector3D(0, -1, 0),
                                new Vector3D(0, -1, 0),
                                new Vector3D(0, -1, 0),
                                new Vector3D(0, -1, 0),
                            }),

                    TextureCoordinates = new PointCollection(new List<Point>()
                            {
                                new Point(0.125, 0.5),
                                new Point(0.375, 0.5),
                                new Point(0.375, 0.75),
                                new Point(0.125, 0.75),
                            }),

                    TriangleIndices = new Int32Collection(new List<int>()
                            {
                                0, 1, 2, 0, 2, 3,
                            }),
                }
            };
            MaterialGroup materialGroup5 = new MaterialGroup();
            materialGroup5.Children.Add(faceBrushDiffuseMaterialDesaturated5);
            materialGroup5.Children.Add(faceBrushDiffuseMaterial5);
            GeometryModel3D face5Geometry = new()
            {
                Material = materialGroup5,
                Geometry = new MeshGeometry3D()
                {
                    Positions = new Point3DCollection(new List<Point3D>()
                            {
                                new Point3D(-1, -1, -1),
                                new Point3D(-1, 1, -1),
                                new Point3D(1, 1, -1),
                                new Point3D(1, -1, -1),
                            }),

                    Normals = new Vector3DCollection(new List<Vector3D>()
                            {
                                new Vector3D(0, 0, -1),
                                new Vector3D(0, 0, -1),
                                new Vector3D(0, 0, -1),
                                new Vector3D(0, 0, -1),
                            }),

                    TextureCoordinates = new PointCollection(new List<Point>()
                            {
                                new Point(0.375, 0.75),
                                new Point(0.625, 0.75),
                                new Point(0.625, 1),
                                new Point(0.375, 1),
                            }),

                    TriangleIndices = new Int32Collection(new List<int>()
                            {
                                0, 1, 2, 0, 2, 3,
                            }),
                }
            };

            MaterialGroup materialGroup6 = new MaterialGroup();
            materialGroup6.Children.Add(faceBrushDiffuseMaterialDesaturated6);
            materialGroup6.Children.Add(faceBrushDiffuseMaterial6);
            GeometryModel3D face6Geometry = new()
            {
                Material = materialGroup6,
                Geometry = new MeshGeometry3D()
                {
                    Positions = new Point3DCollection(new List<Point3D>()
                            {
                                new Point3D(-1, 1, 1),
                                new Point3D(1, 1, 1),
                                new Point3D(1, 1, -1),
                                new Point3D(-1, 1, -1),
                            }),

                    Normals = new Vector3DCollection(new List<Vector3D>()
                            {
                                new Vector3D(0, 1, 0),
                                new Vector3D(0, 1, 0),
                                new Vector3D(0, 1, 0),
                                new Vector3D(0, 1, 0),
                            }),

                    TextureCoordinates = new PointCollection(new List<Point>()
                            {
                                new Point(0.625, 0.5),
                                new Point(0.875, 0.5),
                                new Point(0.875, 0.75),
                                new Point(0.625, 0.75),
                            }),

                    TriangleIndices = new Int32Collection(new List<int>()
                            {
                                0, 1, 2, 0, 2, 3,
                            }),

                }
            };

            model3DGroup.Children.Add(new AmbientLight());

            model3DGroup.Children.Add(face1Geometry);
            model3DGroup.Children.Add(face2Geometry);
            model3DGroup.Children.Add(face3Geometry);
            model3DGroup.Children.Add(face4Geometry);
            model3DGroup.Children.Add(face5Geometry);
            model3DGroup.Children.Add(face6Geometry);

            return model3DGroup;
        }

        Point hsl3dDisplayMousePoint = new Point();
        bool hsl3dMouseInteraction = false;

        private void Hsl3dDisplayDecorator_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Released)
                return;

            hsl3dMouseInteraction = false;
        }


        private void Hsl3dDisplayDecorator_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed || Hsl3dDisplayDecorator == null || ColorModel != ColorModel.HSL)
                return;

            hslComponentAreaInteraction = false;
            hsl3dMouseInteraction = true;

            hsl3dDisplayMousePoint = e.GetPosition(Hsl3dDisplayDecorator);
        }

        private void Hsl3dDisplayDecorator_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (Hsl3dDisplayDecorator == null || e.Delta == 0 || ColorModel != ColorModel.HSL)
                return;
            var polarity = (e.Delta > 0) ? 1 : -1;
            var change = Math.Clamp(S + (polarity * (e.Delta / e.Delta) / 50.0), HSL_MIN, SL_MAX);
            if (S != change)
            {
                S = change;
            }
        }

        private void Hsl3dDisplayDecorator_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed || Hsl3dDisplayDecorator == null || hslComponentAreaInteraction == true || ColorModel != ColorModel.HSL)
                return;

            Point newPoint = e.GetPosition(Hsl3dDisplayDecorator);

            // Method 1: recalculate H and V values based solely on point coordinates, disregarding pre-existing values:
            //var calcH = Math.Clamp((H_MAX - (((newPoint.Y) / Hsl3dDisplayDecorator.ActualHeight) * H_MAX)), HSL_MIN, H_MAX);
            //var calcV = Math.Clamp((newPoint.X / Hsl3dDisplayDecorator.ActualWidth), HSL_MIN, SL_MAX);

            // Method 2: recalculate H and V values based on point coordinates and pre-existing values:
            var xChange = (hsl3dDisplayMousePoint.X - newPoint.X) / Hsl3dDisplayDecorator.ActualWidth;
            var yChange = hsl3dDisplayMousePoint.Y - newPoint.Y;

            //Debug.WriteLine($"Change: {xChange} x {yChange}\nPoint: {newPoint.X} x {newPoint.Y}\nOldPoint: {hsl3dDisplayMousePoint.X} x {hsl3dDisplayMousePoint.Y}");

            hsl3dDisplayMousePoint = newPoint;

            bool skipH = false;
            bool skipV = false;

            if (H == H_MAX && yChange >= 0 || H == HSL_MIN && yChange <= 0)
                skipH = true;

            if (V == SL_MAX && xChange <= 0 || V == HSL_MIN && xChange >= 0)
                skipV = true;

            if (!skipH)
            {
                var calcH = Math.Clamp(H + ((yChange / Hsl3dDisplayDecorator.ActualHeight) * H_MAX), HSL_MIN, H_MAX);
                if (H != calcH)
                    H = calcH;
            }

            if (!skipV)
            {
                var calcV = Math.Clamp(V - xChange, HSL_MIN, SL_MAX);
                if (V != calcV)
                    V = calcV;
            }
        }

        private Selector? hslComponentSelector;
        protected Selector? HslComponentSelector
        {
            get { return hslComponentSelector; }

            set
            {
                if (hslComponentSelector != null)
                {
                    BindingOperations.ClearBinding(hslComponentSelector, ItemsControl.ItemsSourceProperty);
                    hslComponentSelector.SelectionChanged -= new SelectionChangedEventHandler(HslComponentSelector_SelectionChanged);
                }
                hslComponentSelector = value;

                if (hslComponentSelector != null)
                {
                    Binding binding = new(nameof(HslComponentList)) { Mode = BindingMode.OneWay, Source = this };
                    hslComponentSelector.SetBinding(ItemsControl.ItemsSourceProperty, binding);
                    hslComponentSelector.SelectionChanged += new SelectionChangedEventHandler(HslComponentSelector_SelectionChanged);
                }
            }
        }

        private Selector? colorModelSelector;
        protected Selector? ColorModelSelector
        {
            get { return colorModelSelector; }

            set
            {
                if (colorModelSelector != null)
                {
                    BindingOperations.ClearBinding(colorModelSelector, ItemsControl.ItemsSourceProperty);
                    colorModelSelector.SelectionChanged -= new SelectionChangedEventHandler(ColorModelSelector_SelectionChanged);
                }
                colorModelSelector = value;

                if (colorModelSelector != null)
                {
                    Binding binding = new(nameof(ColorModelList)) { Mode = BindingMode.OneWay, Source = this };
                    colorModelSelector.SetBinding(ItemsControl.ItemsSourceProperty, binding);
                    colorModelSelector.SelectionChanged += new SelectionChangedEventHandler(ColorModelSelector_SelectionChanged);
                }
            }
        }

        private void ColorModelSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems == null)
                return;

            if (e.AddedItems.Count == 0)
                return;

            var selected = e.AddedItems[0] as ColorModel?;
            if (selected is not null)
            {
                ColorModel = selected.Value;
            }
        }

        private void HslComponentSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems == null)
                return;

            if (e.AddedItems.Count == 0)
                return;

            var selected = e.AddedItems[0] as HslComponent?;
            if (selected is not null)
            {
                HslComponentSelection = selected.Value;
            }
        }

        private void ADecrementButtonBase_Click(object sender, RoutedEventArgs e)
        {
            A = Math.Clamp(A - ARGB_ROC, Byte.MinValue, Byte.MaxValue);
        }

        private void RDecrementButtonBase_Click(object sender, RoutedEventArgs e)
        {
            R = Math.Clamp(R - ARGB_ROC, Byte.MinValue, Byte.MaxValue);
        }

        private void GDecrementButtonBase_Click(object sender, RoutedEventArgs e)
        {
            G = Math.Clamp(G - ARGB_ROC, Byte.MinValue, Byte.MaxValue);
        }

        private void BDecrementButtonBase_Click(object sender, RoutedEventArgs e)
        {
            B = Math.Clamp(B - ARGB_ROC, Byte.MinValue, Byte.MaxValue);
        }

        private void HDecrementButtonBase_Click(object sender, RoutedEventArgs e)
        {
            H = Math.Clamp(Math.Round(H - H_ROC), HSL_MIN, H_MAX);
        }

        private void SDecrementButtonBase_Click(object sender, RoutedEventArgs e)
        {
            S = Math.Clamp(Math.Round(S - SL_ROC, 2), HSL_MIN, SL_MAX);
        }

        private void VDecrementButtonBase_Click(object sender, RoutedEventArgs e)
        {
            V = Math.Clamp(Math.Round(V - SL_ROC, 2), HSL_MIN, SL_MAX);
        }

        private void AIncrementButtonBase_Click(object sender, RoutedEventArgs e)
        {
            A = Math.Clamp(A + ARGB_ROC, Byte.MinValue, Byte.MaxValue);
        }

        private void RIncrementButtonBase_Click(object sender, RoutedEventArgs e)
        {
            R = Math.Clamp(R + ARGB_ROC, Byte.MinValue, Byte.MaxValue);
        }

        private void GIncrementButtonBase_Click(object sender, RoutedEventArgs e)
        {
            G = Math.Clamp(G + ARGB_ROC, Byte.MinValue, Byte.MaxValue);
        }

        private void BIncrementButtonBase_Click(object sender, RoutedEventArgs e)
        {
            B = Math.Clamp(B + ARGB_ROC, Byte.MinValue, Byte.MaxValue);
        }

        private void HIncrementButtonBase_Click(object sender, RoutedEventArgs e)
        {
            H = Math.Clamp(Math.Round(H + H_ROC), HSL_MIN, H_MAX);        }

        private void SIncrementButtonBase_Click(object sender, RoutedEventArgs e)
        {
            S = Math.Clamp(Math.Round(S + SL_ROC, 2), HSL_MIN, SL_MAX);
        }

        private void VIncrementButtonBase_Click(object sender, RoutedEventArgs e)
        {
            V = Math.Clamp(Math.Round(V + SL_ROC, 2), HSL_MIN, SL_MAX);
        }

        private Selector? presetColorsSelector;
        protected Selector? PresetColorsSelector
        {
            get { return presetColorsSelector; }

            set
            {
                if (presetColorsSelector != null)
                {
                    BindingOperations.ClearBinding(presetColorsSelector, ItemsControl.ItemsSourceProperty);
                    presetColorsSelector.SelectionChanged -= new SelectionChangedEventHandler(ColorsSelector_SelectionChanged);
                }
                presetColorsSelector = value;

                if (presetColorsSelector != null)
                {
                    Binding binding = new(nameof(PresetColors)) { Mode = BindingMode.OneWay, Source = this };
                    presetColorsSelector.SetBinding(ItemsControl.ItemsSourceProperty, binding);
                    presetColorsSelector.SelectionChanged += new SelectionChangedEventHandler(ColorsSelector_SelectionChanged);
                }
            }
        }

        private Selector? customColorsSelector;
        protected Selector? CustomColorsSelector
        {
            get { return customColorsSelector; }

            set
            {
                if (customColorsSelector != null)
                {
                    BindingOperations.ClearBinding(customColorsSelector, ItemsControl.ItemsSourceProperty);
                    customColorsSelector.SelectionChanged -= new SelectionChangedEventHandler(ColorsSelector_SelectionChanged);
                }
                customColorsSelector = value;

                if (customColorsSelector != null)
                {
                    Binding binding = new(nameof(CustomColors)) { Mode = BindingMode.OneWay, Source = this };
                    customColorsSelector.SetBinding(ItemsControl.ItemsSourceProperty, binding);
                    customColorsSelector.SelectionChanged += new SelectionChangedEventHandler(ColorsSelector_SelectionChanged);
                }
            }
        }

        readonly GradientStop hslComponentAreaHueLowBoundGraientStop = new() { Color = Colors.Gray, Offset = 0 };
        readonly GradientStop hslComponentAreaHueHighBoundGradientStop = new() { Offset = 1 };
        readonly LinearGradientBrush hslComponentAreaSaturationGradientBrush = new() { EndPoint = new(1,0)};
        readonly LinearGradientBrush hslComponentAreaLightnessGradientBrush = new() { EndPoint = new(1, 0) };
        readonly LinearGradientBrush hslComponentAreaValueGradientBrush = new() { EndPoint = new(1, 0) };
        readonly LinearGradientBrush hslComponentAreaLightnessRelativeSaturationOverlay = new() 
        {
            EndPoint = new Point(0, 1),
            GradientStops = new GradientStopCollection()
            {
                new GradientStop(Colors.Gray, 1),
                new GradientStop((Color)ColorConverter.ConvertFromString("#00000000"), 0)
            }
        };
        readonly LinearGradientBrush hslComponentAreaLightnessRelativeValueOverlay = new()
        {
            EndPoint = new Point(0, 1),
            GradientStops = new GradientStopCollection()
            {
                new GradientStop(Colors.White, 1),
                new GradientStop((Color)ColorConverter.ConvertFromString("#00000000"), 0)
            }
        };
        readonly SolidColorBrush hslComponentAreaLightnessWhiteBackground = new() { Color = Colors.White };

        Grid hslComponentAreaXaxisValueGrid = new() {  HorizontalAlignment = HorizontalAlignment.Left };
        Grid hslComponentAreaYaxisValueGrid = new() {  VerticalAlignment = VerticalAlignment.Top };
        Border hslComponentAreaXaxisBoundGuide = MakeHslComponentGridXaxisGuide();
        Border hslComponentAreaYaxisBoundGuide = MakeHslComponentGridYaxisGuide();
        GradientStopCollection RgbSpectrumGraidentStops = new()
        {
            new GradientStop((Color)ColorConverter.ConvertFromString("#FF0000"), 0),
            new GradientStop((Color)ColorConverter.ConvertFromString("#FFFF00"), 1.0 / 6),
            new GradientStop((Color)ColorConverter.ConvertFromString("#00FF00"), (1.0 / 6) * 2),
            new GradientStop((Color)ColorConverter.ConvertFromString("#00FFFF"), (1.0 / 6) * 3),
            new GradientStop((Color)ColorConverter.ConvertFromString("#0000FF"), (1.0 / 6) * 4),
            new GradientStop((Color)ColorConverter.ConvertFromString("#FF00FF"), (1.0 / 6) * 5),
            new GradientStop((Color)ColorConverter.ConvertFromString("#FF0000"), 1),
        };
        private Panel? hslComponentArea;
        protected Panel? HslComponentArea
        {
            get { return hslComponentArea; }

            set
            {
                if (hslComponentArea != null)
                {
                    BindingOperations.ClearBinding(hslComponentAreaHueHighBoundGradientStop, GradientStop.ColorProperty);
                    BindingOperations.ClearBinding(hslComponentAreaSaturationGradientBrush, LinearGradientBrush.OpacityProperty);
                    BindingOperations.ClearBinding(hslComponentAreaLightnessGradientBrush, LinearGradientBrush.OpacityProperty);
                    BindingOperations.ClearBinding(hslComponentAreaValueGradientBrush, LinearGradientBrush.OpacityProperty);
                    BindingOperations.ClearBinding(hslComponentAreaLightnessRelativeSaturationOverlay, LinearGradientBrush.OpacityProperty);
                    BindingOperations.ClearBinding(hslComponentAreaLightnessRelativeValueOverlay, LinearGradientBrush.OpacityProperty);
                    BindingOperations.ClearBinding(hslComponentAreaLightnessWhiteBackground, LinearGradientBrush.OpacityProperty);
                    hslComponentArea.PreviewMouseLeftButtonDown -= new MouseButtonEventHandler(HslComponentArea_PreviewMouseLeftButtonDown);
                    hslComponentArea.PreviewMouseMove -= new MouseEventHandler(HslComponentArea_PreviewMouseMove);
                    hslComponentArea.PreviewMouseUp -= new MouseButtonEventHandler(HslComponentArea_PreviewMouseUp);
                    hslComponentArea.SizeChanged -= new SizeChangedEventHandler(HslComponentArea_SizeChanged);
                }
                hslComponentArea = value;

                if (hslComponentArea != null)
                {
                    hslComponentArea.PreviewMouseMove += new MouseEventHandler(HslComponentArea_PreviewMouseMove);
                    hslComponentArea.PreviewMouseLeftButtonDown += new MouseButtonEventHandler(HslComponentArea_PreviewMouseLeftButtonDown);
                    hslComponentArea.PreviewMouseUp += new MouseButtonEventHandler(HslComponentArea_PreviewMouseUp);
                    hslComponentArea.SizeChanged += new SizeChangedEventHandler(HslComponentArea_SizeChanged);

                    Binding highBoundBinding = new(nameof(CurrentHueColor)) { Mode = BindingMode.OneWay, Source = this };
                    BindingOperations.SetBinding(hslComponentAreaHueHighBoundGradientStop, GradientStop.ColorProperty, highBoundBinding);

                    hslComponentAreaSaturationGradientBrush.GradientStops = RgbSpectrumGraidentStops;
                    Binding saturationOpacityBinding = new(nameof(S)) { Mode = BindingMode.OneWay, Source = this };
                    BindingOperations.SetBinding(hslComponentAreaSaturationGradientBrush, LinearGradientBrush.OpacityProperty, saturationOpacityBinding);

                    hslComponentAreaLightnessGradientBrush.GradientStops = RgbSpectrumGraidentStops;
                    Binding lightnessOpacityBindinig = new(nameof(V)) { Mode = BindingMode.OneWay, Source = this, Converter = new DoubleToReflectedAbsoluteValueDouble() };
                    BindingOperations.SetBinding(hslComponentAreaLightnessGradientBrush, LinearGradientBrush.OpacityProperty, lightnessOpacityBindinig);

                    hslComponentAreaValueGradientBrush.GradientStops = RgbSpectrumGraidentStops;
                    Binding valueOpacityBindinig = new(nameof(V)) { Mode = BindingMode.OneWay, Source = this };
                    BindingOperations.SetBinding(hslComponentAreaValueGradientBrush, LinearGradientBrush.OpacityProperty, valueOpacityBindinig);

                    Binding valueOpacityBindinig2 = new(nameof(V)) { Mode = BindingMode.OneWay, Source = this };
                    BindingOperations.SetBinding(hslComponentAreaLightnessRelativeValueOverlay, LinearGradientBrush.OpacityProperty, valueOpacityBindinig2);

                    Binding lightnessOpacityBindinig2 = new(nameof(V)) { Mode = BindingMode.OneWay, Source = this, Converter = new DoubleToReflectedAbsoluteValueDouble() };
                    BindingOperations.SetBinding(hslComponentAreaLightnessRelativeSaturationOverlay, LinearGradientBrush.OpacityProperty, lightnessOpacityBindinig2);

                    Binding lightnessOpacityBindinig3 = new(nameof(V)) { Mode = BindingMode.OneWay, Source = this, Converter = new DoubleToBooleanSwitchDouble() };
                    BindingOperations.SetBinding(hslComponentAreaLightnessWhiteBackground, SolidColorBrush.OpacityProperty, lightnessOpacityBindinig3);

                    GenerateHslComponentAreaContainer(HslComponentSelection);
                }
            }
        }



        private void HslComponentArea_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            GenerateHslComponentAreaContainer(HslComponentSelection);
        }

        public void SetGuideBindings(HslComponent hslComponentSelection)
        {
            BindingOperations.ClearBinding(hslComponentAreaXaxisBoundGuide, Border.HeightProperty);
            BindingOperations.ClearBinding(hslComponentAreaYaxisBoundGuide, Border.WidthProperty);
            BindingOperations.ClearBinding(hslComponentAreaXaxisValueGrid, Panel.MarginProperty);
            BindingOperations.ClearBinding(hslComponentAreaYaxisValueGrid, Panel.MarginProperty);

            switch (hslComponentSelection)
            {
                case HslComponent.Hue:
                    Binding xAxisValueBinding = new(nameof(S)) { Mode = BindingMode.OneWay, Source = this, Converter = new DoubleToLengthMultipliedDouble(), ConverterParameter = HslComponentArea?.ActualWidth };
                    BindingOperations.SetBinding(hslComponentAreaYaxisBoundGuide, Border.WidthProperty, xAxisValueBinding);

                    Binding xAxisMarginBinding = new(nameof(S)) { Mode = BindingMode.OneWay, Source = this, Converter = new DoubleToHslGuideLeftMargin(), ConverterParameter = HslComponentArea?.ActualWidth };
                    BindingOperations.SetBinding(hslComponentAreaXaxisValueGrid, Panel.MarginProperty, xAxisMarginBinding);

                    Binding yAxisValueBinding = new(nameof(V)) { Mode = BindingMode.OneWay, Source = this, Converter = new DoubleToLengthMultipliedInvertedDouble(), ConverterParameter = HslComponentArea?.ActualHeight };
                    BindingOperations.SetBinding(hslComponentAreaXaxisBoundGuide, Border.HeightProperty, yAxisValueBinding);

                    Binding yAxisMarginBinding = new(nameof(V)) { Mode = BindingMode.OneWay, Source = this, Converter = new DoubleToHslGuideTopMargin(), ConverterParameter = HslComponentArea?.ActualHeight };
                    BindingOperations.SetBinding(hslComponentAreaYaxisValueGrid, Panel.MarginProperty, yAxisMarginBinding);
                    break;
                case HslComponent.Saturation:
                    xAxisValueBinding = new(nameof(H)) { Mode = BindingMode.OneWay, Source = this, Converter = new DoubleToLengthMultipliedDoubleHue(), ConverterParameter = HslComponentArea?.ActualWidth };
                    BindingOperations.SetBinding(hslComponentAreaYaxisBoundGuide, Border.WidthProperty, xAxisValueBinding);

                    xAxisMarginBinding = new(nameof(H)) { Mode = BindingMode.OneWay, Source = this, Converter = new DoubleToHslGuideLeftMarginHue(), ConverterParameter = HslComponentArea?.ActualWidth };
                    BindingOperations.SetBinding(hslComponentAreaXaxisValueGrid, Panel.MarginProperty, xAxisMarginBinding);

                    yAxisValueBinding = new(nameof(V)) { Mode = BindingMode.OneWay, Source = this, Converter = new DoubleToLengthMultipliedInvertedDouble(), ConverterParameter = HslComponentArea?.ActualHeight };
                    BindingOperations.SetBinding(hslComponentAreaXaxisBoundGuide, Border.HeightProperty, yAxisValueBinding);

                    yAxisMarginBinding = new(nameof(V)) { Mode = BindingMode.OneWay, Source = this, Converter = new DoubleToHslGuideTopMargin(), ConverterParameter = HslComponentArea?.ActualHeight };
                    BindingOperations.SetBinding(hslComponentAreaYaxisValueGrid, Panel.MarginProperty, yAxisMarginBinding);
                    break;
                case HslComponent.Lightness:
                case HslComponent.Value:
                    xAxisValueBinding = new(nameof(H)) { Mode = BindingMode.OneWay, Source = this, Converter = new DoubleToLengthMultipliedDoubleHue(), ConverterParameter = HslComponentArea?.ActualWidth };
                    BindingOperations.SetBinding(hslComponentAreaYaxisBoundGuide, Border.WidthProperty, xAxisValueBinding);

                    xAxisMarginBinding = new(nameof(H)) { Mode = BindingMode.OneWay, Source = this, Converter = new DoubleToHslGuideLeftMarginHue(), ConverterParameter = HslComponentArea?.ActualWidth };
                    BindingOperations.SetBinding(hslComponentAreaXaxisValueGrid, Panel.MarginProperty, xAxisMarginBinding);

                    yAxisValueBinding = new(nameof(S)) { Mode = BindingMode.OneWay, Source = this, Converter = new DoubleToLengthMultipliedInvertedDouble(), ConverterParameter = HslComponentArea?.ActualHeight };
                    BindingOperations.SetBinding(hslComponentAreaXaxisBoundGuide, Border.HeightProperty, yAxisValueBinding);

                    yAxisMarginBinding = new(nameof(S)) { Mode = BindingMode.OneWay, Source = this, Converter = new DoubleToHslGuideTopMargin(), ConverterParameter = HslComponentArea?.ActualHeight };
                    BindingOperations.SetBinding(hslComponentAreaYaxisValueGrid, Panel.MarginProperty, yAxisMarginBinding);
                    break;
                default:
                    break;
            }
        }

        public static Border MakeHslComponentGridMidPointGuide()
        {
            return new Border()
            {
                Width = 3,
                Height = 3,
                Background = new SolidColorBrush(Colors.Transparent)
            };
        }

        public static Border MakeHslComponentGridXaxisGuide()
        {
            return new Border()
            {
                Width = 3,
                BorderThickness = new Thickness(1, 0, 1, 0),
                Background = new SolidColorBrush(Colors.Black),
                BorderBrush = new SolidColorBrush(Colors.White)
            };
        }

        public static Border MakeHslComponentGridYaxisGuide()
        {
            return new Border()
            {
                Height = 3,
                BorderThickness = new Thickness(0, 1, 0, 1),
                Background = new SolidColorBrush(Colors.Black),
                BorderBrush = new SolidColorBrush(Colors.White)
            };
        }

        public void GenerateHslComponentAreaContainerGuides()
        {
            hslComponentAreaXaxisValueGrid = new Grid() { HorizontalAlignment = HorizontalAlignment.Left };
            hslComponentAreaYaxisValueGrid = new Grid() { VerticalAlignment = VerticalAlignment.Top };
            hslComponentAreaXaxisBoundGuide = MakeHslComponentGridXaxisGuide();
            hslComponentAreaYaxisBoundGuide = MakeHslComponentGridYaxisGuide();

            hslComponentAreaXaxisValueGrid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(0, GridUnitType.Auto) });
            hslComponentAreaXaxisValueGrid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(0, GridUnitType.Auto) });
            hslComponentAreaXaxisValueGrid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Star) });

            hslComponentAreaYaxisValueGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(0, GridUnitType.Auto) });
            hslComponentAreaYaxisValueGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(0, GridUnitType.Auto) });
            hslComponentAreaYaxisValueGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });

            Border midGuideX = MakeHslComponentGridMidPointGuide();
            Border midGuideY = MakeHslComponentGridMidPointGuide();
            var guide2X = MakeHslComponentGridXaxisGuide();
            var guide2Y = MakeHslComponentGridYaxisGuide();

            Grid.SetRow(hslComponentAreaXaxisBoundGuide, 0);
            Grid.SetRow(midGuideX, 1);
            Grid.SetRow(guide2X, 2);

            Grid.SetColumn(hslComponentAreaYaxisBoundGuide, 0);
            Grid.SetColumn(midGuideY, 1);
            Grid.SetColumn(guide2Y, 2);

            hslComponentAreaXaxisValueGrid.Children.Add(hslComponentAreaXaxisBoundGuide);
            hslComponentAreaXaxisValueGrid.Children.Add(midGuideX);
            hslComponentAreaXaxisValueGrid.Children.Add(guide2X);

            hslComponentAreaYaxisValueGrid.Children.Add(hslComponentAreaYaxisBoundGuide);
            hslComponentAreaYaxisValueGrid.Children.Add(midGuideY);
            hslComponentAreaYaxisValueGrid.Children.Add(guide2Y);

            HslComponentArea?.Children.Add(hslComponentAreaXaxisValueGrid);
            HslComponentArea?.Children.Add(hslComponentAreaYaxisValueGrid);

            SetGuideBindings(HslComponentSelection);
        }

        public void GenerateHslComponentAreaHslHue()
        {
            if (HslComponentArea is null)
                return;

            hslComponentAreaHueLowBoundGraientStop.Color = Colors.Gray;
            LinearGradientBrush background = new()
            {
                GradientStops = new GradientStopCollection()
                        {
                            hslComponentAreaHueLowBoundGraientStop,
                            hslComponentAreaHueHighBoundGradientStop
                        },
                EndPoint = new Point(1, 0)
            };
            HslComponentArea.Background = background;

            HslComponentArea.Children.Add(new Border()
            {
                Background = new LinearGradientBrush()
                {
                    GradientStops = new GradientStopCollection()
                            {
                                new GradientStop(Colors.Black, 1),
                                new GradientStop((Color)ColorConverter.ConvertFromString("#00000000"), 0.5),
                                new GradientStop(Colors.White, 0)
                            },
                    EndPoint = new Point(0, 1)
                }
            });
        }

        public void GenerateHslComponentAreaHslSaturation()
        {
            if (HslComponentArea is null)
                return;

            HslComponentArea.Background = new LinearGradientBrush()
            {
                GradientStops = new GradientStopCollection()
                            {
                                new GradientStop(Colors.Black, 1),
                                new GradientStop(Colors.White, 0)
                            },
                EndPoint = new Point(0, 1)
            };

            Border child2 = new()
            {
                Background = new LinearGradientBrush()
                {
                    GradientStops = new GradientStopCollection()
                            {
                                new GradientStop(Colors.Black, 1),
                                new GradientStop((Color)ColorConverter.ConvertFromString("#00000000"), 0.5),
                                new GradientStop(Colors.White, 0)
                            },
                    EndPoint = new Point(0, 1)
                }
            };

            Border child1 = new()
            {
                Background = hslComponentAreaSaturationGradientBrush,
                Child = child2
            };

            HslComponentArea.Children.Add(child1);
        }

        public void GenerateHslComponentAreaHslLightness()
        {
            if (HslComponentArea is null)
                return;

            HslComponentArea.Background = new SolidColorBrush(Colors.Black);

            Border child4 = new()
            {
                Background = hslComponentAreaLightnessGradientBrush,
                Child = new Border() { Background = hslComponentAreaLightnessRelativeSaturationOverlay }
            };

            Border child3 = new()
            {
                Background = hslComponentAreaLightnessWhiteBackground,
                Child = child4
            };

            HslComponentArea.Children.Add(child3);
        }

        public void GenerateHslComponentAreaHsvHue()
        {
            if (HslComponentArea is null)
                return;

            hslComponentAreaHueLowBoundGraientStop.Color = Colors.White;
            LinearGradientBrush background = new()
            {
                GradientStops = new GradientStopCollection()
                        {
                            hslComponentAreaHueLowBoundGraientStop,
                            hslComponentAreaHueHighBoundGradientStop
                        },
                EndPoint = new Point(1, 0)
            };
            HslComponentArea.Background = background;

            HslComponentArea.Children.Add(new Border()
            {
                Background = new LinearGradientBrush()
                {
                    GradientStops = new GradientStopCollection()
                            {
                                new GradientStop(Colors.Black, 1),
                                new GradientStop((Color)ColorConverter.ConvertFromString("#00000000"), 0)
                            },
                    EndPoint = new Point(0, 1)
                }
            });
        }

        public void GenerateHslComponentAreaHsvSaturation()
        {
            if (HslComponentArea is null)
                return;

            HslComponentArea.Background = new SolidColorBrush(Colors.White);

            Border child2 = new()
            {
                Background = new LinearGradientBrush()
                {
                    GradientStops = new GradientStopCollection()
                                {
                                    new GradientStop(Colors.Black, 1),
                                    new GradientStop((Color)ColorConverter.ConvertFromString("#00000000"), 0)
                                },
                    EndPoint = new Point(0, 1)
                },
            };

            Border child1 = new()
            {
                Background = hslComponentAreaSaturationGradientBrush,
                Child = child2
            };

            HslComponentArea.Children.Add(child1);
        }

        public void GenerateHslComponentAreaHsvValue()
        {
            if (HslComponentArea is null)
                return;

            HslComponentArea.Background = new SolidColorBrush(Colors.Black);

            Border child4 = new()
            {
                Background = hslComponentAreaValueGradientBrush,
                Child = new Border() { Background = hslComponentAreaLightnessRelativeValueOverlay }
            };

            //Border child3 = new()
            //{
            //    Background = hslComponentAreaLightnessWhiteBackground,
            //    Child = child4
            //};

            HslComponentArea.Children.Add(child4);
        }

        /// <summary>
        /// The HSL component area is an interactive display that visualizes the relationship between
        /// the color components of Hue, Saturation, and Lightness. Three modes of operation, one dedicated
        /// to each color mode, govern changes in the component area display and interaction. Depending on the
        /// mode of operation, two of the color components are treated as dependant variables and the remaining
        /// component as an independent variable; the X-axis and Y-axis of the display depict the dependant
        /// variables, while a slider allows the user to adjust the independent variable. Moving lines for each
        /// axis depict the coordinate on the HSL component area that corresponds to the current color.
        /// 
        /// When Hue is the active HslComponentSelection, the HSL component area is composed of 
        /// overlays that depict, relative to the current hue, Saturation on the X-axis and lightness 
        /// on the Y-axis. The background is set to a linear gradient composed of a low-bound stop 
        /// depicting zero saturation (gray) and a high-bound stop depicting the current hue at 
        /// maximum saturation. An overlay is set on the component area to show lightness relative to the Y-axis.
        /// 
        ///  When Saturation is the active HslComponentSelection, the HSL component area is composed of 
        /// overlays that depict, relative to the current saturation, hue on the X-axis and lightness 
        /// on the Y-axis. The background is set to a linear gradient depicting the visible spectrum.
        /// An overlay is set on the component area to show lightness relative to the Y-axis.
        /// 
        ///  When Lightness is the active HslComponentSelection, the HSL component area is composed of 
        /// overlays that depict, relative to the current saturation, hue on the X-axis and saturation 
        /// on the Y-axis. The background is set to a linear gradient depicting the visible spectrum.
        /// An overlay is set on the component area to show saturation relative to the Y-axis.
        /// 
        /// </summary>
        /// <param name="selection">The HslComponentSelection to govern the state of the component area display.</param>
        /// <seealso cref="ProcessHslComponentAreaMouseInput"/>
        public void GenerateHslComponentAreaContainer(HslComponent selection)
        {
            if (HslComponentArea is null)
                return;

            HslComponentArea.Children.Clear();
            
            switch (ColorModel)
            {
                case ColorModel.HSL:
                    switch (selection)
                    {
                        case HslComponent.Hue:
                            GenerateHslComponentAreaHslHue();
                            break;
                        case HslComponent.Saturation:
                            GenerateHslComponentAreaHslSaturation();
                            break;
                        case HslComponent.Lightness:
                            GenerateHslComponentAreaHslLightness();
                            break;
                        default:
                            break;
                    }
                    break;
                case ColorModel.HSV:
                    switch (selection)
                    {
                        case HslComponent.Hue:
                            GenerateHslComponentAreaHsvHue();
                            break;
                        case HslComponent.Saturation:
                            GenerateHslComponentAreaHsvSaturation();
                            break;
                        case HslComponent.Value:
                            GenerateHslComponentAreaHsvValue();
                            break;
                        default:
                            break;
                    }
                    break;
            }

            GenerateHslComponentAreaContainerGuides();
        }

        bool hslComponentAreaInteraction = false;

        private void HslComponentArea_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState != MouseButtonState.Released)
                return;

            hslComponentAreaInteraction = false;
        }

        /// <summary>
        /// When the HSL component area is clicked on, process the click coordinates and
        /// set the relevant color component properties.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <seealso cref="ProcessHslComponentAreaMouseInput"/>
        private void HslComponentArea_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState != MouseButtonState.Pressed)
                return;

            hsl3dMouseInteraction = false;
            hslComponentAreaInteraction = true;
            ProcessHslComponentAreaMouseInput(e.GetPosition(HslComponentArea));
        }

        /// <summary>
        /// When the HSL component area has mouse movement while the mouse button is depressed,
        /// process the mouse location coordinates and set the relevant color component properties
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <seealso cref="ProcessHslComponentAreaMouseInput"/>
        private void HslComponentArea_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed || hsl3dMouseInteraction == true)
                return;

            ProcessHslComponentAreaMouseInput(e.GetPosition(HslComponentArea));
        }

        /// <summary>
        /// Process a point as two color component values, depending on the current
        /// HslComponentSelection value.
        /// </summary>
        /// <param name="point"></param>
        /// <seealso cref="GenerateHslComponentAreaContainer"/>
        public void ProcessHslComponentAreaMouseInput(Point point)
        {
            if (HslComponentArea is null)
                return;

            switch (HslComponentSelection)
            {
                case HslComponent.Hue:
                    S = Math.Clamp((point.X) / HslComponentArea.ActualWidth, HSL_MIN, SL_MAX);
                    V = Math.Clamp(1.0 - (point.Y) / HslComponentArea.ActualHeight, HSL_MIN, SL_MAX);
                    break;
                case HslComponent.Saturation:
                    H = Math.Clamp(((point.X) / HslComponentArea.ActualWidth) * 360.0, HSL_MIN, H_MAX);
                    V = Math.Clamp(1.0 - (point.Y) / HslComponentArea.ActualHeight, HSL_MIN, SL_MAX);
                    break;
                case HslComponent.Lightness:
                case HslComponent.Value:
                    H = Math.Clamp(((point.X) / HslComponentArea.ActualWidth) * 360.0, HSL_MIN, H_MAX);
                    S = Math.Clamp(1.0 - (point.Y) / HslComponentArea.ActualHeight, HSL_MIN, SL_MAX);
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// Used to force update the underlying data source of a TextBox's Text binding if the Enter key is pressed
        /// while the TextBox is focused.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter)
                return;

            TextBox tb = (TextBox)sender;
            if (tb == null)
                return;

            BindingExpression bindingExpression = tb.GetBindingExpression(TextBox.TextProperty);
            bindingExpression.UpdateSource();
        }

        public Color GetColorFromRawColor()
        {
            return Color.FromArgb(ToByte(CurrentColor.A), ToByte(CurrentColor.R), ToByte(CurrentColor.G), ToByte(CurrentColor.B));
        }

        /// <summary>
        /// When a preset or custom color is selected, set the selection as the currently
        /// selected color and raise the color-selection event.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void ColorsSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count < 1)
                return;

            var selected = e.AddedItems[0] as Color?;
            if (selected is not null)
            {
                var color = GetColorFromRawColor();
                if (color == selected.Value)
                    return;

                RefreshCurrentColor(new RawColor(selected.Value.A, selected.Value.R, selected.Value.G, selected.Value.B));
                SelectedColor = color;
                RaiseColorSelectedEvent();
            }
        }

        /// <summary>
        /// Saves the current color as a new custom color.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SaveCustomColorButtonBase_Click(object sender, RoutedEventArgs e)
        {
            if (CustomColors.Count >= 10)
                CustomColors.RemoveAt(CustomColors.Count - 1);

            CustomColors.Insert(0, GetColorFromRawColor());

            RaiseCustomColorSavedEvent();
        }

        /// <summary>
        /// Sets the current color as the currently selected color.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SelectCustomColorButtonBase_Click(object sender, RoutedEventArgs e)
        {
            SelectedColor = GetColorFromRawColor();
            RaiseColorSelectedEvent();
        }

        public override void OnApplyTemplate()
        {
            SelectCustomColorButtonBase = GetTemplateChild(nameof(TemplatePart.PART_selectCustomColorButtonBase)) as ButtonBase;
            SaveCustomColorButtonBase = GetTemplateChild(nameof(TemplatePart.PART_saveCustomColorButtonBase)) as ButtonBase;

            AIncrementButtonBase = GetTemplateChild(nameof(TemplatePart.PART_aIncrementButtonBase)) as ButtonBase;
            RIncrementButtonBase = GetTemplateChild(nameof(TemplatePart.PART_rIncrementButtonBase)) as ButtonBase;
            GIncrementButtonBase = GetTemplateChild(nameof(TemplatePart.PART_gIncrementButtonBase)) as ButtonBase;
            BIncrementButtonBase = GetTemplateChild(nameof(TemplatePart.PART_bIncrementButtonBase)) as ButtonBase;
            HIncrementButtonBase = GetTemplateChild(nameof(TemplatePart.PART_hIncrementButtonBase)) as ButtonBase;
            SIncrementButtonBase = GetTemplateChild(nameof(TemplatePart.PART_sIncrementButtonBase)) as ButtonBase;
            VIncrementButtonBase = GetTemplateChild(nameof(TemplatePart.PART_vIncrementButtonBase)) as ButtonBase;

            ADecrementButtonBase = GetTemplateChild(nameof(TemplatePart.PART_aDecrementButtonBase)) as ButtonBase;
            RDecrementButtonBase = GetTemplateChild(nameof(TemplatePart.PART_rDecrementButtonBase)) as ButtonBase;
            GDecrementButtonBase = GetTemplateChild(nameof(TemplatePart.PART_gDecrementButtonBase)) as ButtonBase;
            BDecrementButtonBase = GetTemplateChild(nameof(TemplatePart.PART_bDecrementButtonBase)) as ButtonBase;
            HDecrementButtonBase = GetTemplateChild(nameof(TemplatePart.PART_hDecrementButtonBase)) as ButtonBase;
            SDecrementButtonBase = GetTemplateChild(nameof(TemplatePart.PART_sDecrementButtonBase)) as ButtonBase;
            VDecrementButtonBase = GetTemplateChild(nameof(TemplatePart.PART_vDecrementButtonBase)) as ButtonBase;

            HexTextBox = GetTemplateChild(nameof(TemplatePart.PART_hexTextBox)) as TextBox;
            ATextBox = GetTemplateChild(nameof(TemplatePart.PART_aTextBox)) as TextBox;
            RTextBox = GetTemplateChild(nameof(TemplatePart.PART_rTextBox)) as TextBox;
            GTextBox = GetTemplateChild(nameof(TemplatePart.PART_gTextBox)) as TextBox;
            BTextBox = GetTemplateChild(nameof(TemplatePart.PART_bTextBox)) as TextBox;
            HTextBox = GetTemplateChild(nameof(TemplatePart.PART_hTextBox)) as TextBox;
            STextBox = GetTemplateChild(nameof(TemplatePart.PART_sTextBox)) as TextBox;
            VTextBox = GetTemplateChild(nameof(TemplatePart.PART_vTextBox)) as TextBox;

            ARangeBase = GetTemplateChild(nameof(TemplatePart.PART_aRangeBase)) as RangeBase;
            RRangeBase = GetTemplateChild(nameof(TemplatePart.PART_rRangeBase)) as RangeBase;
            GRangeBase = GetTemplateChild(nameof(TemplatePart.PART_gRangeBase)) as RangeBase;
            BRangeBase = GetTemplateChild(nameof(TemplatePart.PART_bRangeBase)) as RangeBase;
            HRangeBase = GetTemplateChild(nameof(TemplatePart.PART_hRangeBase)) as RangeBase;
            SRangeBase = GetTemplateChild(nameof(TemplatePart.PART_sRangeBase)) as RangeBase;
            VRangeBase = GetTemplateChild(nameof(TemplatePart.PART_vRangeBase)) as RangeBase;
            HslRangeBase = GetTemplateChild(nameof(TemplatePart.PART_hslRangeBase)) as RangeBase;

            PresetColorsSelector = GetTemplateChild(nameof(TemplatePart.PART_presetColorsSelector)) as Selector;
            CustomColorsSelector = GetTemplateChild(nameof(TemplatePart.PART_customColorsSelector)) as Selector;
            HslComponentSelector = GetTemplateChild(nameof(TemplatePart.PART_hslComponentSelector)) as Selector;
            ColorModelSelector = GetTemplateChild(nameof(TemplatePart.PART_colorModelSelector)) as Selector;

            HslComponentArea = GetTemplateChild(nameof(TemplatePart.PART_hslComponentAreaPanel)) as Panel;

            Hsl3dDisplayDecorator = GetTemplateChild(nameof(TemplatePart.PART_hsl3dDisplayDecorator)) as Decorator;

            MenuOpenButtonBase = GetTemplateChild(nameof(TemplatePart.PART_menuOpenButton)) as ButtonBase;
            MenuCloseButtonBase = GetTemplateChild(nameof(TemplatePart.PART_menuCloseButton)) as ButtonBase;
            CloseMenuDecorator = GetTemplateChild(nameof(TemplatePart.PART_closeMenuDecorator)) as Decorator;

            ColorModelsVisibilityToggleButton = GetTemplateChild(nameof(TemplatePart.PART_colorModelsVisibilityToggleButton)) as ToggleButton;
            PresetColorsVisibilityToggleButton = GetTemplateChild(nameof(TemplatePart.PART_presetColorsVisibilityToggleButton)) as ToggleButton;
            Display2dVisibilityToggleButton = GetTemplateChild(nameof(TemplatePart.PART_display2dVisibilityToggleButton)) as ToggleButton;
            Display3dVisibilityToggleButton = GetTemplateChild(nameof(TemplatePart.PART_display3dVisibilityToggleButton)) as ToggleButton;
            ComponentsVisibilityToggleButton = GetTemplateChild(nameof(TemplatePart.PART_componentsVisibilityToggleButton)) as ToggleButton;
            ColorPreviewVisibilityToggleButton = GetTemplateChild(nameof(TemplatePart.PART_colorPreviewVisibilityToggleButton)) as ToggleButton;
            CustomColorsVisibilityToggleButton = GetTemplateChild(nameof(TemplatePart.PART_customColorsVisibilityToggleButton)) as ToggleButton;

            HexadecimalComponentVisibilityToggleButton = GetTemplateChild(nameof(TemplatePart.PART_hexadecimalComponentVisibilityToggleButton)) as ToggleButton;
            AlphaComponentVisibilityToggleButton = GetTemplateChild(nameof(TemplatePart.PART_alphaComponentVisibilityToggleButton)) as ToggleButton;
            RgbComponentVisibilityToggleButton = GetTemplateChild(nameof(TemplatePart.PART_rgbComponentVisibilityToggleButton)) as ToggleButton;
            HslvComponentVisibilityToggleButton = GetTemplateChild(nameof(TemplatePart.PART_hslvComponentVisibilityToggleButton)) as ToggleButton;

            HslComponentList = new ObservableCollection<HslComponent>((HslComponent[])Enum.GetValues(typeof(HslComponent)));
            RebuildColorModelList();
            RefreshRangeBaseVisuals();
            ProcessHslComponentSelection(HslComponentSelection);

            // Setting ApplicationCommands.Paste on Control to enable Clipboard paste into the color selector:
            this.CommandBindings.Remove(new CommandBinding(ApplicationCommands.Paste, new ExecutedRoutedEventHandler(PasteHandler)));
            this.CommandBindings.Add(new CommandBinding(ApplicationCommands.Paste, new ExecutedRoutedEventHandler(PasteHandler)));
        }

        /// <summary>
        /// Support for recieving pasted data and attempting to parse it as a hexadecimal string.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void PasteHandler(object sender, ExecutedRoutedEventArgs e)
        {
            if (Clipboard.ContainsData(DataFormats.Text) || Clipboard.ContainsData(DataFormats.UnicodeText))
            {
                var data = Clipboard.GetDataObject();
                var formatted = data.GetData(nameof(DataFormats.UnicodeText), true);
                if (formatted == null)
                    return;

                var validator = new ArgbHexadecimalColorStringValidationRule();
                var result = validator.Validate(formatted, CultureInfo.CurrentCulture);
                if (result.IsValid)
                {
                    var stringFormatted = System.Convert.ToString(formatted);
                    if (stringFormatted != null)
                        HexValueString = stringFormatted;
                }
            }
        }

        public static byte ToByte(double value)
        {
            return Convert.ToByte(value);
            //return (byte)value;
        }

        /// <summary>
        /// Converts HSL color components to corresponding RGB values.
        /// 
        /// Algorithms: https://www.niwa.nu/2013/05/math-behind-colorspace-conversions-rgb-hsl/
        /// https://www.baeldung.com/cs/convert-color-hsl-rgb
        /// </summary>
        /// <param name="h"></param>
        /// <param name="s"></param>
        /// <param name="l"></param>
        /// <returns></returns>
        public static List<double> HslToRgb(double h, double s, double l)
        {
            var chroma = (1 - Math.Abs(2 * l - 1)) * s;
            var hueSector = h / 60;
            var x = chroma * (1 - Math.Abs((hueSector % 2) - 1));
            var m = l - (chroma * 0.5);

            var v1 = (chroma + m) * 255;
            var v2 = (x + m) * 255;
            var v3 = (0 + m) * 255;

            if (0 <= hueSector && hueSector <= 1)
                return new List<double>() { v1, v2, v3 };
            else if (1 <= hueSector && hueSector <= 2)
                return new List<double>() { v2, v1, v3 };
            else if (2 <= hueSector && hueSector <= 3)
                return new List<double>() { v3, v1, v2 };
            else if (3 <= hueSector && hueSector <= 4)
                return new List<double>() { v3, v2, v1 };
            else if (4 <= hueSector && hueSector <= 5)
                return new List<double>() { v2, v3, v1 };
            else if (5 <= hueSector && hueSector <= 6)
                return new List<double>() { v1, v3, v2 };

            return new List<double>() { 0, 0, 0 };
        }

        /// <summary>
        /// Converts RGB color components to corresponding HSL values.
        /// 
        /// Algorithms: https://www.niwa.nu/2013/05/math-behind-colorspace-conversions-rgb-hsl/
        /// https://stackoverflow.com/a/39147465
        /// </summary>
        /// <param name="r"></param>
        /// <param name="g"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static List<double> RgbToHsl(double r, double g, double b)
        {
            r /= Byte.MaxValue;
            g /= Byte.MaxValue;
            b /= Byte.MaxValue;

            var min = Math.Min(Math.Min(r, g), b);
            var max = Math.Max(Math.Max(r, g), b);

            var lightness = (max + min) / 2;

            var chroma = max - min;

            var saturation = (lightness <= 0.5) ?
                chroma / (max + min) : chroma / (2 - max - min);

            var hue = 0.0;

            if (r == max)
            {
                var segment = (g - b) / chroma;
                var shift = 0 / 60;
                if (segment < 0)
                {
                    shift = 360 / 60;
                }
                hue = segment + shift;
            }
            else if (g == max)
            {
                var segment = (b - r) / chroma;
                var shift = 120 / 60;
                hue = segment + shift;
            }
            else if (b == max)
            {
                var segment = (r - g) / chroma;
                var shift = 240 / 60;
                hue = segment + shift;
            }

            var finalHue = hue * 60;

            return new List<double>(3) { finalHue, saturation, lightness };
        }

        public static List<double> RgbToHsv(double r, double g, double b)
        {
            double max = Math.Max(r, Math.Max(g, b));
            double min = Math.Min(r, Math.Min(g, b));

            double hue = System.Drawing.Color.FromArgb(255, ToByte(r), ToByte(g), ToByte(b)).GetHue();
            double saturation = (max == 0) ? 0 : 1d - (1d * min / max);
            double value = max / 255d;

            return new List<double>(3) { hue, saturation, value };
        }

        public static List<double> HsvToRgb(double hue, double saturation, double value)
        {
            int hi = Convert.ToInt32(Math.Floor(hue / 60)) % 6;
            double f = hue / 60 - Math.Floor(hue / 60);

            value *= 255;
            var v = value;
            var p = value * (1 - saturation);
            var q = value * (1 - f * saturation);
            var t = value * (1 - (1 - f) * saturation);

            if (hi == 0)
                return new List<double>(3) { v, t, p };
            else if (hi == 1)
                return new List<double>(3) { q, v, p };
            else if (hi == 2)
                return new List<double>(3) { p, v, t };
            else if (hi == 3)
                return new List<double>(3) { p, q, v };
            else if (hi == 4)
                return new List<double>(3) { t, p, v };
            else
                return new List<double>(3) { v, p, q };
        }

        public Color GetRgbColorFromModel(double d1, double d2, double d3)
        {
            var temp = ModelToRgb(d1, d2, d3);
            return Color.FromArgb(Byte.MaxValue, ToByte(temp[0]), ToByte(temp[1]), ToByte(temp[2]));
        }

        public Color GetRgbColorFromModelSpecifyHue(double hue)
        {
            return GetRgbColorFromModel(hue, S, V);
        }

        public Color GetRgbColorFromModelSpecifySaturation(double saturation)
        {
            return GetRgbColorFromModel(H, saturation, V);
        }

        public Color GetRgbColorFromModelSpecifyLightness(double lightness)
        {
            return GetRgbColorFromModel(H, S, lightness);
        }

        public List<double> ModelToRgb(double d1, double d2, double d3)
        {
            switch (ColorModel)
            {
                case ColorModel.HSL:
                    return HslToRgb(d1, d2, d3);
                case ColorModel.HSV:
                    return HsvToRgb(d1, d2, d3);
                default:
                    break;
            }
            return new List<double>(3) { 0, 0, 0 };
        }

        public List<double> RgbToModel(double d1, double d2, double d3)
        {
            switch (ColorModel)
            {
                case ColorModel.HSL:
                    return RgbToHsl(d1, d2, d3);
                case ColorModel.HSV:
                    return RgbToHsv(d1, d2, d3);
                default:
                    break;
            }
            return new List<double>(3) { 0, 0, 0 };
        }

        /// <summary>
        /// Refreshes the GradientStop Colors in the LinearGradientBrush objects of the RangeBase Controls.
        /// </summary>
        public void RefreshRangeBaseVisuals()
        {
            HueSector0 = GetRgbColorFromModelSpecifyHue(0);
            HueSector1 = GetRgbColorFromModelSpecifyHue(60);
            HueSector2 = GetRgbColorFromModelSpecifyHue(120);
            HueSector3 = GetRgbColorFromModelSpecifyHue(180);
            HueSector4 = GetRgbColorFromModelSpecifyHue(240);
            HueSector5 = GetRgbColorFromModelSpecifyHue(300);

            SColorLowBound = GetRgbColorFromModelSpecifySaturation(0);
            SColorHighBound = GetRgbColorFromModelSpecifySaturation(1);

            VColorLowBound = GetRgbColorFromModelSpecifyLightness(0);
            VColorHighBound = GetRgbColorFromModelSpecifyLightness(1);

            var rgbTemp = ModelToRgb(H, S, V);
            RColorLowBound = Color.FromArgb(Byte.MaxValue, Byte.MinValue, ToByte(rgbTemp[1]), ToByte(rgbTemp[2]));
            RColorHighBound = Color.FromArgb(Byte.MaxValue, Byte.MaxValue, ToByte(rgbTemp[1]), ToByte(rgbTemp[2]));
            GColorLowBound = Color.FromArgb(Byte.MaxValue, ToByte(rgbTemp[0]), Byte.MinValue, ToByte(rgbTemp[2]));
            GColorHighBound = Color.FromArgb(Byte.MaxValue, ToByte(rgbTemp[0]), Byte.MaxValue, ToByte(rgbTemp[2]));
            BColorLowBound = Color.FromArgb(Byte.MaxValue, ToByte(rgbTemp[0]), ToByte(rgbTemp[1]), Byte.MinValue);
            BColorHighBound = Color.FromArgb(Byte.MaxValue, ToByte(rgbTemp[0]), ToByte(rgbTemp[1]), Byte.MaxValue);

            double d3 = 0.0;
            switch (ColorModel)
            {
                case ColorModel.HSL:
                    d3 = 0.5;
                    break;
                case ColorModel.HSV:
                    d3 = 1.0;
                    break;
            }

            var hueTemp = ModelToRgb(H, 1, d3);
            CurrentHueColor = Color.FromArgb(Byte.MaxValue, ToByte(hueTemp[0]), ToByte(hueTemp[1]), ToByte(hueTemp[2]));

            // ProcessHslComponentSelection(this, this.HslComponentSelection);
        }

        /// <summary>
        /// Commits the current state of the ARGB color components to the CurrentColor Property.
        /// </summary>
        public void RefreshCurrentColor(RawColor rawColor)
        {
            if (rawColor is null)
                return;

            CurrentColor = new RawColor( rawColor.A, rawColor.R, rawColor.G, rawColor.B );
            ProcessColorChange("");
            RefreshRangeBaseVisuals();
        }

        public void RefreshCurrentColor(RawColor rawColor, string originatingPropertyName)
        {
            if (rawColor is null)
                return;

            CurrentColor = new RawColor(rawColor.A, rawColor.R, rawColor.G, rawColor.B);
            ProcessColorChange(originatingPropertyName);
            RefreshRangeBaseVisuals();
        }

        public void ProcessColorChange(string originatingPropertyName)
        {
            RawColor c = CurrentColor;
            if (c is null)
                return;

            IgnoreChange = true;

            Byte[] colorBytes = { ToByte(c.A), ToByte(c.R), ToByte(c.G), ToByte(c.B) };

            CurrentMediaColor = Color.FromArgb(colorBytes[0], colorBytes[1], colorBytes[2], colorBytes[3]);

            HexValueString = $"#{colorBytes[0]:X2}{colorBytes[1]:X2}{colorBytes[2]:X2}{colorBytes[3]:X2}";

            if (A != c.A)
                A = c.A;
            if (R != c.R)
                R = c.R;
            if (G != c.G)
                G = c.G;
            if (B != c.B)
                B = c.B;

            System.Drawing.Color d = System.Drawing.Color.FromArgb(colorBytes[0], colorBytes[1], colorBytes[2], colorBytes[3]);

            double hue = d.GetHue();

            if (H != hue && V > HSL_MIN && S > HSL_MIN && V < SL_MAX && nameof(H) != originatingPropertyName)
                H = hue;
            else if (H != hue && H == HSL_MIN && V > HSL_MIN && nameof(H) != originatingPropertyName)
                H = hue;
            else if (H != hue && H == HSL_MIN && V == HSL_MIN && S == HSL_MIN && nameof(H) != originatingPropertyName)
                H = hue;

            double saturation = 0;
            double value = 0;

            switch (ColorModel)
            {
                case ColorModel.HSL:
                    saturation = d.GetSaturation();
                    value = d.GetBrightness();
                    break;
                case ColorModel.HSV:
                    var rgb = RgbToModel(c.R, c.G, c.B);
                    saturation = rgb[1];
                    value = rgb[2];
                    break;
            }

            if (S != saturation && V < SL_MAX && nameof(S) != originatingPropertyName)
                S = saturation;

            if (V != value && nameof(V) != originatingPropertyName)
                V = value;

            // Force correct ARGB values to populate any TextBoxes with validation errors:
            if (ATextBox != null)
                if (Validation.GetHasError(ATextBox))
                    ATextBox.Text = A.ToString();
            if (RTextBox != null)
                if (Validation.GetHasError(RTextBox))
                    RTextBox.Text = R.ToString();
            if (GTextBox != null)
                if (Validation.GetHasError(GTextBox))
                    GTextBox.Text = G.ToString();
            if (BTextBox != null)
                if (Validation.GetHasError(BTextBox))
                    BTextBox.Text = B.ToString();

            IgnoreChange = false;
            RaiseCurrentColorChangedEvent();
        }

        /// <summary>
        /// Event that is raised whenever the user confirms selection of a color.
        /// </summary>
        public static readonly RoutedEvent ColorSelectedEvent = EventManager.RegisterRoutedEvent(
        name: nameof(ColorSelected),
        routingStrategy: RoutingStrategy.Direct,
        handlerType: typeof(RoutedEventHandler),
        ownerType: typeof(ColorSelector));

        public event RoutedEventHandler ColorSelected
        {
            add { AddHandler(ColorSelectedEvent, value); }
            remove { RemoveHandler(ColorSelectedEvent, value); }
        }

        void RaiseColorSelectedEvent()
        {
            RoutedEventArgs routedEventArgs = new(routedEvent: ColorSelectedEvent);
            RaiseEvent(routedEventArgs);
        }

        /// <summary>
        /// Event that is raised whenever the user edits the current color values.
        /// </summary>
        public static readonly RoutedEvent CurrentColorChangedEvent = EventManager.RegisterRoutedEvent(
        name: nameof(CurrentColorChanged),
        routingStrategy: RoutingStrategy.Direct,
        handlerType: typeof(RoutedEventHandler),
        ownerType: typeof(ColorSelector));

        public event RoutedEventHandler CurrentColorChanged
        {
            add { AddHandler(CurrentColorChangedEvent, value); }
            remove { RemoveHandler(CurrentColorChangedEvent, value); }
        }

        void RaiseCurrentColorChangedEvent()
        {
            RoutedEventArgs routedEventArgs = new(routedEvent: CurrentColorChangedEvent);
            RaiseEvent(routedEventArgs);
        }

        /// <summary>
        /// Event that is raised whenever the user adds a custom color to the custom color history.
        /// </summary>
        public static readonly RoutedEvent CustomColorSavedEvent = EventManager.RegisterRoutedEvent(
        name: nameof(CustomColorSaved),
        routingStrategy: RoutingStrategy.Direct,
        handlerType: typeof(RoutedEventHandler),
        ownerType: typeof(ColorSelector));

        public event RoutedEventHandler CustomColorSaved
        {
            add { AddHandler(CustomColorSavedEvent, value); }
            remove { RemoveHandler(CustomColorSavedEvent, value); }
        }

        void RaiseCustomColorSavedEvent()
        {
            RoutedEventArgs routedEventArgs = new(routedEvent: CustomColorSavedEvent);
            RaiseEvent(routedEventArgs);
        }

        public static readonly DependencyProperty HslComponentSelectionProperty =
            DependencyProperty.Register(nameof(HslComponentSelection), typeof(HslComponent), typeof(ColorSelector), new PropertyMetadata(HslComponent.Hue, new PropertyChangedCallback(HslComponentSelectionChangedCallback)));

        public HslComponent HslComponentSelection
        {
            get { return (HslComponent)GetValue(HslComponentSelectionProperty); }
            set { SetValue(HslComponentSelectionProperty, value); }
        }

        private static void HslComponentSelectionChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ColorSelector selector = (ColorSelector)d;

            selector.ProcessHslComponentSelection((HslComponent)e.NewValue);
        }

        public void ProcessHslComponentSelection(HslComponent selection)
        {
            if (HslRangeBase is null)
                return;

            Binding binding;
            switch (selection)
            {
                case HslComponent.Hue:
                    BindingOperations.ClearBinding(HslRangeBase, RangeBase.ValueProperty);
                    HslRangeBase.Minimum = HSL_MIN;
                    HslRangeBase.Maximum = H_MAX;
                    HslRangeBase.SmallChange = H_ROC;
                    HslRangeBase.LargeChange = H_ROC;
                    HslRangeBase.Value = HRangeBaseValue;
                    binding = new Binding(nameof(HRangeBaseValue)) { Mode = BindingMode.TwoWay, Source = this };
                    HslRangeBase.SetBinding(RangeBase.ValueProperty, binding);

                    double d3 = 0.0;
                    switch (ColorModel)
                    {
                        case ColorModel.HSL:
                            d3 = 0.5;
                            break;
                        case ColorModel.HSV:
                            d3 = 1.0;
                            break;
                    }

                    HslRangeBase.Background = new LinearGradientBrush()
                    {
                        GradientStops = new GradientStopCollection()
                        {
                            new GradientStop(GetRgbColorFromModel(0, 1, d3), 0),
                            new GradientStop(GetRgbColorFromModel(60, 1, d3), 1.0 / 6),
                            new GradientStop(GetRgbColorFromModel(120, 1, d3), (1.0 / 6) * 2),
                            new GradientStop(GetRgbColorFromModel(180, 1, d3), (1.0 / 6) * 3),
                            new GradientStop(GetRgbColorFromModel(240, 1, d3), (1.0 / 6) * 4),
                            new GradientStop(GetRgbColorFromModel(300, 1, d3), (1.0 / 6) * 5),
                            new GradientStop(GetRgbColorFromModel(360, 1, d3), 1),
                        },
                        StartPoint = new Point(0, 1),
                        EndPoint = new Point(0, 0)
                    };
                    break;
                case HslComponent.Saturation:
                    BindingOperations.ClearBinding(HslRangeBase, RangeBase.ValueProperty);
                    HslRangeBase.Minimum = HSL_MIN;
                    HslRangeBase.Maximum = SL_MAX;
                    HslRangeBase.SmallChange = SL_ROC;
                    HslRangeBase.LargeChange = SL_ROC;
                    HslRangeBase.Value = SRangeBaseValue;
                    binding = new Binding(nameof(SRangeBaseValue)) { Mode = BindingMode.TwoWay, Source = this };
                    HslRangeBase.SetBinding(RangeBase.ValueProperty, binding);
                    HslRangeBase.Background = new LinearGradientBrush()
                    {
                        GradientStops = new GradientStopCollection()
                        {
                            new GradientStop(GetRgbColorFromModel(0,0,0), 0),
                            new GradientStop(GetRgbColorFromModel(0,0,1), 1),
                        },
                        StartPoint = new Point(0, 1),
                        EndPoint = new Point(0, 0)
                    };
                    break;
                case HslComponent.Lightness:
                case HslComponent.Value:
                    BindingOperations.ClearBinding(HslRangeBase, RangeBase.ValueProperty);
                    HslRangeBase.Minimum = HSL_MIN;
                    HslRangeBase.Maximum = SL_MAX;
                    HslRangeBase.SmallChange = SL_ROC;
                    HslRangeBase.LargeChange = SL_ROC;
                    HslRangeBase.Value = VRangeBaseValue;
                    binding = new Binding(nameof(VRangeBaseValue)) { Mode = BindingMode.TwoWay, Source = this };
                    HslRangeBase.SetBinding(RangeBase.ValueProperty, binding);
                    HslRangeBase.Background = new LinearGradientBrush()
                    {
                        GradientStops = new GradientStopCollection()
                        {
                            new GradientStop(GetRgbColorFromModel(0,0,0), 0),
                            new GradientStop(GetRgbColorFromModel(0,0,1), 1),
                        },
                        StartPoint = new Point(0, 1),
                        EndPoint = new Point(0, 0)
                    };
                    break;
                default:
                    break;
            }

            GenerateHslComponentAreaContainer(selection);
        }

        public ObservableCollection<Color> CustomColors { get; set; } = new ObservableCollection<Color>() { };

        public static readonly DependencyProperty PresetColorsProperty =
            DependencyProperty.Register(nameof(PresetColors), typeof(ObservableCollection<Color>), typeof(ColorSelector), new PropertyMetadata(new ObservableCollection<Color>()));

        public ObservableCollection<Color> PresetColors
        {
            get { return (ObservableCollection<Color>)GetValue(PresetColorsProperty); }
            set { SetValue(PresetColorsProperty, value); }
        }

        public static readonly DependencyProperty SelectedColorProperty =
            DependencyProperty.Register(nameof(SelectedColor), typeof(Color?), typeof(ColorSelector), new PropertyMetadata(null));

        public Color? SelectedColor
        {
            get { return (Color?)GetValue(SelectedColorProperty); }
            set { SetValue(SelectedColorProperty, value); }
        }

        static Color DefaultColor = (Color)ColorConverter.ConvertFromString("#FFF20D0D");//Colors.Black;

        public static readonly DependencyProperty CurrentColorProperty =
            DependencyProperty.Register(nameof(CurrentColor), typeof(RawColor), typeof(ColorSelector), new PropertyMetadata(new RawColor(DefaultColor.A, DefaultColor.R, DefaultColor.G, DefaultColor.B), new PropertyChangedCallback(CurrentColorChangedCallback)));

        public RawColor CurrentColor
        {
            get { return (RawColor)GetValue(CurrentColorProperty); }
            set { SetValue(CurrentColorProperty, value); }
        }

        public static readonly DependencyProperty CurrentMediaColorProperty =
            DependencyProperty.Register(nameof(CurrentMediaColor), typeof(Color), typeof(ColorSelector), new PropertyMetadata(DefaultColor));

        public Color CurrentMediaColor
        {
            get { return (Color)GetValue(CurrentMediaColorProperty); }
            set { SetValue(CurrentMediaColorProperty, value); }
        }

        public static readonly DependencyProperty CurrentHueColorProperty =
            DependencyProperty.Register(nameof(CurrentHueColor), typeof(Color), typeof(ColorSelector), new PropertyMetadata(DefaultColor));

        public Color CurrentHueColor
        {
            get { return (Color)GetValue(CurrentHueColorProperty); }
            set { SetValue(CurrentHueColorProperty, value); }
        }

        public static readonly DependencyProperty ColorModelProperty =
            DependencyProperty.Register(nameof(ColorModel), typeof(ColorModel), typeof(ColorSelector), new PropertyMetadata(ColorModel.HSV, new PropertyChangedCallback(ColorModelChangedCallback)));

        public ColorModel ColorModel
        {
            get { return (ColorModel)GetValue(ColorModelProperty); }
            set { SetValue(ColorModelProperty, value); }
        }

        public void RebuildColorModelList()
        {
            switch (ColorModel)
            {
                case ColorModel.HSL:
                    HslComponentList.Remove(HslComponent.Value);
                    if (!HslComponentList.Contains(HslComponent.Lightness))
                        HslComponentList.Add(HslComponent.Lightness);
                    break;
                case ColorModel.HSV:
                    HslComponentList.Remove(HslComponent.Lightness);
                    if (!HslComponentList.Contains(HslComponent.Value))
                        HslComponentList.Add(HslComponent.Value);
                    break;
            }
        }

        public void RefreshHsl3dDisplay()
        {
            switch (ColorModel)
            {
                case ColorModel.HSL:
                    Hsl3dDisplayViewport3D.Children.Clear();
                    Hsl3dDisplayViewport3D.Children.Add(Hsl3dDisplayModelVisual3DCube);
                    break;
                case ColorModel.HSV:
                    Hsl3dDisplayViewport3D.Children.Clear();
                    Hsl3dDisplayViewport3D.Children.Add(Hsl3dDisplayModelVisual3DCone);
                    break;
            }
        }

        private static void ColorModelChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ColorSelector cs = (ColorSelector)d;
            var rgb = cs.ModelToRgb(cs.H, cs.S, cs.V);
            cs.RebuildColorModelList();
            cs.RefreshCurrentColor(new RawColor(cs.A, rgb[0], rgb[1], rgb[2]));
            cs.ProcessHslComponentSelection(cs.HslComponentSelection);
            cs.RefreshHsl3dDisplay();
        }

        public static readonly DependencyProperty ColorModelListProperty =
            DependencyProperty.Register(nameof(ColorModelList), typeof(List<ColorModel>), typeof(ColorSelector), 
                new PropertyMetadata(new List<ColorModel>((ColorModel[])Enum.GetValues(typeof(ColorModel)))));

        public List<ColorModel> ColorModelList
        {
            get { return (List<ColorModel>)GetValue(ColorModelListProperty); }
            set { SetValue(ColorModelListProperty, value); }
        }

        public static readonly DependencyProperty HslComponentListProperty =
            DependencyProperty.Register(nameof(HslComponentList), typeof(ObservableCollection<HslComponent>), typeof(ColorSelector), 
                new PropertyMetadata());

        public ObservableCollection<HslComponent> HslComponentList
        {
            get { return (ObservableCollection<HslComponent>)GetValue(HslComponentListProperty); }
            set { SetValue(HslComponentListProperty, value); }
        }

        private static void CurrentColorChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            //ColorSelector cs = (ColorSelector)d;
            //cs.ProcessColorChange();
        }

        public static readonly DependencyProperty RColorLowBoundProperty =
            DependencyProperty.Register(nameof(RColorLowBound), typeof(Color), typeof(ColorSelector), new PropertyMetadata());

        public Color RColorLowBound
        {
            get { return (Color)GetValue(RColorLowBoundProperty); }
            set { SetValue(RColorLowBoundProperty, value); }
        }

        public static readonly DependencyProperty RColorHighBoundProperty =
            DependencyProperty.Register(nameof(RColorHighBound), typeof(Color), typeof(ColorSelector), new PropertyMetadata());

        public Color RColorHighBound
        {
            get { return (Color)GetValue(RColorHighBoundProperty); }
            set { SetValue(RColorHighBoundProperty, value); }
        }

        public static readonly DependencyProperty GColorLowBoundProperty =
            DependencyProperty.Register(nameof(GColorLowBound), typeof(Color), typeof(ColorSelector), new PropertyMetadata());

        public Color GColorLowBound
        {
            get { return (Color)GetValue(GColorLowBoundProperty); }
            set { SetValue(GColorLowBoundProperty, value); }
        }

        public static readonly DependencyProperty GColorHighBoundProperty =
            DependencyProperty.Register(nameof(GColorHighBound), typeof(Color), typeof(ColorSelector), new PropertyMetadata());

        public Color GColorHighBound
        {
            get { return (Color)GetValue(GColorHighBoundProperty); }
            set { SetValue(GColorHighBoundProperty, value); }
        }

        public static readonly DependencyProperty BColorLowBoundProperty =
            DependencyProperty.Register(nameof(BColorLowBound), typeof(Color), typeof(ColorSelector), new PropertyMetadata());

        public Color BColorLowBound
        {
            get { return (Color)GetValue(BColorLowBoundProperty); }
            set { SetValue(BColorLowBoundProperty, value); }
        }

        public static readonly DependencyProperty BColorHighBoundProperty =
            DependencyProperty.Register(nameof(BColorHighBound), typeof(Color), typeof(ColorSelector), new PropertyMetadata());

        public Color BColorHighBound
        {
            get { return (Color)GetValue(BColorHighBoundProperty); }
            set { SetValue(BColorHighBoundProperty, value); }
        }

        public static readonly DependencyProperty HueSector0Property =
            DependencyProperty.Register(nameof(HueSector0), typeof(Color), typeof(ColorSelector), new PropertyMetadata());

        public Color HueSector0
        {
            get { return (Color)GetValue(HueSector0Property); }
            set { SetValue(HueSector0Property, value); }
        }

        public static readonly DependencyProperty HueSector1Property =
            DependencyProperty.Register(nameof(HueSector1), typeof(Color), typeof(ColorSelector), new PropertyMetadata());

        public Color HueSector1
        {
            get { return (Color)GetValue(HueSector1Property); }
            set { SetValue(HueSector1Property, value); }
        }

        public static readonly DependencyProperty HueSector2Property =
            DependencyProperty.Register(nameof(HueSector2), typeof(Color), typeof(ColorSelector), new PropertyMetadata());

        public Color HueSector2
        {
            get { return (Color)GetValue(HueSector2Property); }
            set { SetValue(HueSector2Property, value); }
        }

        public static readonly DependencyProperty HueSector3Property =
            DependencyProperty.Register(nameof(HueSector3), typeof(Color), typeof(ColorSelector), new PropertyMetadata());

        public Color HueSector3
        {
            get { return (Color)GetValue(HueSector3Property); }
            set { SetValue(HueSector3Property, value); }
        }

        public static readonly DependencyProperty HueSector4Property =
            DependencyProperty.Register(nameof(HueSector4), typeof(Color), typeof(ColorSelector), new PropertyMetadata());

        public Color HueSector4
        {
            get { return (Color)GetValue(HueSector4Property); }
            set { SetValue(HueSector4Property, value); }
        }

        public static readonly DependencyProperty HueSector5Property =
            DependencyProperty.Register(nameof(HueSector5), typeof(Color), typeof(ColorSelector), new PropertyMetadata());

        public Color HueSector5
        {
            get { return (Color)GetValue(HueSector5Property); }
            set { SetValue(HueSector5Property, value); }
        }

        public static readonly DependencyProperty FaceBrush1Property =
            DependencyProperty.Register(nameof(FaceBrush1), typeof(ImageBrush), typeof(ColorSelector), new PropertyMetadata());

        public ImageBrush FaceBrush1
        {
            get { return (ImageBrush)GetValue(FaceBrush1Property); }
            set { SetValue(FaceBrush1Property, value); }
        }

        public static readonly DependencyProperty FaceBrush2Property =
            DependencyProperty.Register(nameof(FaceBrush2), typeof(ImageBrush), typeof(ColorSelector), new PropertyMetadata());

        public ImageBrush FaceBrush2
        {
            get { return (ImageBrush)GetValue(FaceBrush2Property); }
            set { SetValue(FaceBrush2Property, value); }
        }

        public static readonly DependencyProperty FaceBrush3Property =
            DependencyProperty.Register(nameof(FaceBrush3), typeof(ImageBrush), typeof(ColorSelector), new PropertyMetadata());

        public ImageBrush FaceBrush3
        {
            get { return (ImageBrush)GetValue(FaceBrush3Property); }
            set { SetValue(FaceBrush3Property, value); }
        }

        public static readonly DependencyProperty FaceBrush4Property =
            DependencyProperty.Register(nameof(FaceBrush4), typeof(ImageBrush), typeof(ColorSelector), new PropertyMetadata());

        public ImageBrush FaceBrush4
        {
            get { return (ImageBrush)GetValue(FaceBrush4Property); }
            set { SetValue(FaceBrush4Property, value); }
        }

        public static readonly DependencyProperty FaceBrush5Property =
            DependencyProperty.Register(nameof(FaceBrush5), typeof(ImageBrush), typeof(ColorSelector), new PropertyMetadata());

        public ImageBrush FaceBrush5
        {
            get { return (ImageBrush)GetValue(FaceBrush5Property); }
            set { SetValue(FaceBrush5Property, value); }
        }

        public static readonly DependencyProperty FaceBrush6Property =
            DependencyProperty.Register(nameof(FaceBrush6), typeof(ImageBrush), typeof(ColorSelector), new PropertyMetadata());

        public ImageBrush FaceBrush6
        {
            get { return (ImageBrush)GetValue(FaceBrush6Property); }
            set { SetValue(FaceBrush6Property, value); }
        }





        public static readonly DependencyProperty SColorLowBoundProperty =
            DependencyProperty.Register(nameof(SColorLowBound), typeof(Color), typeof(ColorSelector), new PropertyMetadata());

        public Color SColorLowBound
        {
            get { return (Color)GetValue(SColorLowBoundProperty); }
            set { SetValue(SColorLowBoundProperty, value); }
        }

        public static readonly DependencyProperty SColorHighBoundProperty =
            DependencyProperty.Register(nameof(SColorHighBound), typeof(Color), typeof(ColorSelector), new PropertyMetadata());

        public Color SColorHighBound
        {
            get { return (Color)GetValue(SColorHighBoundProperty); }
            set { SetValue(SColorHighBoundProperty, value); }
        }

        public static readonly DependencyProperty VColorLowBoundProperty =
            DependencyProperty.Register(nameof(VColorLowBound), typeof(Color), typeof(ColorSelector), new PropertyMetadata());

        public Color VColorLowBound
        {
            get { return (Color)GetValue(VColorLowBoundProperty); }
            set { SetValue(VColorLowBoundProperty, value); }
        }

        public static readonly DependencyProperty VColorHighBoundProperty =
            DependencyProperty.Register(nameof(VColorHighBound), typeof(Color), typeof(ColorSelector), new PropertyMetadata());

        public Color VColorHighBound
        {
            get { return (Color)GetValue(VColorHighBoundProperty); }
            set { SetValue(VColorHighBoundProperty, value); }
        }

        static readonly DependencyProperty HexValueStringProperty =
            DependencyProperty.Register(nameof(HexValueString), typeof(string), typeof(ColorSelector), new PropertyMetadata(DefaultColor.ToString(), new PropertyChangedCallback(HexValueStringChanged)));

        private static void HexValueStringChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ColorSelector cs = (ColorSelector)d;

            if (cs.IgnoreChange)
                return;

            var currentColor = $"#{ToByte(cs.A):X2}{ToByte(cs.R):X2}{ToByte(cs.G):X2}{ToByte(cs.B):X2}";
            if (!cs.HexValueString.StartsWith('#'))
                cs.HexValueString = "#" + cs.HexValueString;
            var hexColor = (Color)ColorConverter.ConvertFromString(cs.HexValueString);
            if (cs.HexValueString != currentColor)
                cs.RefreshCurrentColor(new RawColor(hexColor.A, hexColor.R, hexColor.G, hexColor.B));//(Color)ColorConverter.ConvertFromString(selector.HexValueString));
        }

        protected bool IgnoreChange { get; set; } = false;

        public string HexValueString
        {
            get { return (string)GetValue(HexValueStringProperty); }
            set { SetValue(HexValueStringProperty, value); }
        }

        public static readonly DependencyProperty AProperty =
            DependencyProperty.Register(nameof(A), typeof(double), typeof(ColorSelector), new PropertyMetadata(Convert.ToDouble(DefaultColor.A)));

        public double A
        {
            get { return (double)GetValue(AProperty); }
            set
            {
                SetValue(AProperty, value);
                //Byte byteVal = ToByte(A);
                if (ARangeBaseValue != A)
                {
                    ARangeBaseValue = A;
                }
                if (ATextBoxValue != A)
                {
                    ATextBoxValue = A;
                }

                if (IgnoreChange)
                    return;

                if (CurrentColor.A != A)
                    RefreshCurrentColor(new RawColor(A, R, G, B));
            }
        }

        readonly static DependencyProperty ARangeBaseValueProperty =
            DependencyProperty.Register(nameof(ARangeBaseValue), typeof(double), typeof(ColorSelector), new PropertyMetadata(Convert.ToDouble(DefaultColor.A), new PropertyChangedCallback(ARangeBaseValueChanged)));

        private static void ARangeBaseValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ColorSelector selector = (ColorSelector)d;
            if (selector.A != selector.ARangeBaseValue)
            {
                selector.A = selector.ARangeBaseValue;
            }
        }

        public double ARangeBaseValue
        {
            get { return (double)GetValue(ARangeBaseValueProperty); }
            set { SetValue(ARangeBaseValueProperty, value); }
        }

        static readonly DependencyProperty ATextBoxValueProperty =
            DependencyProperty.Register(nameof(ATextBoxValue), typeof(double), typeof(ColorSelector), new PropertyMetadata(Convert.ToDouble(DefaultColor.A), new PropertyChangedCallback(ATextBoxValueChanged)));

        private static void ATextBoxValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ColorSelector selector = (ColorSelector)d;
            if (selector.A != selector.ATextBoxValue)
            { 
                selector.A = selector.ATextBoxValue;
            }
        }

        public double ATextBoxValue
        {
            get { return (double)GetValue(ATextBoxValueProperty); }
            set { SetValue(ATextBoxValueProperty, value); }
        }

        public static readonly DependencyProperty RProperty =
            DependencyProperty.Register(nameof(R), typeof(double), typeof(ColorSelector), new PropertyMetadata(Convert.ToDouble(DefaultColor.R)));

        public double R
        {
            get { return (double)GetValue(RProperty); }
            set
            {
                SetValue(RProperty, value);
                // Byte byteVal = ToByte(R);
                if (RRangeBaseValue != R)
                {
                    RRangeBaseValue = R;
                }
                if (RTextBoxValue != R)
                {
                    RTextBoxValue = R;
                }

                if (IgnoreChange)
                    return;

                if (CurrentColor.R != R)
                    RefreshCurrentColor(new RawColor(A, R, G, B));
            }
        }

        readonly static DependencyProperty RRangeBaseValueProperty =
            DependencyProperty.Register(nameof(RRangeBaseValue), typeof(double), typeof(ColorSelector), new PropertyMetadata(Convert.ToDouble(DefaultColor.R), new PropertyChangedCallback(RRangeBaseValueChanged)));

        private static void RRangeBaseValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ColorSelector selector = (ColorSelector)d;
            if (selector.R != selector.RRangeBaseValue)
            {
                selector.R = selector.RRangeBaseValue;
            }
        }

        public double RRangeBaseValue
        {
            get { return (double)GetValue(RRangeBaseValueProperty); }
            set { SetValue(RRangeBaseValueProperty, value); }
        }

        static readonly DependencyProperty RTextBoxValueProperty =
            DependencyProperty.Register(nameof(RTextBoxValue), typeof(double), typeof(ColorSelector), new PropertyMetadata(Convert.ToDouble(DefaultColor.R), new PropertyChangedCallback(RTextBoxValueChanged)));

        private static void RTextBoxValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ColorSelector selector = (ColorSelector)d;
            if (selector.R != selector.RTextBoxValue)
            {
                selector.R = selector.RTextBoxValue;
            }
        }

        public double RTextBoxValue
        {
            get { return (double)GetValue(RTextBoxValueProperty); }
            set { SetValue(RTextBoxValueProperty, value); }
        }

        public static readonly DependencyProperty GProperty =
            DependencyProperty.Register(nameof(G), typeof(double), typeof(ColorSelector), new PropertyMetadata(Convert.ToDouble(DefaultColor.G)));

        public double G
        {
            get { return (double)GetValue(GProperty); }
            set
            {
                SetValue(GProperty, value);
                //Byte byteVal = ToByte(G);
                if (GRangeBaseValue != G)
                {
                    GRangeBaseValue = G;
                }
                if (GTextBoxValue != G)
                {
                    GTextBoxValue = G;
                }

                if (IgnoreChange)
                    return;

                if (CurrentColor.G != G)
                    RefreshCurrentColor(new RawColor(A, R, G, B));
            }
        }

        readonly static DependencyProperty GRangeBaseValueProperty =
            DependencyProperty.Register(nameof(GRangeBaseValue), typeof(double), typeof(ColorSelector), new PropertyMetadata(Convert.ToDouble(DefaultColor.G), new PropertyChangedCallback(GRangeBaseValueChanged)));

        private static void GRangeBaseValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ColorSelector selector = (ColorSelector)d;
            if (selector.G != selector.GRangeBaseValue)
            { 
                selector.G = selector.GRangeBaseValue;
            }
        }

        public double GRangeBaseValue
        {
            get { return (double)GetValue(GRangeBaseValueProperty); }
            set { SetValue(GRangeBaseValueProperty, value); }
        }

        static readonly DependencyProperty GTextBoxValueProperty =
            DependencyProperty.Register(nameof(GTextBoxValue), typeof(double), typeof(ColorSelector), new PropertyMetadata(Convert.ToDouble(DefaultColor.G), new PropertyChangedCallback(GTextBoxValueChanged)));

        private static void GTextBoxValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ColorSelector selector = (ColorSelector)d;
            if (selector.G != selector.GTextBoxValue)
            {
                selector.G = selector.GTextBoxValue;
            }
        }

        public double GTextBoxValue
        {
            get { return (double)GetValue(GTextBoxValueProperty); }
            set { SetValue(GTextBoxValueProperty, value); }
        }

        public static readonly DependencyProperty BProperty =
            DependencyProperty.Register(nameof(B), typeof(double), typeof(ColorSelector), new PropertyMetadata(Convert.ToDouble(DefaultColor.B)));

        public double B
        {
            get { return (double)GetValue(BProperty); }
            set
            {
                SetValue(BProperty, value);
                //Byte byteVal = ToByte(B);
                if (BRangeBaseValue != B)
                {
                    BRangeBaseValue = B;
                }
                if (BTextBoxValue != B)
                {
                    BTextBoxValue = B;
                }

                if (IgnoreChange)
                    return;

                if (CurrentColor.B != B)
                    RefreshCurrentColor(new RawColor(A, R, G, B));
            }
        }

        readonly static DependencyProperty BRangeBaseValueProperty =
            DependencyProperty.Register(nameof(BRangeBaseValue), typeof(double), typeof(ColorSelector), new PropertyMetadata(Convert.ToDouble(DefaultColor.B), new PropertyChangedCallback(BRangeBaseValueChanged)));

        private static void BRangeBaseValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ColorSelector selector = (ColorSelector)d;
            if (selector.B != selector.BRangeBaseValue)
            {
                selector.B = selector.BRangeBaseValue;
            }
        }

        public double BRangeBaseValue
        {
            get { return (double)GetValue(BRangeBaseValueProperty); }
            set { SetValue(BRangeBaseValueProperty, value); }
        }

        static readonly DependencyProperty BTextBoxValueProperty =
            DependencyProperty.Register(nameof(BTextBoxValue), typeof(double), typeof(ColorSelector), new PropertyMetadata(Convert.ToDouble(DefaultColor.B), new PropertyChangedCallback(BTextBoxValueChanged)));

        private static void BTextBoxValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ColorSelector selector = (ColorSelector)d;
            if (selector.B != selector.BTextBoxValue)
            {
                selector.B = selector.BTextBoxValue;
            }
        }

        public double BTextBoxValue
        {
            get { return (double)GetValue(BTextBoxValueProperty); }
            set { SetValue(BTextBoxValueProperty, value); }
        }

        public static readonly DependencyProperty HProperty =
            DependencyProperty.Register(nameof(H), typeof(double), typeof(ColorSelector), new PropertyMetadata((double)System.Drawing.Color.FromArgb(DefaultColor.A, DefaultColor.R, DefaultColor.G, DefaultColor.B).GetHue()));

        public double H
        {
            get { return (double)GetValue(HProperty); }
            set
            {
                SetValue(HProperty, value);
                if (HRangeBaseValue != H)
                {
                    HRangeBaseValue = H;
                }
                if (HTextBoxValue != H)
                {
                    HTextBoxValue = H;
                }

                if (IgnoreChange)
                    return;

                var tempHsl = RgbToModel(R, G, B);
                if (tempHsl[0] != H)
                {
                    var rgb = ModelToRgb(H, S, V);
                    RefreshCurrentColor(new RawColor(A, rgb[0], rgb[1], rgb[2]), nameof(H));
                }
            }
        }

        readonly static DependencyProperty HRangeBaseValueProperty =
            DependencyProperty.Register(nameof(HRangeBaseValue), typeof(double), typeof(ColorSelector), new PropertyMetadata((double)System.Drawing.Color.FromArgb(DefaultColor.A, DefaultColor.R, DefaultColor.G, DefaultColor.B).GetHue(), new PropertyChangedCallback(HRangeBaseValueChanged)));

        private static void HRangeBaseValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ColorSelector selector = (ColorSelector)d;
            if (selector.H != selector.HRangeBaseValue)
            {
                selector.H = selector.HRangeBaseValue;
            }
        }

        public double HRangeBaseValue
        {
            get { return (double)GetValue(HRangeBaseValueProperty); }
            set { SetValue(HRangeBaseValueProperty, value); }
        }

        static readonly DependencyProperty HTextBoxValueProperty =
            DependencyProperty.Register(nameof(HTextBoxValue), typeof(double), typeof(ColorSelector), new PropertyMetadata((double)System.Drawing.Color.FromArgb(DefaultColor.A, DefaultColor.R, DefaultColor.G, DefaultColor.B).GetHue(), new PropertyChangedCallback(HTextBoxValueChanged)));

        private static void HTextBoxValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ColorSelector selector = (ColorSelector)d;
            if (selector.H != selector.HTextBoxValue)
            {
                selector.H = selector.HTextBoxValue;
            }
        }

        public double HTextBoxValue
        {
            get { return (double)GetValue(HTextBoxValueProperty); }
            set { SetValue(HTextBoxValueProperty, value); }
        }

        public static readonly DependencyProperty SProperty =
            DependencyProperty.Register(nameof(S), typeof(double), typeof(ColorSelector), new PropertyMetadata((double)System.Drawing.Color.FromArgb(DefaultColor.A, DefaultColor.R, DefaultColor.G, DefaultColor.B).GetSaturation()));

        public double S
        {
            get { return (double)GetValue(SProperty); }
            set
            {
                SetValue(SProperty, value);
                if (SRangeBaseValue != S)
                {
                    SRangeBaseValue = S;
                }
                if (STextBoxValue != S)
                {
                    STextBoxValue = S;
                }

                if (IgnoreChange)
                    return;

                var tempHsl = RgbToModel(R, G, B);
                if (tempHsl[1] != S)
                {
                    var rgb = ModelToRgb(H, S, V);
                    RefreshCurrentColor(new RawColor(A, rgb[0], rgb[1], rgb[2]), nameof(S));
                }
            }
        }

        readonly static DependencyProperty SRangeBaseValueProperty =
            DependencyProperty.Register(nameof(SRangeBaseValue), typeof(double), typeof(ColorSelector), new PropertyMetadata((double)System.Drawing.Color.FromArgb(DefaultColor.A, DefaultColor.R, DefaultColor.G, DefaultColor.B).GetSaturation(), new PropertyChangedCallback(SRangeBaseValueChanged)));

        private static void SRangeBaseValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ColorSelector selector = (ColorSelector)d;
            if (selector.S != selector.SRangeBaseValue)
            {
                selector.S = selector.SRangeBaseValue;
            }
        }

        public double SRangeBaseValue
        {
            get { return (double)GetValue(SRangeBaseValueProperty); }
            set { SetValue(SRangeBaseValueProperty, value); }
        }

        static readonly DependencyProperty STextBoxValueProperty =
            DependencyProperty.Register(nameof(STextBoxValue), typeof(double), typeof(ColorSelector), new PropertyMetadata((double)System.Drawing.Color.FromArgb(DefaultColor.A, DefaultColor.R, DefaultColor.G, DefaultColor.B).GetSaturation(), new PropertyChangedCallback(STextBoxValueChanged)));

        private static void STextBoxValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ColorSelector selector = (ColorSelector)d;
            if (selector.S != selector.STextBoxValue)
            {
                selector.S = selector.STextBoxValue;
            }
        }

        public double STextBoxValue
        {
            get { return (double)GetValue(STextBoxValueProperty); }
            set { SetValue(STextBoxValueProperty, value); }
        }

        public static readonly DependencyProperty VProperty =
            DependencyProperty.Register(nameof(V), typeof(double), typeof(ColorSelector), new PropertyMetadata((double)System.Drawing.Color.FromArgb(DefaultColor.A, DefaultColor.R, DefaultColor.G, DefaultColor.B).GetBrightness()));

        public double V
        {
            get { return (double)GetValue(VProperty); }
            set
            {
                SetValue(VProperty, value);
                if (VRangeBaseValue != V)
                {
                    VRangeBaseValue = V;
                }
                if (VTextBoxValue != V)
                {
                    VTextBoxValue = V;
                }

                if (IgnoreChange)
                    return;

                var tempHsl = RgbToModel(R, G, B);
                if (tempHsl[2] != V)
                {
                    var rgb = ModelToRgb(H, S, V);
                    RefreshCurrentColor(new RawColor(A, rgb[0], rgb[1], rgb[2]), nameof(V));
                }
            }
        }

        readonly static DependencyProperty VRangeBaseValueProperty =
            DependencyProperty.Register(nameof(VRangeBaseValue), typeof(double), typeof(ColorSelector), new PropertyMetadata((double)System.Drawing.Color.FromArgb(DefaultColor.A, DefaultColor.R, DefaultColor.G, DefaultColor.B).GetBrightness(), new PropertyChangedCallback(VRangeBaseValueChanged)));

        private static void VRangeBaseValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ColorSelector selector = (ColorSelector)d;
            if (selector.V != selector.VRangeBaseValue)
            {
                selector.V = selector.VRangeBaseValue;
            }
        }

        public double VRangeBaseValue
        {
            get { return (double)GetValue(VRangeBaseValueProperty); }
            set { SetValue(VRangeBaseValueProperty, value); }
        }

        static readonly DependencyProperty VTextBoxValueProperty =
            DependencyProperty.Register(nameof(VTextBoxValue), typeof(double), typeof(ColorSelector), new PropertyMetadata((double)System.Drawing.Color.FromArgb(DefaultColor.A, DefaultColor.R, DefaultColor.G, DefaultColor.B).GetBrightness(), new PropertyChangedCallback(VTextBoxValueChanged)));

        private static void VTextBoxValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ColorSelector selector = (ColorSelector)d;
            if (selector.V != selector.VTextBoxValue)
            {
                selector.V = selector.VTextBoxValue;
            }
        }

        public double VTextBoxValue
        {
            get { return (double)GetValue(VTextBoxValueProperty); }
            set { SetValue(VTextBoxValueProperty, value); }
        }

        public static readonly DependencyProperty IsMenuOpenProperty =
            DependencyProperty.Register(nameof(IsMenuOpen), typeof(bool), typeof(ColorSelector), new PropertyMetadata(false));

        public bool IsMenuOpen
        {
            get { return (bool)GetValue(IsMenuOpenProperty); }
            set { SetValue(IsMenuOpenProperty, value); }
        }

        public static readonly DependencyProperty ColorModelsVisibleProperty =
            DependencyProperty.Register(nameof(ColorModelsVisible), typeof(bool), typeof(ColorSelector), new PropertyMetadata(true));

        public bool ColorModelsVisible
        {
            get { return (bool)GetValue(ColorModelsVisibleProperty); }
            set { SetValue(ColorModelsVisibleProperty, value); }
        }

        public static readonly DependencyProperty PresetColorsVisibleProperty =
            DependencyProperty.Register(nameof(PresetColorsVisible), typeof(bool), typeof(ColorSelector), new PropertyMetadata(true));

        public bool PresetColorsVisible
        {
            get { return (bool)GetValue(PresetColorsVisibleProperty); }
            set { SetValue(PresetColorsVisibleProperty, value); }
        }

        public static readonly DependencyProperty Display2dVisibleProperty =
            DependencyProperty.Register(nameof(Display2dVisible), typeof(bool), typeof(ColorSelector), new PropertyMetadata(true));

        public bool Display2dVisible
        {
            get { return (bool)GetValue(Display2dVisibleProperty); }
            set { SetValue(Display2dVisibleProperty, value); }
        }

        public static readonly DependencyProperty Display3dVisibleProperty =
            DependencyProperty.Register(nameof(Display3dVisible), typeof(bool), typeof(ColorSelector), new PropertyMetadata(true));

        public bool Display3dVisible
        {
            get { return (bool)GetValue(Display3dVisibleProperty); }
            set { SetValue(Display3dVisibleProperty, value); }
        }

        public static readonly DependencyProperty ComponentsVisibleProperty =
            DependencyProperty.Register(nameof(ComponentsVisible), typeof(bool), typeof(ColorSelector), new PropertyMetadata(true));

        public bool ComponentsVisible
        {
            get { return (bool)GetValue(ComponentsVisibleProperty); }
            set { SetValue(ComponentsVisibleProperty, value); }
        }

        public static readonly DependencyProperty ColorPreviewVisibleProperty =
            DependencyProperty.Register(nameof(ColorPreviewVisible), typeof(bool), typeof(ColorSelector), new PropertyMetadata(true));

        public bool ColorPreviewVisible
        {
            get { return (bool)GetValue(ColorPreviewVisibleProperty); }
            set { SetValue(ColorPreviewVisibleProperty, value); }
        }

        public static readonly DependencyProperty CustomColorsVisibleProperty =
            DependencyProperty.Register(nameof(CustomColorsVisible), typeof(bool), typeof(ColorSelector), new PropertyMetadata(true));

        public bool CustomColorsVisible
        {
            get { return (bool)GetValue(CustomColorsVisibleProperty); }
            set { SetValue(CustomColorsVisibleProperty, value); }
        }

        public static readonly DependencyProperty HexadecimalComponentVisibleProperty =
            DependencyProperty.Register(nameof(HexadecimalComponentVisible), typeof(bool), typeof(ColorSelector), new PropertyMetadata(true));

        public bool HexadecimalComponentVisible
        {
            get { return (bool)GetValue(HexadecimalComponentVisibleProperty); }
            set { SetValue(HexadecimalComponentVisibleProperty, value); }
        }

        public static readonly DependencyProperty AlphaComponentVisibleProperty =
            DependencyProperty.Register(nameof(AlphaComponentVisible), typeof(bool), typeof(ColorSelector), new PropertyMetadata(true));

        public bool AlphaComponentVisible
        {
            get { return (bool)GetValue(AlphaComponentVisibleProperty); }
            set { SetValue(AlphaComponentVisibleProperty, value); }
        }

        public static readonly DependencyProperty RgbComponentVisibleProperty =
            DependencyProperty.Register(nameof(RgbComponentVisible), typeof(bool), typeof(ColorSelector), new PropertyMetadata(true));

        public bool RgbComponentVisible
        {
            get { return (bool)GetValue(RgbComponentVisibleProperty); }
            set { SetValue(RgbComponentVisibleProperty, value); }
        }

        public static readonly DependencyProperty HslvComponentVisibleProperty =
            DependencyProperty.Register(nameof(HslvComponentVisible), typeof(bool), typeof(ColorSelector), new PropertyMetadata(true));

        public bool HslvComponentVisible
        {
            get { return (bool)GetValue(HslvComponentVisibleProperty); }
            set { SetValue(HslvComponentVisibleProperty, value); }
        }
    }
}
