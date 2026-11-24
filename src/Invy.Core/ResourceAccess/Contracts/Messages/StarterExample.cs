using System.Text.Json;

namespace Invy.Core.ResourceAccess.Contracts.Messages;

/// <summary>An example item in a starter pack, with what is inside it. Setup adds them when asked to.</summary>
internal sealed class StarterExample
{
    public string Name { get; set; } = string.Empty;

    /// <summary>The name of one of the pack's types, or null for none.</summary>
    public string? Type { get; set; }

    /// <summary>For an example at the top: the name of the place it goes in, made when it isn't there.</summary>
    public string? Place { get; set; }

    /// <summary>The amount of a counted example, or null for a unique one.</summary>
    public decimal? Quantity { get; set; }

    public string? Unit { get; set; }

    /// <summary>A counted example's low-stock level, or null.</summary>
    public decimal? LowStockAt { get; set; }

    /// <summary>Field values by the field's key, such as price.</summary>
    public Dictionary<string, JsonElement> Fields { get; set; } = [];

    /// <summary>What is inside it.</summary>
    public List<StarterExample> Contents { get; set; } = [];
}
