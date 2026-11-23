namespace Invy.Core.ResourceAccess.Contracts.Messages;

/// <summary>name:, marking: or notes: that column contains the text.</summary>
internal sealed record Contains(ItemText Column, string Text) : QueryNode;
