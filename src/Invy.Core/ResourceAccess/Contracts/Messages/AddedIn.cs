namespace Invy.Core.ResourceAccess.Contracts.Messages;

/// <summary>added: when it was added is in the range.</summary>
internal sealed record AddedIn(Range<DateTimeOffset> Range) : QueryNode;
