namespace BotForge.Routing.Abstractions;

/// <summary>
/// Validates user input and returns a typed result.
/// </summary>
public interface IInputValidator<T>
{
    ValidationResult<T> Validate(string rawInput);
}

/// <summary>
/// Result of input validation.
/// </summary>
public class ValidationResult<T>
{
    public bool IsValid { get; init; }
    public T? Value { get; init; }
    public string? ErrorMessage { get; init; }

    public static ValidationResult<T> Success(T value)
    {
        return new ValidationResult<T> { IsValid = true, Value = value };
    }

    public static ValidationResult<T> Error(string message)
    {
        return new ValidationResult<T> { IsValid = false, ErrorMessage = message };
    }
}
