namespace Invy.Core.Managers.Contracts.Messages;

/// <summary>A photo or PDF on the public internet to download and attach to an item, such as a product photo.</summary>
/// <param name="Url">Its http or https address.</param>
/// <param name="Name">What to call it, or null to use the last part of the address.</param>
public sealed record AttachFromWebRequest(string Url, string? Name);
