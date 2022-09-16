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
        return new ValidationResult(Double.TryParse((string)value, out val) && 0.0 <= val && val <= 1.0,
            String.Format("Value must be a number, with two optional significant digits, within the range [{0}, {1}]", 0.0, 1.0));
    }
}

public class ValueStringValidationRule : ValidationRule
{
    double val = -1;
    public override ValidationResult Validate(object value, System.Globalization.CultureInfo cultureInfo)
    {
        val = -1;
        return new ValidationResult(Double.TryParse((string)value, out val) && 0.0 <= val && val <= 1.0,
            String.Format("Value must be a number, with two optional significant digits, within the range [{0}, {1}]", 0.0, 1.0));
    }
}
