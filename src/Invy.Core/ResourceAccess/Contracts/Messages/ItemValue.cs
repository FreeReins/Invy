namespace Invy.Core.ResourceAccess.Contracts.Messages;

/// <summary>A typed copy of one sortable field value of an item, for ranges and sorting. Exactly one value is set.</summary>
internal sealed class ItemValue
{
    public Guid SpaceId { get; set; }

    public Guid ItemId { get; set; }

    public Guid FieldId { get; set; }

    /// <summary>Numbers, money, and yes (1) or no (0).</summary>
    public decimal? Num { get; set; }

    public DateOnly? Date { get; set; }

    /// <summary>Text, choices, web addresses, barcodes and people, in lower case.</summary>
    public string? Text { get; set; }
}
