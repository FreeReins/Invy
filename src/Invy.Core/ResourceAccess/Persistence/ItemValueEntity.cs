namespace Invy.Core.ResourceAccess.Persistence;

internal sealed class ItemValueEntity
{
    public Guid SpaceId { get; set; }

    public Guid ItemId { get; set; }

    public Guid FieldId { get; set; }

    public decimal? Num { get; set; }

    public DateOnly? Date { get; set; }

    public string? Text { get; set; }
}
