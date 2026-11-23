namespace Invy.Core.Managers.Contracts.Messages;

/// <summary>An item named in another answer, such as a container in a location chain.</summary>
/// <param name="Id">The item's id.</param>
/// <param name="Name">What it is called now.</param>
/// <param name="Marking">Whatever is written on it, or null.</param>
public sealed record ItemRef(Guid Id, string Name, string? Marking);
