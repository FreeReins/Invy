namespace Invy.Core.Managers.Contracts.Messages;

/// <summary>A type to copy from a starter pack into a space.</summary>
/// <param name="Pack">The pack's key, such as hobby.</param>
/// <param name="Type">The name of one of its types, such as Kit.</param>
public sealed record PackTypeRequest(string Pack, string Type);
