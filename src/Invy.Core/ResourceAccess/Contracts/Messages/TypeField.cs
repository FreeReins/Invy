using Invy.Core.Managers.Contracts.Messages;

namespace Invy.Core.ResourceAccess.Contracts.Messages;

/// <summary>One custom field of an item type, as stored in the type's <c>Fields</c> jsonb.</summary>
internal sealed class TypeField
{
    /// <summary>Given by ValidationEngine.TypeDraft: kept from the request, or new.</summary>
    public Guid Id { get; set; }

    public string Key { get; set; } = string.Empty;

    public string Label { get; set; } = string.Empty;

    public FieldKind Kind { get; set; }

    public string? Unit { get; set; }

    public List<string> Options { get; set; } = [];

    public bool Required { get; set; }

    public bool Sortable { get; set; }

    /// <summary>Date fields only: remind this many days ahead, or null for no reminder.</summary>
    public int? Remind { get; set; }

    /// <summary>Date fields with a reminder only: how many months Done moves an upkeep date on, or null.</summary>
    public int? RepeatMonths { get; set; }
}
