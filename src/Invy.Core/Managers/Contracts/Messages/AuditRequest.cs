namespace Invy.Core.Managers.Contracts.Messages;

/// <summary>What was found in a container during an audit walk.</summary>
/// <param name="Present">The items ticked or scanned as there: usually its contents, but anything scanned counts.</param>
public sealed record AuditRequest(IReadOnlyList<Guid> Present);
