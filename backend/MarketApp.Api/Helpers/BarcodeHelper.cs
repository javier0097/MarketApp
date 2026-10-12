namespace MarketApp.Api.Helpers;

public static class BarcodeHelper
{
    // Expects only digits and a length of 8, 12, 13 or 14.
    // An 8-digit code can be EAN-8 or UPC-E, and UPC-E computes its check digit on its UPC-A expansion
    public static bool HasValidCheckDigit(string code) =>
        IsCheckDigitValid(code) || (IsUpcE(code) && IsCheckDigitValid(ExpandUpcE(code)));

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
