using System.Diagnostics;
using Xunit.Abstractions;
using ColorSelector;
using Xunit;
using Xunit.Sdk;
using System.Windows.Threading;
using System.Windows;
using System.Globalization;
using System;

namespace ColorSelectorXunitTests
{
    public class ValidationTests
    {
        private readonly ITestOutputHelper testOutputHelper;
        private ColorSelector.ColorSelector? selector;
        private readonly ColorSelector.Validators.ColorByteStringValidationRule byteValidator;
        public static Random rand = new Random();
        public static int MemberDataCount = 20;

        public ValidationTests(ITestOutputHelper output)
        {
            testOutputHelper = output;

            byteValidator = new();
        }

        public static TheoryData<string> GoodNumericByteStrings()
        {
            TheoryData<string> data = new TheoryData<string>();
            foreach (var _ in Enumerable.Range(start: 1, count: MemberDataCount))
            {
                data.Add(rand.Next(Byte.MinValue, Byte.MaxValue).ToString());
            }
            return data;
        }

        public static TheoryData<string> BadNumericByteStrings()
        {
            TheoryData<string> data = new TheoryData<string>();
            foreach (var i in Enumerable.Range(start: 1, count: MemberDataCount))
            {
                string randomString = "";

                // generate a random string between 1 and 16 random characters in length:
                foreach (var j in Enumerable.Range(1, rand.Next(1, 16)))
                    randomString += (char)rand.Next(char.MinValue, char.MaxValue);

                data.Add(randomString);
            }
            return data;
        }

        public static TheoryData<string> RangedNumericByteStrings()
        {
            TheoryData<string> data = new TheoryData<string>();
            foreach (var _ in Enumerable.Range(start: 1, count: MemberDataCount))
            {
                data.Add(rand.Next(Byte.MinValue - 100, Byte.MaxValue + 100).ToString());
            }
            return data;
        }

        [Theory]
        [MemberData(nameof(GoodNumericByteStrings))]
        public void ArgbValidatorAlwaysTrue(string value)
        {
            Assert.True(byteValidator.Validate(value, CultureInfo.CurrentCulture).IsValid);
        }

        [Theory]
        [MemberData(nameof(BadNumericByteStrings))]
        public void ArgbValidatorRandomStrings(string value)
        {
            var num = int.MinValue;
            if (Int32.TryParse(value, out num))
            {
                if (Byte.MinValue <= num && num <= Byte.MaxValue)
                    Assert.True(byteValidator.Validate(value, CultureInfo.CurrentCulture).IsValid);
            }
            else
                Assert.False(byteValidator.Validate(value, CultureInfo.CurrentCulture).IsValid);
        }

        [Theory]
        [MemberData(nameof(RangedNumericByteStrings))]
        public void ArgbValidatorRangeChecking(string value)
        {
            var num = int.Parse(value);
            if (Byte.MinValue <= num && num <= Byte.MaxValue)
                Assert.True(byteValidator.Validate(value, CultureInfo.CurrentCulture).IsValid);
            else
                Assert.False(byteValidator.Validate(value, CultureInfo.CurrentCulture).IsValid);
        }

        [StaFact]
        public void VerifyARGBComponentDefines()
        {
            var selector = new ColorSelector.ColorSelector();
            selector.A = 255.0;
            Assert.Equal(255.0, selector.A);
            selector.R = 255.0;
            Assert.Equal(255.0, selector.R);
            selector.G = 255.0;
            Assert.Equal(255.0, selector.G);
            selector.B = 255.0;
            Assert.Equal(255.0, selector.B);
        }
    }
}