namespace Invy.Core.Managers.Contracts.Messages;

/// <summary>One room of an insurance inventory.</summary>
/// <param name="Room">The container at the top, or null for things at the top outside any container.</param>
/// <param name="Value">The value of the things listed in it.</param>
/// <param name="Items">The things in it at any depth, by name, each with its details (fields, dates, photo).</param>
public sealed record InsuranceRoom(ItemRef? Room, decimal Value, IReadOnlyList<SearchResult> Items);
