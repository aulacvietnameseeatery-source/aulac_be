namespace Core.Exception;

/// <summary>
/// Base exception for business rule violations.
/// These exceptions are caught by middleware and converted to user-friendly error responses.
/// </summary>
public class BusinessException : System.Exception
{
    public string ErrorCode { get; }
    public int StatusCode { get; }

    public BusinessException(
        string errorCode,
        string message,
        int statusCode = 400)
      : base(message)
    {
        ErrorCode = errorCode;
        StatusCode = statusCode;
    }
}

/// <summary>
/// Exception thrown when a requested resource is not found.
/// </summary>
public class NotFoundException : BusinessException
{
    public NotFoundException(string message)
        : base("NOT_FOUND", message, 404)
    {
    }
}

/// <summary>
/// Exception thrown when there's a conflict with existing data (e.g., duplicate email).
/// </summary>
public class ConflictException : BusinessException
{
    public ConflictException(string message)
   : base("CONFLICT", message, 409)
    {
    }
}

/// <summary>
/// Exception thrown when the user is not authorized to perform an action.
/// </summary>
public class ForbiddenException : BusinessException
{
    public ForbiddenException(string message)
        : base("FORBIDDEN", message, 403)
    {
    }
}

/// <summary>
/// Exception thrown when business validation fails.
/// </summary>
public class ValidationException : BusinessException
{
    public ValidationException(string message)
        : base("VALIDATION_ERROR", message, 400)
    {
    }
}
