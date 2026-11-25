namespace Invy.Core.Tests;

/// <summary>A row of the JSON that <see cref="JsonStreamReaderTests"/> reads.</summary>
public sealed record StreamRow(string Name, string? Notes);
