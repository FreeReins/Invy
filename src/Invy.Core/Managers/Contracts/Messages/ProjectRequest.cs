namespace Invy.Core.Managers.Contracts.Messages;

/// <summary>A project's name and notes, to start one or change them.</summary>
/// <param name="Name">What it is, such as Build a bookshelf.</param>
/// <param name="Notes">Free text, or null.</param>
/// <param name="Version">When changing: the version that was opened. Ignored when starting one.</param>
public sealed record ProjectRequest(string Name, string? Notes, int Version);
