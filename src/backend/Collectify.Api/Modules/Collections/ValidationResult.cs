namespace Collectify.Api.Modules.Collections;

public sealed class ValidationResult<T>
{
    private ValidationResult(T? value, Dictionary<string, string[]> errors)
    {
        Value = value;
        Errors = errors;
    }

    public T? Value { get; }
    public Dictionary<string, string[]> Errors { get; }
    public bool IsValid => Errors.Count == 0;

    public static ValidationResult<T> Success(T value)
    {
        return new ValidationResult<T>(value, []);
    }

    public static ValidationResult<T> Failure(Dictionary<string, string[]> errors)
    {
        return new ValidationResult<T>(default, errors);
    }
}
