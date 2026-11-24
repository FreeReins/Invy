namespace Invy.Core.ResourceAccess.Contracts.Messages;

/// <summary><c>lent:sam</c>: it is lent to someone or somewhere whose name contains the text.</summary>
internal sealed record LentTo(string Text) : QueryNode;
