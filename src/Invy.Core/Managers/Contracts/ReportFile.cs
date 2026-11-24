namespace Invy.Core.Managers.Contracts;

/// <summary>A CSV report to send as a download, written line by line as it is read so a large space never sits in memory.</summary>
/// <param name="Name">The file name to download it as, such as "Home insurance inventory 2026-11-24.csv".</param>
/// <param name="Lines">The file's lines, the header first, each ending in CRLF.</param>
public sealed record ReportFile(string Name, IAsyncEnumerable<string> Lines);
