namespace Invy.Core.ResourceAccess.Contracts.Messages;

/// <summary>How many of each kind of record a backup file holds.</summary>
internal sealed record BackupCounts(int Users);
