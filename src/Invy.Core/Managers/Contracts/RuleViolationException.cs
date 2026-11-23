namespace Invy.Core.Managers.Contracts;

/// <summary>The request or configuration breaks a rule, such as an account without a username.</summary>
public sealed class RuleViolationException(string message) : InvyException(message);
