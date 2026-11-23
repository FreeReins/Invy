namespace Invy.Core.Managers.Contracts;

/// <summary>A web page or photo on the web couldn't be downloaded.</summary>
public sealed class WebSourceException(string message, Exception? innerException = null)
    : InvyException(message, innerException);
