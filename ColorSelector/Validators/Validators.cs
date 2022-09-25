using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace ColorSelector.Validators;

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
    private double val = -1;
    private readonly byte min = byte.MinValue;
    private readonly byte max = byte.MaxValue;
    public override ValidationResult Validate(object value, System.Globalization.CultureInfo cultureInfo)
    {
        val = -1;
        return new ValidationResult(Double.TryParse((string)value, out val) && min <= val && val <= max, $"Value must be a number with within the range [{min}, {max}]");
    }
}

public class HueStringValidationRule : ValidationRule
{
    public override ValidationResult Validate(object value, System.Globalization.CultureInfo cultureInfo)
    {
        return new ValidationResult(Regex.Match((string)value, "^(?:36[0]|3[0-5][0-9]|[12][0-9][0-9]|[1-9][0-9]|[0-9])$").Success, $"Value must be a number, with two optional significant digits, within the range [{0}, {360}]");
    }
}

public class SaturationStringValidationRule : ValidationRule
{
    private double val = -1;
    private readonly double min = 0.0;
    private readonly double max = 1.0;
    public override ValidationResult Validate(object value, System.Globalization.CultureInfo cultureInfo)
    {
        val = -1;
        return new ValidationResult(Double.TryParse((string)value, out val) && min <= val && val <= max, $"Value must be a number, with two optional significant digits, within the range [{min}, {max}]");
    }
}

public class ValueStringValidationRule : ValidationRule
{
    private double val = -1;
    private readonly double min = 0.0;
    private readonly double max = 1.0;
    public override ValidationResult Validate(object value, System.Globalization.CultureInfo cultureInfo)
    {
        val = -1;
        return new ValidationResult(Double.TryParse((string)value, out val) && min <= val && val <= max, $"Value must be a number, with two optional significant digits, within the range [{min}, {max}]");
    }
}

public class ColorColumnValidationRule : ValidationRule
{
    private int val = -1;
    private readonly int min = 1;
    private readonly int max = 100;
    public override ValidationResult Validate(object value, System.Globalization.CultureInfo cultureInfo)
    {
        val = -1;
        return new ValidationResult(Int32.TryParse((string)value, out val) && min <= val && val <= max, $"Value must be an integer within the range [{min}, {max}]");
    }
}

public class CustomColorsLimitValidationRule : ValidationRule
{
    private int val = -1;
    private readonly int min = 1;
    private readonly int max = 1000;
    public override ValidationResult Validate(object value, System.Globalization.CultureInfo cultureInfo)
    {
        val = -1;
        return new ValidationResult(Int32.TryParse((string)value, out val) && min <= val && val <= max, $"Value must be an integer within the range [{min}, {max}]");
    }
}