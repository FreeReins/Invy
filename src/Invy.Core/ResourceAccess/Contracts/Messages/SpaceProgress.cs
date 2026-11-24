namespace Invy.Core.ResourceAccess.Contracts.Messages;

/// <summary>What a space has so far, for its getting-started card. Examples don't count.</summary>
internal sealed class SpaceProgress
{
    /// <summary>A container at the top that isn't an example.</summary>
    public bool HasPlaces { get; set; }

    /// <summary>Something of the person's own inside a container.</summary>
    public bool HasPutAway { get; set; }

    /// <summary>Something of the person's own with a value.</summary>
    public bool HasValues { get; set; }

    /// <summary>Something of the person's own marked as seen.</summary>
    public bool HasSeen { get; set; }

    /// <summary>How many examples are left.</summary>
    public int ExamplesLeft { get; set; }
}
