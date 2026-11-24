namespace Invy.Core.ResourceAccess.Contracts.Messages;

/// <summary><c>going:kitchen</c>: its moving destination has this name, marking or label code.</summary>
internal sealed record GoingTo(string Place) : QueryNode;
