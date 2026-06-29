namespace ZeroTrustSaaS.Domain.Common;

public sealed record Error(
    string Code,
    string Description,
    ErrorType Type)
{
    public static readonly Error None =
        new(
            string.Empty,
            string.Empty,
            ErrorType.None);

    public static Error Validation(
        string code,
        string description)
    {
        return new(
            code,
            description,
            ErrorType.Validation);
    }

    public static Error Failure(
        string code,
        string description)
    {
        return new(
            code,
            description,
            ErrorType.Failure);
    }

    public static Error NotFound(
        string code,
        string description)
    {
        return new(
            code,
            description,
            ErrorType.NotFound);
    }

    public static Error Unauthorized(
        string code,
        string description)
    {
        return new(
            code,
            description,
            ErrorType.Unauthorized);
    }

    public static Error Forbidden(
        string code,
        string description)
    {
        return new(
            code,
            description,
            ErrorType.Forbidden);
    }

    public static Error Conflict(
        string code,
        string description)
    {
        return new(
            code,
            description,
            ErrorType.Conflict);
    }
}

public enum ErrorType
{
    None = 0,

    Validation = 1,

    Failure = 2,

    NotFound = 3,

    Unauthorized = 4,

    Forbidden = 5,

    Conflict = 6
}