using MarketApp.Api.Exceptions;
using MarketApp.Api.Helpers;

namespace MarketApp.Api.Tests.UnitTests;

public class BarcodeHelperTests
{
    private const string Field = "barcode";

    private const string OnlyDigitsMessage = "El código de barras solo puede tener números.";
    private const string LengthMessage = "El código de barras debe tener 8, 12, 13 o 14 dígitos.";
    private const string InvalidCheckDigitMessage =
        "El código de barras no es válido. Revise que esté bien escrito o marque \"Sin código de barras\".";

    // ---------- Valid codes ----------

    // EAN-8 96385074: 9*3 + 6*1 + 3*3 + 8*1 + 5*3 + 0*1 + 7*3 = 86 -> check (10 - 6) = 4
    [Fact]
    public void Accepts_a_valid_EAN8_barcode()
    {
        var exception = Record.Exception(() => BarcodeHelper.Validate("96385074", Field));

        Assert.Null(exception);
    }

    // EAN-8 04252610: 0*3 + 4*1 + 2*3 + 5*1 + 2*3 + 6*1 + 1*3 = 30 -> check 0  (valid EAN-8)
    // As UPC-E it expands to 042100005264, whose check is 4, not 0             (invalid UPC-E)
    [Fact]
    public void Accepts_an_8_digit_barcode_that_is_a_valid_EAN8_but_not_a_valid_UPCE()
    {
        var exception = Record.Exception(() => BarcodeHelper.Validate("04252610", Field));

        Assert.Null(exception);
    }

    // Each code is a valid UPC-E and an invalid EAN-8: one per expansion rule where UPC-E and EAN-8 can differ (last digit 0–2, 3, 4), plus first digit 1
    // (the UPC-E rule applies whatever the first digit is).
    // UPC-A check: odd positions * 3 + even positions, over the 11 expanded digits.
    //
    // 01234505  last digit 0 -> 0 12000 00345: odd 0+2+0+0+3+5 = 10*3 = 30, even 1+0+0+0+4 = 5,  total 35 -> 5. EAN-8 expects 3.
    // 04252614  last digit 1 -> 0 42100 00526: odd 0+2+0+0+5+6 = 13*3 = 39, even 4+1+0+0+2 = 7,  total 46 -> 4. EAN-8 expects 0.
    // 01234531  last digit 3 -> 0 12300 00045: odd 0+2+0+0+0+5 =  7*3 = 21, even 1+3+0+0+4 = 8,  total 29 -> 1. EAN-8 expects 4.
    // 01234543  last digit 4 -> 0 12340 00005: odd 0+2+4+0+0+5 = 11*3 = 33, even 1+3+0+0+0 = 4,  total 37 -> 3. EAN-8 expects 1.
    // 12345610  first digit 1 -> 1 23100 00456: odd 1+3+0+0+4+6 = 14*3 = 42, even 2+1+0+0+5 = 8,  total 50 -> 0. EAN-8 expects 8.
    [Theory]
    [InlineData("01234505")]
    [InlineData("04252614")]
    [InlineData("01234531")]
    [InlineData("01234543")]
    [InlineData("12345610")]
    public void Accepts_an_8_digit_barcode_that_is_a_valid_UPCE_but_not_a_valid_EAN8(string code)
    {
        var exception = Record.Exception(() => BarcodeHelper.Validate(code, Field));

        Assert.Null(exception);
    }

    // UPC-E only exists for first digit 0 or 1. These codes would pass the UPC-E calculation,
    // but they start with 2 and 9 and are not valid EAN-8, so they must be rejected.
    //   21234518: UPC-E calculation gives 8 (2 12100 00345: 36 + 6 = 42), EAN-8 expects 4.
    //   91234508: UPC-E calculation gives 8 (9 12000 00345: 57 + 5 = 62), EAN-8 expects 6.
    [Theory]
    [InlineData("21234518")]
    [InlineData("91234508")]
    public void Rejects_an_8_digit_barcode_starting_with_2_to_9_that_only_passes_the_UPCE_calculation(string code)
    {
        var exception = Assert.Throws<FieldValidationException>(() => BarcodeHelper.Validate(code, Field));

        Assert.Equal(InvalidCheckDigitMessage, exception.Message);
    }

    // UPC-A 036000291452: odd 0+6+0+2+1+5 = 14 * 3 = 42, even 3+0+0+9+4 = 16, total 58 -> check 2
    [Fact]
    public void Accepts_a_valid_UPCA_barcode()
    {
        var exception = Record.Exception(() => BarcodeHelper.Validate("036000291452", Field));

        Assert.Null(exception);
    }

    // EAN-13 4006381333931: weights 1,3,... -> 4+0+0+18+3+24+1+9+3+9+9+9 = 89 -> check 1
    // EAN-13 5901234123457: 5+27+0+3+2+9+4+3+2+9+4+15 = 83 -> check 7
    [Theory]
    [InlineData("4006381333931")]
    [InlineData("5901234123457")]
    public void Accepts_a_valid_EAN13_barcode(string code)
    {
        var exception = Record.Exception(() => BarcodeHelper.Validate(code, Field));

        Assert.Null(exception);
    }

    // EAN-13 5901234123150: 5+27+0+3+2+9+4+3+2+9+1+15 = 80 -> sum % 10 == 0 -> check 0 (not 10)
    [Fact]
    public void Accepts_a_barcode_whose_check_digit_is_zero()
    {
        var exception = Record.Exception(() => BarcodeHelper.Validate("5901234123150", Field));

        Assert.Null(exception);
    }

    // GTIN-14 10012345678902: weights 3,1,... -> 3+0+0+1+6+3+12+5+18+7+24+9+0 = 88 -> check 2
    [Fact]
    public void Accepts_a_valid_GTIN14_barcode()
    {
        var exception = Record.Exception(() => BarcodeHelper.Validate("10012345678902", Field));

        Assert.Null(exception);
    }

    // Each code is a valid one above with the last digit changed.
    // The 8-digit ones fail both rules:
    //   96385075: EAN-8 expects 4; not UPC-E (starts with 9).
    //   01234566: EAN-8 expects 5, UPC-E expects 5.
    //   04252612: EAN-8 expects 0, UPC-E expects 4.
    //   12345612: EAN-8 expects 8, UPC-E expects 0.
    //   21234519: EAN-8 expects 4; not UPC-E (starts with 2).
    [Theory]
    [InlineData("96385075")]
    [InlineData("01234566")]
    [InlineData("04252612")]
    [InlineData("12345612")]
    [InlineData("21234519")]
    [InlineData("036000291453")]
    [InlineData("4006381333932")]
    [InlineData("10012345678903")]
    public void Rejects_a_barcode_with_a_wrong_check_digit(string code)
    {
        var exception = Assert.Throws<FieldValidationException>(() => BarcodeHelper.Validate(code, Field));

        Assert.Equal(InvalidCheckDigitMessage, exception.Message);
    }

    // 4006381333391 swaps the 11th and 12th digits of 4006381333931.
    // 4+0+0+18+3+24+1+9+3+9+3+27 = 101 -> expected check 9, not 1
    [Fact]
    public void Rejects_a_barcode_with_two_swapped_digits()
    {
        var exception = Assert.Throws<FieldValidationException>(
            () => BarcodeHelper.Validate("4006381333391", Field));

        Assert.Equal(InvalidCheckDigitMessage, exception.Message);
    }

    // All have 13 characters, so the length rule cannot be the cause.
    [Theory]
    [InlineData("400638133393A")]
    [InlineData("4006-81333931")]
    [InlineData("4006.81333931")]
    [InlineData("-400638133393")]
    [InlineData("+400638133393")]
    public void Rejects_a_barcode_with_non_digit_characters(string code)
    {
        var exception = Assert.Throws<FieldValidationException>(() => BarcodeHelper.Validate(code, Field));

        Assert.Equal(OnlyDigitsMessage, exception.Message);
    }

    // Spaces are part of the code: they are not trimmed, so they count as non-digit characters.
    // All have a valid length (13, or 8 for the spaces-only one).
    [Theory]
    [InlineData("4006 81333931")]
    [InlineData(" 400638133393")]
    [InlineData("400638133393 ")]
    [InlineData("        ")]
    public void Rejects_a_barcode_containing_spaces(string code)
    {
        var exception = Assert.Throws<FieldValidationException>(() => BarcodeHelper.Validate(code, Field));

        Assert.Equal(OnlyDigitsMessage, exception.Message);
    }

    // The empty string has no non-digit characters, so only the length rule applies.
    [Theory]
    [InlineData("")]
    [InlineData("1")]
    [InlineData("9638507")]
    [InlineData("963850749")]
    [InlineData("9638507490")]
    [InlineData("03600029145")]
    [InlineData("100123456789020")]
    public void Rejects_a_barcode_with_a_length_other_than_8_12_13_or_14_digits(string code)
    {
        var exception = Assert.Throws<FieldValidationException>(() => BarcodeHelper.Validate(code, Field));

        Assert.Equal(LengthMessage, exception.Message);
    }

    [Fact]
    public void Reports_the_given_field_name_in_the_exception()
    {
        var exception = Assert.Throws<FieldValidationException>(
            () => BarcodeHelper.Validate("4006381333932", "productBarcode"));

        Assert.Equal("productBarcode", exception.Field);
    }
}
