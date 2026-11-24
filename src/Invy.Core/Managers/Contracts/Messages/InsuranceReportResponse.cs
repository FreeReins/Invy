namespace Invy.Core.Managers.Contracts.Messages;

/// <summary>
/// An insurance inventory: everything that isn't a container (and containers with a value of their own), by room, with
/// photos and values. A room is a container at the top, such as Kitchen or Shed.
/// </summary>
/// <param name="MadeAt">When it was made.</param>
/// <param name="Total">The value of everything listed. Currencies aren't converted.</param>
/// <param name="Count">How many things are listed.</param>
/// <param name="Truncated">True when the space has more than the report lists; the CSV has everything.</param>
/// <param name="Rooms">Each room, by name, then the things at the top outside any room.</param>
public sealed record InsuranceReportResponse(DateTimeOffset MadeAt, decimal Total, int Count, bool Truncated, IReadOnlyList<InsuranceRoom> Rooms);
