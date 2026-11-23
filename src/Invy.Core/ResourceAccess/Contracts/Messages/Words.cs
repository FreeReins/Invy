namespace Invy.Core.ResourceAccess.Contracts.Messages;

/// <summary>A plain word or quoted phrase: found in the name, marking, tags, notes or text fields, by prefix or by a near spelling.</summary>
internal sealed record Words(string Text) : QueryNode;
