namespace Invy.Core.ResourceAccess.Contracts.Messages;

/// <summary>An amount of one item set aside for a project. A unique item's amount is 1.</summary>
internal sealed class ProjectItem
{
    public Guid ItemId { get; set; }

    public decimal Amount { get; set; }
}
