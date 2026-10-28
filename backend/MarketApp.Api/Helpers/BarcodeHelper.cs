using MarketApp.Api.Exceptions;

namespace MarketApp.Api.Helpers;

public static class BarcodeHelper
{
    public static void Validate(string code, string field)
    {
        if (!IsDigitsOnly(code))
        {
            throw new FieldValidationException(field, "El código de barras solo puede tener números.");
        }

        if (!HasSupportedLength(code))
        {
            throw new FieldValidationException(field, "El código de barras debe tener 8, 12, 13 o 14 dígitos.");
        }

        if (!MatchesCheckDigit(code))
        {
            throw new FieldValidationException(field, "El código de barras no es válido. Revise que esté bien escrito o marque \"Sin código de barras\".");
        }
    }

    // An 8-digit code can be EAN-8 or UPC-E, and UPC-E computes its check digit on its UPC-A expansion
    private static bool MatchesCheckDigit(string code) =>
        IsCheckDigitValid(code) || (IsUpcE(code) && IsCheckDigitValid(ExpandUpcE(code)));

    private static bool IsDigitsOnly(string code) => code is not null && code.All(char.IsAsciiDigit);

    // 8: EAN-8 or UPC-E, 12: UPC-A, 13: EAN-13, 14: GTIN-14
    private static bool HasSupportedLength(string code) => code is { Length: 8 or 12 or 13 or 14 };

    // GS1 check digit, shared by EAN-8, UPC-A, EAN-13 and GTIN-14
    private static bool IsCheckDigitValid(string code)
    {
        var sum = 0;
        var weight = 3;
        for (var i = code.Length - 2; i >= 0; i--)
        {
            sum += (code[i] - '0') * weight;
            weight = weight == 3 ? 1 : 3;
        }

        var checkDigit = (10 - (sum % 10)) % 10;
        return checkDigit == code[^1] - '0';
    }

    private static bool IsUpcE(string code) => code.Length == 8 && code[0] is '0' or '1';

    private static string ExpandUpcE(string code)
    {
        var numberSystem = code[0];
        var digits = code[1..7];
        var checkDigit = code[7];

        var body = digits[5] switch
        {
            '0' or '1' or '2' => $"{digits[..2]}{digits[5]}0000{digits[2..5]}",
            '3' => $"{digits[..3]}00000{digits[3..5]}",
            '4' => $"{digits[..4]}00000{digits[4]}",
            _ => $"{digits[..5]}0000{digits[5]}",
        };

        return $"{numberSystem}{body}{checkDigit}";
    }
}
