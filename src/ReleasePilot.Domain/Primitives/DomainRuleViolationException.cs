namespace ReleasePilot.Api.Domain.Primitives;

public sealed class DomainRuleViolationException : Exception
{
    public DomainRuleViolationException(string message) : base(message)
    {
    }
}
