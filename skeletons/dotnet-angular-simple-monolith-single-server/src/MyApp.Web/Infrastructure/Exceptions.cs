namespace MyApp.Web.Infrastructure;

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

public sealed class UnauthorizedException(string detail) : AppException(detail)
{
    public override int StatusCode => StatusCodes.Status401Unauthorized;
    public override string Title => "Unauthorized";
}

public sealed class ForbiddenException(string detail) : AppException(detail)
{
    public override int StatusCode => StatusCodes.Status403Forbidden;
    public override string Title => "Forbidden";
}
