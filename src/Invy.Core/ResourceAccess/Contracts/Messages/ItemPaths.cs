namespace Invy.Core.ResourceAccess.Contracts.Messages;

/// <summary>
/// Builds and reads an item's ltree path: its containers' ids and its own, top first, each written as 32 hex
/// digits (ltree labels can't hold dashes on every PostgreSQL), joined by dots.
/// </summary>
internal static class ItemPaths
{
    public static string Label(Guid id) => id.ToString("N");

    /// <summary>The path of an item with this id inside a container with <paramref name="parentPath"/> (null at the top).</summary>
    public static string Of(Guid id, string? parentPath) =>
        string.IsNullOrEmpty(parentPath) ? Label(id) : $"{parentPath}.{Label(id)}";

    /// <summary>The ids in a path, top first.</summary>
    public static IReadOnlyList<Guid> Ids(string path) =>
        string.IsNullOrEmpty(path) ? [] : path.Split('.').Select(label => Guid.ParseExact(label, "N")).ToList();

    /// <summary>The ids of the containers in a path, top first, leaving out the item itself.</summary>
    public static IReadOnlyList<Guid> AncestorIds(string path) => Ids(path).SkipLast(1).ToList();

    /// <summary>True when <paramref name="path"/> is <paramref name="ancestorPath"/> or lies inside it.</summary>
    public static bool IsWithin(string path, string ancestorPath) =>
        path == ancestorPath || path.StartsWith(ancestorPath + ".", StringComparison.Ordinal);

    public static int Depth(string path) => path.Count(c => c == '.');
}
