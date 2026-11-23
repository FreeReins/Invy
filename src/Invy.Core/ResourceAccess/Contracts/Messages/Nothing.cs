namespace Invy.Core.ResourceAccess.Contracts.Messages;

/// <summary>Nothing matches, such as has:photo before photos exist.</summary>
internal sealed record Nothing : QueryNode;
