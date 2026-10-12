namespace MarketApp.Api.Exceptions;

public class FieldValidationException(string field, string message) : Exception(message)
{
    public string Field { get; } = field;
}
