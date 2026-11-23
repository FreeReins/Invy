namespace Invy.Core.ResourceAccess.Contracts.Messages;

/// <summary>How many items a search matches.</summary>
/// <param name="Count">The number, exact up to the cap asked for, an estimate above it.</param>
/// <param name="Estimated">True when <paramref name="Count"/> is the database's estimate.</param>
internal sealed record SearchCount(int Count, bool Estimated);
