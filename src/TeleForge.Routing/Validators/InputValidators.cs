using TeleForge.Routing.Abstractions;

namespace TeleForge.Routing.Validators;

/// <summary>
/// Validates that input is a positive integer within a specified range.
/// </summary>
public class AmountValidator(int min = 1, int max = int.MaxValue) : IInputValidator<int>
{
    public ValidationResult<int> Validate(string rawInput)
    {
        ArgumentNullException.ThrowIfNull(rawInput);

        if (!int.TryParse(rawInput.Trim(), out var value))
            return ValidationResult<int>.Error("Please enter a valid number.");

        if (value < min || value > max)
            return ValidationResult<int>.Error($"Please enter a number between {min} and {max}.");

        return ValidationResult<int>.Success(value);
    }
}

/// <summary>
/// Validates that input is a number within a specified range.
/// </summary>
public class RangeValidator(decimal min, decimal max) : IInputValidator<decimal>
{
    public ValidationResult<decimal> Validate(string rawInput)
    {
        ArgumentNullException.ThrowIfNull(rawInput);

        if (!decimal.TryParse(rawInput.Trim(), out var value))
            return ValidationResult<decimal>.Error("Please enter a valid number.");

        if (value < min || value > max)
            return ValidationResult<decimal>.Error($"Please enter a number between {min} and {max}.");

        return ValidationResult<decimal>.Success(value);
    }
}

/// <summary>
/// Validates that input looks like a valid email address.
/// </summary>
public class EmailValidator : IInputValidator<string>
{
    public ValidationResult<string> Validate(string rawInput)
    {
        ArgumentNullException.ThrowIfNull(rawInput);

        var trimmed = rawInput.Trim();
        if (string.IsNullOrEmpty(trimmed) ||
            !trimmed.Contains('@') ||
            !trimmed.Contains('.') ||
            trimmed.Length < 5)
            return ValidationResult<string>.Error("Please enter a valid email address.");

        return ValidationResult<string>.Success(trimmed);
    }
}

/// <summary>
/// Validates that input is non-empty text within length limits.
/// </summary>
public class TextLengthValidator(int minLength = 1, int maxLength = 4096) : IInputValidator<string>
{
    public ValidationResult<string> Validate(string rawInput)
    {
        ArgumentNullException.ThrowIfNull(rawInput);

        var trimmed = rawInput.Trim();
        if (trimmed.Length < minLength)
            return ValidationResult<string>.Error($"Please enter at least {minLength} characters.");

        if (trimmed.Length > maxLength)
            return ValidationResult<string>.Error($"Please enter at most {maxLength} characters.");

        return ValidationResult<string>.Success(trimmed);
    }
}
