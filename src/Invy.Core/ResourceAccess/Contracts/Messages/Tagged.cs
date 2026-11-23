namespace Invy.Core.ResourceAccess.Contracts.Messages;

/// <summary>tag: the item has this tag (tags are kept in lower case).</summary>
internal sealed record Tagged(string Tag) : QueryNode;
