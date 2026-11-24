namespace Invy.Core.ResourceAccess.Contracts.Messages;

/// <summary><c>project:bookshelf</c>: an open project with this name sets some of it aside.</summary>
internal sealed record ReservedFor(string Project) : QueryNode;
