namespace Invy.Core.Managers.Contracts;

/// <summary>The request is valid but clashes with the current state, such as a type someone else just changed.</summary>
public sealed class ConflictException(string message, Exception? innerException = null) : InvyException(message, innerException);
