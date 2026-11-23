namespace Invy.Core.Managers.Contracts;

/// <summary>The requested account, link, backup or other record doesn't exist.</summary>
public sealed class NotFoundException(string message) : InvyException(message);
