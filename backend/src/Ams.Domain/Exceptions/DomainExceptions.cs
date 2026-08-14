namespace Ams.Domain.Exceptions;

/// <summary>Base type for expected, rule-driven failures. Mapped to HTTP status by middleware.</summary>
public abstract class DomainException(string message) : Exception(message)
{
    public abstract string ErrorCode { get; }
}

/// <summary>A requested entity does not exist. → 404</summary>
public sealed class NotFoundException(string message) : DomainException(message)
{
    public override string ErrorCode => "not_found";
}

/// <summary>The caller is authenticated but not permitted to act on this resource. → 403</summary>
public sealed class ForbiddenException(string message) : DomainException(message)
{
    public override string ErrorCode => "forbidden";
}

/// <summary>The request breaks a business rule given current state. → 409</summary>
public sealed class BusinessRuleException(string message) : DomainException(message)
{
    public override string ErrorCode => "business_rule_violation";
}

/// <summary>The request is malformed or fails validation. → 400</summary>
public sealed class ValidationFailedException(string message) : DomainException(message)
{
    public override string ErrorCode => "validation_failed";
}
