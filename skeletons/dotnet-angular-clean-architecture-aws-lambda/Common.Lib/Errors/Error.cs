namespace Common.Lib.Errors;

public enum ErrorType
{
    None = 0,
    Validation,
    NotFound,
    Conflict,
    Failure,
}

public sealed record Error(string Code, string Message, ErrorType Type)
{
    public static readonly Error None = new(string.Empty, string.Empty, ErrorType.None);
}

/// <summary>Shared error factories (family-B convention: GenericErrors lives in Common.Lib/Errors).</summary>
public static class GenericErrors
{
    public static Error NotFound(string entity, Guid id) =>
        new($"{entity}.NotFound", $"{entity} {id} was not found.", ErrorType.NotFound);

    public static Error Conflict(string code, string message) =>
        new(code, message, ErrorType.Conflict);

    public static Error Validation(string code, string message) =>
        new(code, message, ErrorType.Validation);
}
