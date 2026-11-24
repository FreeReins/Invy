namespace Invy.Core.ResourceAccess.Contracts.Messages;

/// <summary>The places a starter pack suggests during setup, such as rooms for a home.</summary>
internal sealed class StarterPlaces
{
    /// <summary>The name of the pack's type that places get, such as Room.</summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>The names offered as chips to tick, such as Kitchen and Garage.</summary>
    public List<string> Suggest { get; set; } = [];
}
