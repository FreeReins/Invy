namespace Invy.Core.Managers.Contracts.Messages;

/// <summary>A row an import skipped, and why.</summary>
/// <param name="Row">The row in the file, counting the heading row as 1.</param>
/// <param name="Problem">What was wrong with it.</param>
public sealed record ImportProblem(int Row, string Problem);
