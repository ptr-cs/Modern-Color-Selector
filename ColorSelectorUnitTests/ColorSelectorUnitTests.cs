using NUnit.Framework;
using ColorSelector;
using System.Globalization;
using System.Windows.Controls;
using ColorSelector.Validators;

namespace ColorSelectorUnitTests
{
    /// <summary>
    /// Tests the input validators used on Controls that accept string input.
    /// </summary>
    [TestFixture]
    [RequiresThread(ApartmentState.STA)]
    public class ValidationTests
    {
        public const double MinRGBA = 0.0;
        public const double MaxRGBA = 255.0;
        public const double MinHue = 0.0;
        public const double MaxHue = 360.0;
        public const double MinSLV = 0.0;
        public const double MaxSLV = 1.0;
        public const int ValidatorRand = 1000;
        public const string FuzzingChars = " \"!#$%&\'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~";
        readonly ArgbHexadecimalColorStringValidationRule hexRule = new();
        readonly ColorByteStringValidationRule argbRule = new();
        readonly HueStringValidationRule hueRule = new();
        readonly SaturationStringValidationRule saturationRule = new();
        readonly SaturationStringValidationRule valueRule = new();

        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void HexadecimalValidator([Random(0, 20, ValidatorRand)] int length)
        {
            string str = TestContext.CurrentContext.Random.GetString(length, FuzzingChars);
            TestContext.WriteLine(str);
            ValidationResult result = hexRule.Validate(str, CultureInfo.CurrentCulture);
            if (long.TryParse(str, NumberStyles.HexNumber, CultureInfo.CurrentCulture, out _) && (6 <= length && length <= 9))
                Assert.That(result.IsValid, Is.True, str);
            else
                Assert.That(result.IsValid, Is.False, str);
        }

        [Test]
        public void ArgbValidator([Random(0, 20, ValidatorRand)] int length)
        {
            string str = TestContext.CurrentContext.Random.GetString(length, FuzzingChars);
            TestContext.WriteLine(str);
            ValidationResult result = argbRule.Validate(str, CultureInfo.CurrentCulture);
            if (double.TryParse(str, NumberStyles.Integer|NumberStyles.Float|NumberStyles.AllowLeadingSign, CultureInfo.CurrentCulture, out double parsed))
            {
                if ((MinRGBA <= parsed && parsed <= MaxRGBA))
                    Assert.That(result.IsValid, Is.True, str);
            }
            else
                Assert.That(result.IsValid, Is.False, str);
        }

        [Test]
        public void HueValidator([Random(0, 20, ValidatorRand)] int length)
        {
            string str = TestContext.CurrentContext.Random.GetString(length, FuzzingChars);
            TestContext.WriteLine(str);
            ValidationResult result = hueRule.Validate(str, CultureInfo.CurrentCulture);
            if (double.TryParse(str, NumberStyles.Integer | NumberStyles.Float, CultureInfo.CurrentCulture, out double parsed))
            {
                if ((MinHue <= parsed && parsed <= MaxHue))
                    Assert.That(result.IsValid, Is.True, str);
            }
            else
                Assert.That(result.IsValid, Is.False, str);
        }

        [Test]
        public void SaturationValidator([Random(0, 20, ValidatorRand)] int length)
        {
            string str = TestContext.CurrentContext.Random.GetString(length, FuzzingChars);
            TestContext.WriteLine(str);
            ValidationResult result = saturationRule.Validate(str, CultureInfo.CurrentCulture);
            if (double.TryParse(str, NumberStyles.Integer | NumberStyles.Float, CultureInfo.CurrentCulture, out double parsed))
            {
                if ((MinSLV <= parsed && parsed <= MaxSLV))
                    Assert.That(result.IsValid, Is.True, str);
            }
            else
                Assert.That(result.IsValid, Is.False, str);
        }

        [Test]
        public void ValueValidator([Random(0, 20, ValidatorRand)] int length)
        {
            string str = TestContext.CurrentContext.Random.GetString(length, FuzzingChars);
            TestContext.WriteLine(str);
            ValidationResult result = valueRule.Validate(str, CultureInfo.CurrentCulture);
            if (double.TryParse(str, NumberStyles.Integer | NumberStyles.Float, CultureInfo.CurrentCulture, out double parsed))
            {
                if ((MinSLV <= parsed && parsed <= MaxSLV))
                    Assert.That(result.IsValid, Is.True, str);
            }
            else
                Assert.That(result.IsValid, Is.False, str);
        }
    }

    /// <summary>
    /// Tests the code that is involved in processing new colors, color changes, and changes 
    /// of the active color model (HSL <-> HSV).
    /// </summary>
    [TestFixture]
    [RequiresThread(ApartmentState.STA)]
    public class ProcessingTests
    {
        public const double MinRGBA = 0.0;
        public const double MaxRGBA = 255.0;
        public const double MinHue = 0.0;
        public const double MaxHue = 360.0;
        public const double MinSLV = 0.0;
        public const double MaxSLV = 1.0;
        public const int RgbRand = 6;
        public const int HslvRand = 10;
        public const int SignificantDigits = 9;
        readonly ColorSelector.ColorSelector selector = new();

        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void ArgbRange([Range(MinRGBA, MaxRGBA, 0.1)] double x)
        {
            selector.A = x;
            Assert.That(selector.CurrentColor.A, Is.EqualTo(x));
            selector.R = x;
            Assert.That(selector.CurrentColor.R, Is.EqualTo(x));
            selector.G = x;
            Assert.That(selector.CurrentColor.G, Is.EqualTo(x));
            selector.B = x;
            Assert.That(selector.CurrentColor.B, Is.EqualTo(x));
        }

        [Test]
        public void HueRange([Range(MinHue, MaxHue, 0.1)] double hue)
        {
            selector.ColorModel = ColorModel.HSL;
            selector.H = hue;
            var r = selector.CurrentColor.R;
            var g = selector.CurrentColor.G;
            var b = selector.CurrentColor.B;
            Assert.That(Math.Round(ColorSelector.ColorSelector.GetHueFromRgbByteRange(r, g, b), SignificantDigits), Is.EqualTo(Math.Round(hue, SignificantDigits)));
        }

        [Test]
        public void SaturationRange([Range(MinSLV, MaxSLV, 0.001)] double slv)
        {
            selector.ColorModel = ColorModel.HSL;
            selector.S = slv;
            var r = selector.CurrentColor.R;
            var g = selector.CurrentColor.G;
            var b = selector.CurrentColor.B;
            Assert.That(Math.Round(ColorSelector.ColorSelector.GetHslSaturationFromRgbByteRange(r, g, b), SignificantDigits), Is.EqualTo(Math.Round(slv, SignificantDigits)));
        }

        [Test]
        public void LightnessRange([Range(MinSLV, MaxSLV, 0.001)] double slv)
        {
            selector.ColorModel = ColorModel.HSL;
            selector.V = slv;
            var r = selector.CurrentColor.R;
            var g = selector.CurrentColor.G;
            var b = selector.CurrentColor.B;
            Assert.That(Math.Round(ColorSelector.ColorSelector.GetLightnessFromRgbByteRange(r, g, b), SignificantDigits), Is.EqualTo(Math.Round(slv, SignificantDigits)));
        }

        [Test]
        public void ValueRange([Range(MinSLV, MaxSLV, 0.001)] double slv)
        {
            selector.ColorModel = ColorModel.HSV;
            selector.V = slv;
            var r = selector.CurrentColor.R;
            var g = selector.CurrentColor.G;
            var b = selector.CurrentColor.B;
            Assert.That(Math.Round(ColorSelector.ColorSelector.GetHsvValueFromRgbByteRange(r, g, b), SignificantDigits), Is.EqualTo(Math.Round(slv, SignificantDigits)));
        }

        [Test]
        public void CurrentColorFromHexadecmical([Random(0x00, 0xff, RgbRand)] byte a, [Random(0x00, 0xff, RgbRand)] byte r, [Random(0x00, 0xff, RgbRand)] byte g, [Random(0x00, 0xff, RgbRand)] byte b)
        {
            selector.HexValueString = $"{a:X2}{r:X2}{g:X2}{b:X2}";
            Assert.Multiple(() =>
            {
                Assert.That(selector.A, Is.EqualTo(a));
                Assert.That(selector.R, Is.EqualTo(r));
                Assert.That(selector.G, Is.EqualTo(g));
                Assert.That(selector.B, Is.EqualTo(b));
            });
        }

        [Test]
        public void CurrentColorFromRgbDoubles([Random(MinRGBA, MaxRGBA, RgbRand)] double a, [Random(MinRGBA, MaxRGBA, RgbRand)] double r, [Random(MinRGBA, MaxRGBA, RgbRand)] double g, [Random(MinRGBA, MaxRGBA, RgbRand)] double b)
        {
            selector.CurrentColor = new RawColor() { A = a, R = r, G = g, B = b };
            selector.ProcessColorChange();
            Assert.Multiple(() =>
            {
                Assert.That(selector.A, Is.EqualTo(a));
                Assert.That(selector.R, Is.EqualTo(r));
                Assert.That(selector.G, Is.EqualTo(g));
                Assert.That(selector.B, Is.EqualTo(b));
            });
        }

        [Test]
        public void CurrentColorFromHslDoubles([Random(MinHue, MaxHue, HslvRand)] double h, [Random(MinSLV, MaxSLV, HslvRand)] double s, [Random(MinSLV, MaxSLV, HslvRand)] double l)
        {
            selector.ColorModel = ColorModel.HSL;
            var rgb = selector.ModelToRgb(h, s, l);
            selector.CurrentColor = new RawColor(selector.A, rgb[0], rgb[1], rgb[2]);
            selector.ProcessColorChange("");
            var r = selector.CurrentColor.R;
            var g = selector.CurrentColor.G;
            var b = selector.CurrentColor.B;

            Assert.Multiple(() =>
            {
                Assert.That(Math.Round(ColorSelector.ColorSelector.GetHueFromRgbByteRange(r, g, b), SignificantDigits), Is.EqualTo(Math.Round(h, SignificantDigits)));
                Assert.That(Math.Round(ColorSelector.ColorSelector.GetHslSaturationFromRgbByteRange(r, g, b), SignificantDigits), Is.EqualTo(Math.Round(s, SignificantDigits)));
                Assert.That(Math.Round(ColorSelector.ColorSelector.GetLightnessFromRgbByteRange(r, g, b), SignificantDigits), Is.EqualTo(Math.Round(l, SignificantDigits)));
            });
        }

        [Test]
        public void CurrentColorFromHsvDoubles([Random(MinHue, MaxHue, HslvRand)] double h, [Random(MinSLV, MaxSLV, HslvRand)] double s, [Random(MinSLV, MaxSLV, HslvRand)] double v)
        {
            selector.ColorModel = ColorModel.HSV;
            var rgb = selector.ModelToRgb(h, s, v);
            selector.CurrentColor = new RawColor(selector.A, rgb[0], rgb[1], rgb[2]);
            selector.ProcessColorChange("");
            var r = selector.CurrentColor.R;
            var g = selector.CurrentColor.G;
            var b = selector.CurrentColor.B;

            Assert.Multiple(() =>
            {
                Assert.That(Math.Round(ColorSelector.ColorSelector.GetHueFromRgbByteRange(r, g, b), SignificantDigits), Is.EqualTo(Math.Round(h, SignificantDigits)));
                Assert.That(Math.Round(ColorSelector.ColorSelector.GetHsvSaturationFromRgbByteRange(r, g, b), SignificantDigits), Is.EqualTo(Math.Round(s, SignificantDigits)));
                Assert.That(Math.Round(ColorSelector.ColorSelector.GetHsvValueFromRgbByteRange(r, g, b), SignificantDigits), Is.EqualTo(Math.Round(v, SignificantDigits)));
            });
        }
    }
}