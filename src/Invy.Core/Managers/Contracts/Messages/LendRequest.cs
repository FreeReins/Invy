namespace Invy.Core.Managers.Contracts.Messages;

/// <summary>Lends an item out, whole.</summary>
/// <param name="To">Who or where it goes, as people say it, such as Sam or the church hall.</param>
/// <param name="DueOn">When it should come back, or null for no date.</param>
public sealed record LendRequest(string To, DateOnly? DueOn);
