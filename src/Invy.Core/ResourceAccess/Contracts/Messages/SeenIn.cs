namespace Invy.Core.ResourceAccess.Contracts.Messages;

/// <summary>seen: when it was last seen is in the range; <paramref name="OrNever"/> also matches items never seen.</summary>
internal sealed record SeenIn(Range<DateTimeOffset> Range, bool OrNever) : QueryNode;
