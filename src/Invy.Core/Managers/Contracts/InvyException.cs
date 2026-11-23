namespace Invy.Core.Managers.Contracts;

/// <summary>Base type for failures that are reported to the caller with a plain message.</summary>
public abstract class InvyException(string message, Exception? innerException = null)
    : Exception(message, innerException);
