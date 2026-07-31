namespace MyApp.Contracts;

/// <summary>Typed application errors mapped to Problem Details (adrs/dotnet/rfc7807-errors.md).</summary>
public abstract class AppException(string detail) : Exception(detail)
{
    public abstract int StatusCode { get; }
    public abstract string Title { get; }
}

public sealed class NotFoundException(string detail) : AppException(detail)
{
    public override int StatusCode => StatusCodes.Status404NotFound;
    public override string Title => "Not Found";
}

public sealed class ConflictException(string detail) : AppException(detail)
{
    public override int StatusCode => StatusCodes.Status409Conflict;
    public override string Title => "Conflict";
}

internal static class StatusCodes
{
    public const int Status404NotFound = 404;
    public const int Status409Conflict = 409;
}
