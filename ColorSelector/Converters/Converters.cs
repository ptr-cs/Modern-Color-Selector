using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace ColorSelector.Converters;


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

[ValueConversion(typeof(ColorComponent), typeof(bool))]
public class HslComponentComparisonConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        ColorComponent valueToConvert = (ColorComponent)value;
        ColorComponent compareValue = (ColorComponent)parameter;

        return valueToConvert == compareValue;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return ColorComponent.Hue; // Conversions back (not necessary) should default to Hue
    }
}

[ValueConversion(typeof(ColorComponent), typeof(string))]
public class HslComponentToAbbreviatedStringConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        ColorComponent valueToConvert = (ColorComponent)value;
        return valueToConvert.ToString()[..1];
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        string valueToConvert = (string)value;
        var match = ((ColorComponent[])Enum.GetValues(typeof(ColorComponent))).Where(s => s.ToString()[..1] == valueToConvert).FirstOrDefault();
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

[ValueConversion(typeof(double), typeof(double))]
public class RotationAngleConverterConeValue : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        double angle = System.Convert.ToDouble(value);
        return (angle * -180.0) + 90.0;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return System.Convert.ToDouble(value);
    }
}

[ValueConversion(typeof(double), typeof(double))]
public class RotationAngleConverterConeSaturation : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        double angle = System.Convert.ToDouble(value);
        return (angle * 90.0);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return System.Convert.ToDouble(value);
    }
}

[ValueConversion(typeof(object[]), typeof(double))]
public class MultiAngleConverterConeSaturationValue : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        double value = System.Convert.ToDouble(values[0]);
        double saturation = System.Convert.ToDouble(values[1]);
        return (value * -(180 - (saturation * 90))) + 90.0;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        return new object[2] { 0, 0 };
    }
}