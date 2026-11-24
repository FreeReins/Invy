namespace Invy.Core.Managers.Contracts.Messages;

/// <summary>The places a starter pack suggests during setup.</summary>
/// <param name="Type">The name of the type places get, such as Room.</param>
/// <param name="Suggest">The names offered to tick, such as Kitchen and Garage.</param>
public sealed record StarterPlacesResponse(string Type, IReadOnlyList<string> Suggest);
