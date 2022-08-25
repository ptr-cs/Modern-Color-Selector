using System;
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
                Regex.Match((string)value, "^#(?:[0-9a-fA-F]{8})$").Success,
                "String must match a valid ARGB Hexadecimal Color (ex. #FF001122)");
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
    public class doubleToReflectedAbsoluteValueDouble : IValueConverter
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
            return valueToConvert.ToString().Substring(0, 1);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string valueToConvert = (string)value;
            var match = ((HslComponent[])Enum.GetValues(typeof(HslComponent))).Where(s => s.ToString().Substring(0, 1) == valueToConvert).FirstOrDefault();
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
        PART_hslComponentViewport3D
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

    [TemplatePart(Name = nameof(TemplatePart.PART_hslComponentViewport3D), Type = typeof(Viewport3D))]
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
        public static extern bool DeleteObject(IntPtr hObject);

        public static ImageBrush CreateBilinearGradient(int w, int h, Color upperLeft, Color upperRight, Color lowerLeft, Color lowerRight)
        {
            BitmapSource? source;

            using (System.Drawing.Bitmap bmp = new System.Drawing.Bitmap(w, h))
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

        GradientStop rLowBoundGraientStop = new() { Offset = 0 };
        GradientStop rHighBoundGradientStop = new() { Offset = 1 };
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

        GradientStop gLowBoundGraientStop = new() { Offset = 0 };
        GradientStop gHighBoundGradientStop = new() { Offset = 1 };
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

        GradientStop bLowBoundGraientStop = new() { Offset = 0 };
        GradientStop bHighBoundGradientStop = new() { Offset = 1 };
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

        GradientStop hSector0GradientStop = new() { Offset = 0 };
        GradientStop hSector1GradientStop = new() { Offset = 1.0 / 6 };
        GradientStop hSector2GradientStop = new() { Offset = 1.0 / 6 * 2 };
        GradientStop hSector3GradientStop = new() { Offset = 1.0 / 6 * 3 };
        GradientStop hSector4GradientStop = new() { Offset = 1.0 / 6 * 4 };
        GradientStop hSector5GradientStop = new() { Offset = 1.0 / 6 * 5 };
        GradientStop hSector6GradientStop = new() { Offset = 1 };
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

        GradientStop sLowBoundGraientStop = new() { Offset = 0 };
        GradientStop sHighBoundGradientStop = new() { Offset = 1 };
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

        GradientStop vLowBoundGraientStop = new() { Offset = 0 };
        GradientStop vHighBoundGradientStop = new() { Offset = 1 };
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
            var o = H + H_ROC;
            var r = Math.Round(o); 
            var c = Math.Clamp(r, HSL_MIN, H_MAX);
            H = c;
        }

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


        GradientStop hslComponentAreaHueLowBoundGraientStop = new() { Color = Colors.Gray, Offset = 0 };
        GradientStop hslComponentAreaHueHighBoundGradientStop = new() { Offset = 1 };
        LinearGradientBrush hslComponentAreaSaturationGradientBrush = new() { EndPoint = new(1,0)};
        LinearGradientBrush hslComponentAreaLightnessGradientBrush = new() { EndPoint = new(1, 0) };
        LinearGradientBrush hslComponentAreaValueGradientBrush = new() { EndPoint = new(1, 0) };
        LinearGradientBrush hslComponentAreaLightnessRelativeSaturationOverlay = new() 
        { 
            EndPoint = new Point(0, 1),
            GradientStops = new GradientStopCollection()
            {
                new GradientStop(Colors.Gray, 1),
                new GradientStop((Color)ColorConverter.ConvertFromString("#00000000"), 0)
            }
        };
        LinearGradientBrush hslComponentAreaLightnessRelativeValueOverlay = new()
        {
            EndPoint = new Point(0, 1),
            GradientStops = new GradientStopCollection()
            {
                new GradientStop(Colors.White, 1),
                new GradientStop((Color)ColorConverter.ConvertFromString("#00000000"), 0)
            }
        };
        SolidColorBrush hslComponentAreaLightnessWhiteBackground = new() { Color = Colors.White };

        Grid hslComponentAreaXaxisValueGrid = new Grid() {  HorizontalAlignment = HorizontalAlignment.Left };
        Grid hslComponentAreaYaxisValueGrid = new Grid() {  VerticalAlignment = VerticalAlignment.Top };
        Border hslComponentAreaXaxisBoundGuide = makeHslComponentGridXaxisGuide();
        Border hslComponentAreaYaxisBoundGuide = makeHslComponentGridYaxisGuide();
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
                    hslComponentArea.SizeChanged -= new SizeChangedEventHandler(HslComponentArea_SizeChanged);
                }
                hslComponentArea = value;

                if (hslComponentArea != null)
                {
                    hslComponentArea.PreviewMouseMove += new MouseEventHandler(HslComponentArea_PreviewMouseMove);
                    hslComponentArea.PreviewMouseLeftButtonDown += new MouseButtonEventHandler(HslComponentArea_PreviewMouseLeftButtonDown);
                    hslComponentArea.SizeChanged += new SizeChangedEventHandler(HslComponentArea_SizeChanged);

                    Binding highBoundBinding = new(nameof(CurrentHueColor)) { Mode = BindingMode.OneWay, Source = this };
                    BindingOperations.SetBinding(hslComponentAreaHueHighBoundGradientStop, GradientStop.ColorProperty, highBoundBinding);

                    GradientStopCollection spectrum = new()
                    {
                        new GradientStop((Color)ColorConverter.ConvertFromString("#FF0000"), 0),
                        new GradientStop((Color)ColorConverter.ConvertFromString("#FFFF00"), 1.0 / 6),
                        new GradientStop((Color)ColorConverter.ConvertFromString("#00FF00"), (1.0 / 6) * 2),
                        new GradientStop((Color)ColorConverter.ConvertFromString("#00FFFF"), (1.0 / 6) * 3),
                        new GradientStop((Color)ColorConverter.ConvertFromString("#0000FF"), (1.0 / 6) * 4),
                        new GradientStop((Color)ColorConverter.ConvertFromString("#FF00FF"), (1.0 / 6) * 5),
                        new GradientStop((Color)ColorConverter.ConvertFromString("#FF0000"), 1),
                    };

                    hslComponentAreaSaturationGradientBrush.GradientStops = spectrum;
                    Binding saturationOpacityBinding = new(nameof(S)) { Mode = BindingMode.OneWay, Source = this };
                    BindingOperations.SetBinding(hslComponentAreaSaturationGradientBrush, LinearGradientBrush.OpacityProperty, saturationOpacityBinding);

                    hslComponentAreaLightnessGradientBrush.GradientStops = spectrum;
                    Binding lightnessOpacityBindinig = new(nameof(V)) { Mode = BindingMode.OneWay, Source = this, Converter = new doubleToReflectedAbsoluteValueDouble() };
                    BindingOperations.SetBinding(hslComponentAreaLightnessGradientBrush, LinearGradientBrush.OpacityProperty, lightnessOpacityBindinig);

                    hslComponentAreaValueGradientBrush.GradientStops = spectrum;
                    Binding valueOpacityBindinig = new(nameof(V)) { Mode = BindingMode.OneWay, Source = this };
                    BindingOperations.SetBinding(hslComponentAreaValueGradientBrush, LinearGradientBrush.OpacityProperty, valueOpacityBindinig);

                    Binding valueOpacityBindinig2 = new(nameof(V)) { Mode = BindingMode.OneWay, Source = this };
                    BindingOperations.SetBinding(hslComponentAreaLightnessRelativeValueOverlay, LinearGradientBrush.OpacityProperty, valueOpacityBindinig2);

                    Binding lightnessOpacityBindinig2 = new(nameof(V)) { Mode = BindingMode.OneWay, Source = this, Converter = new doubleToReflectedAbsoluteValueDouble() };
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

        public static Border makeHslComponentGridMidPointGuide()
        {
            return new Border()
            {
                Width = 3,
                Height = 3,
                Background = new SolidColorBrush(Colors.Transparent)
            };
        }

        public static Border makeHslComponentGridXaxisGuide()
        {
            return new Border()
            {
                Width = 3,
                BorderThickness = new Thickness(1, 0, 1, 0),
                Background = new SolidColorBrush(Colors.Black),
                BorderBrush = new SolidColorBrush(Colors.White)
            };
        }

        public static Border makeHslComponentGridYaxisGuide()
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
            hslComponentAreaXaxisBoundGuide = makeHslComponentGridXaxisGuide();
            hslComponentAreaYaxisBoundGuide = makeHslComponentGridYaxisGuide();

            hslComponentAreaXaxisValueGrid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(0, GridUnitType.Auto) });
            hslComponentAreaXaxisValueGrid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(0, GridUnitType.Auto) });
            hslComponentAreaXaxisValueGrid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Star) });

            hslComponentAreaYaxisValueGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(0, GridUnitType.Auto) });
            hslComponentAreaYaxisValueGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(0, GridUnitType.Auto) });
            hslComponentAreaYaxisValueGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });

            Border midGuideX = makeHslComponentGridMidPointGuide();
            Border midGuideY = makeHslComponentGridMidPointGuide();
            var guide2X = makeHslComponentGridXaxisGuide();
            var guide2Y = makeHslComponentGridYaxisGuide();

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

        /// <summary>
        /// When the HSL component area is clicked on, process the click coordinates and
        /// set the relevant color component properties.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <seealso cref="ProcessHslComponentAreaMouseInput"/>
        private void HslComponentArea_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
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
            if (e.LeftButton != MouseButtonState.Pressed)
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

        public Color getColorFromRawColor()
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
                var color = getColorFromRawColor();
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

            CustomColors.Insert(0, getColorFromRawColor());

            RaiseCustomColorSavedEvent();
        }

        /// <summary>
        /// Sets the current color as the currently selected color.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SelectCustomColorButtonBase_Click(object sender, RoutedEventArgs e)
        {
            SelectedColor = getColorFromRawColor();
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

            // HslComponentViewport3D = GetTemplateChild(nameof(TemplatePart.PART_hslComponentViewport3D)) as Viewport3D;

            HslComponentList = new ObservableCollection<HslComponent>((HslComponent[])Enum.GetValues(typeof(HslComponent)));
            RebuildColorModelList();
            RefreshRangeBaseVisuals();
            ProcessHslComponentSelection(HslComponentSelection);
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

            // if maxRgbValue is 0, return black HSL;
            //if (max == 0)
            //    return new List<double>(3) { 0, 0, 0 };

            var lightness = (max + min) / 2;

            var chroma = max - min;

            var saturation = (lightness <= 0.5) ?
                chroma / (max + min) : chroma / (2 - max - min);

            var hue = 0.0;
            if (chroma == 0)
            {
                hue = 0;
            }
            else
            {
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

            value = value * 255;
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
            if (H != hue && nameof(H) != originatingPropertyName)
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

            if (S != saturation && nameof(S) != originatingPropertyName)
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

            // Debug.WriteLine($"{R} {G} {B}, {H} {S} {V}");

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

        static Color DefaultColor = Colors.Black;

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

        private static void ColorModelChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ColorSelector cs = (ColorSelector)d;
            var rgb = cs.ModelToRgb(cs.H, cs.S, cs.V);
            cs.RebuildColorModelList();
            cs.RefreshCurrentColor(new RawColor(cs.A, rgb[0], rgb[1], rgb[2]));
            // cs.RefreshRangeBaseVisuals();
            cs.ProcessHslComponentSelection(cs.HslComponentSelection);
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
    } 
}
