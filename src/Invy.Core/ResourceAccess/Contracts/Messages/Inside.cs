namespace Invy.Core.ResourceAccess.Contracts.Messages;

/// <summary>in: the item is somewhere inside a container with this name or marking.</summary>
internal sealed record Inside(string Container) : QueryNode;
