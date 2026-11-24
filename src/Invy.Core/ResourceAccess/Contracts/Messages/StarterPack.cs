namespace Invy.Core.ResourceAccess.Contracts.Messages;

/// <summary>A set of item types a new space can begin with, read from <c>config/types-&lt;key&gt;.json</c>.</summary>
internal sealed class StarterPack
{
    /// <summary>The file name's part after <c>types-</c>, such as home.</summary>
    public string Key { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    /// <summary>A generic icon name for the kind's card, such as house, or null.</summary>
    public string? Icon { get; set; }

    /// <summary>The features a space of this kind starts with.</summary>
    public List<string> Features { get; set; } = [];

    /// <summary>The types, as written in the file. Their ids are given when a space is made from them.</summary>
    public List<ItemType> Types { get; set; } = [];

    /// <summary>The places setup suggests, or null when the pack suggests none.</summary>
    public StarterPlaces? Places { get; set; }

    /// <summary>Example items setup can add, each with what is inside it.</summary>
    public List<StarterExample> Examples { get; set; } = [];

    /// <summary>The keys of the steps on the getting-started card (see <c>GettingStartedSteps</c>), in order.</summary>
    public List<string> GettingStarted { get; set; } = [];

    /// <summary>Text for empty screens, by tab (browse, scan, types, find).</summary>
    public Dictionary<string, string> Hints { get; set; } = [];
}
