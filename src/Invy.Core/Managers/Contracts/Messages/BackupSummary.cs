namespace Invy.Core.Managers.Contracts.Messages;

/// <summary>What a backup holds: how many of each kind of record a restore would bring back.</summary>
/// <param name="Users">The accounts.</param>
public sealed record BackupSummary(int Users);
