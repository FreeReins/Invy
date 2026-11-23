namespace Invy.Core.ResourceAccess.Contracts.Messages;

/// <summary>A set of item types a new space can begin with, read from <c>config/types-&lt;key&gt;.json</c>.</summary>
internal sealed class StarterPack
{
    /// <summary>The file name's part after <c>types-</c>, such as home.</summary>
    public string Key { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    /// <summary>The types, as written in the file. Their ids are given when a space is made from them.</summary>
    public List<ItemType> Types { get; set; } = [];
}
