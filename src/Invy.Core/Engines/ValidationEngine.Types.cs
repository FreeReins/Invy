using System.Text.RegularExpressions;
using Invy.Core.Managers.Contracts;
using Invy.Core.Managers.Contracts.Messages;
using Invy.Core.ResourceAccess.Contracts.Messages;
using Invy.Core.Utilities;

namespace Invy.Core.Engines;

/// <content>Checks on item types and their field definitions.</content>
internal sealed partial class ValidationEngine
{
    public const int TypeNameLength = 80;

    public const int FieldLabelLength = 80;

    public const int MaxFields = 100;

    public const int MaxOptions = 200;

    public const int OptionLength = 80;

    public const int IconLength = 40;

    public const int UnitLength = 20;

    /// <summary>Words search already uses before a colon, so a field can't be named after one.</summary>
    public static readonly IReadOnlySet<string> ReservedFieldKeys =
        new HashSet<string>(["type", "in", "is", "has", "tag", "seen", "added", "qty", "name", "notes", "marking", "code", "status", "lent", "going", "due", "project"]);

    private static readonly Regex ValidFieldKey = new("^[a-z][a-z0-9_]{0,39}$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex ValidColour = new("^#[0-9a-fA-F]{6}$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    /// <summary>The most days ahead a date field can remind.</summary>
    public const int MaxRemindDays = 365;

    /// <summary>The longest repeat of an upkeep date, in months.</summary>
    public const int MaxRepeatMonths = 120;

    /// <summary>Kinds whose values fit in <c>item_values</c>' num, date or text column.</summary>
    private static readonly IReadOnlySet<FieldKind> SortableKinds = new HashSet<FieldKind>(
        [FieldKind.Text, FieldKind.Number, FieldKind.Money, FieldKind.Date, FieldKind.Bool, FieldKind.Select, FieldKind.Url, FieldKind.Barcode, FieldKind.Person]);

    public async Task<ItemType> TypeAsync(Guid spaceId, Guid id, CancellationToken cancellationToken) =>
        await types.FindAsync(spaceId, id, cancellationToken)
        ?? throw new NotFoundException("That type no longer exists. Reload the Types tab to see the current ones.");

    /// <summary>Checks that no other type in the space has the name, ignoring case.</summary>
    public async Task UniqueTypeNameAsync(Guid spaceId, string name, Guid id, CancellationToken cancellationToken)
    {
        var taken = (await types.ListAsync(spaceId, cancellationToken))
            .Any(type => type.Id != id && string.Equals(type.Name, name, StringComparison.CurrentCultureIgnoreCase));
        if (taken)
        {
            throw new ConflictException($"There is already a type called {name}. Pick another name.");
        }
    }

    /// <summary>
    /// The type a request describes, checked and tidied: trimmed names, field keys unique, options only on select
    /// fields. Fields keep the id they were sent with; new ones get one.
    /// </summary>
    public static ItemType TypeDraft(Guid spaceId, Guid id, ItemTypeRequest request)
    {
        var name = Trimmed(request.Name);
        if (name.Length is 0 or > TypeNameLength)
        {
            throw new RuleViolationException($"A type needs a name of 1 to {TypeNameLength} characters.");
        }

        var icon = Optional(request.Icon);
        if (icon?.Length > IconLength)
        {
            throw new RuleViolationException($"An icon name can have up to {IconLength} characters.");
        }

        var colour = Optional(request.Colour);
        if (colour is not null && !ValidColour.IsMatch(colour))
        {
            throw new RuleViolationException("A colour is written like #3b82f6. Pick one from the colour list.");
        }

        var fields = request.Fields ?? [];
        if (fields.Count > MaxFields)
        {
            throw new RuleViolationException($"A type can have up to {MaxFields} fields.");
        }

        var checkedFields = fields.Select(Field).ToList();
        if (checkedFields.GroupBy(field => field.Key).FirstOrDefault(group => group.Count() > 1) is { } sameKey)
        {
            throw new RuleViolationException($"Two fields are called {sameKey.Key}. Give each field its own search name.");
        }

        if (checkedFields.GroupBy(field => field.Id).Any(group => group.Count() > 1))
        {
            throw new RuleViolationException("Two fields have the same id. Reload the type and try again.");
        }

        return new ItemType
        {
            Id = id,
            SpaceId = spaceId,
            Name = name,
            Icon = icon,
            Colour = colour,
            HoldsItems = request.HoldsItems,
            Fields = checkedFields,
        };
    }

    /// <summary>True when a suggested field would pass the checks the Types editor's save makes.</summary>
    public static bool FieldFits(FieldDefinition field)
    {
        try
        {
            Field(field);
            return true;
        }
        catch (RuleViolationException)
        {
            return false;
        }
    }

    private static TypeField Field(FieldDefinition field)
    {
        var label = Trimmed(field.Label);
        if (label.Length is 0 or > FieldLabelLength)
        {
            throw new RuleViolationException($"Each field needs a name of 1 to {FieldLabelLength} characters.");
        }

        var key = Trimmed(field.Key).ToLowerInvariant();
        if (key.Length == 0)
        {
            key = FieldKeys.FromLabel(label) ?? string.Empty;
        }

        if (!ValidFieldKey.IsMatch(key))
        {
            throw new RuleViolationException(
                $"The search name of {label} needs to start with a letter and use only lowercase letters, numbers and underscores (up to 40).");
        }

        if (ReservedFieldKeys.Contains(key))
        {
            throw new RuleViolationException($"Search already uses {key}: for something else. Give {label} another search name.");
        }

        if (!Enum.IsDefined(field.Kind))
        {
            throw new RuleViolationException($"Pick what kind of value {label} holds.");
        }

        var unit = Optional(field.Unit);
        if (unit is not null && field.Kind is not (FieldKind.Number or FieldKind.Money))
        {
            throw new RuleViolationException($"Only number and money fields have a unit. Clear the unit of {label}.");
        }

        if (unit?.Length > UnitLength)
        {
            throw new RuleViolationException($"A unit can have up to {UnitLength} characters.");
        }

        var options = Options(label, field.Kind, field.Options ?? []);
        if (field.Sortable && !SortableKinds.Contains(field.Kind))
        {
            throw new RuleViolationException($"{label} can't be sorted on: long text, multiselect and item fields aren't sortable.");
        }

        if ((field.Remind is not null || field.RepeatMonths is not null) && field.Kind != FieldKind.Date)
        {
            throw new RuleViolationException($"Only date fields can remind you. Clear the reminder of {label}.");
        }

        if (field.Remind is < 0 or > MaxRemindDays)
        {
            throw new RuleViolationException($"{label} can remind you from 0 to {MaxRemindDays} days ahead.");
        }

        if (field.RepeatMonths is not null && (field.Remind is null || field.RepeatMonths is < 1 or > MaxRepeatMonths))
        {
            throw new RuleViolationException(
                $"{label} can repeat every 1 to {MaxRepeatMonths} months, and only when it reminds you. Set how many days ahead to remind, or clear the repeat.");
        }

        return new TypeField
        {
            Id = field.Id is Guid fieldId && fieldId != Guid.Empty ? fieldId : Guid.CreateVersion7(),
            Key = key,
            Label = label,
            Kind = field.Kind,
            Unit = unit,
            Options = options,
            Required = field.Required,
            // A reminder date is found through item_values, so it is always kept sortable.
            Sortable = field.Sortable || field.Remind is not null,
            Remind = field.Remind,
            RepeatMonths = field.RepeatMonths,
        };
    }

    private static List<string> Options(string label, FieldKind kind, IReadOnlyList<string> options)
    {
        var trimmed = options.Select(Trimmed).ToList();
        if (kind is not (FieldKind.Select or FieldKind.MultiSelect))
        {
            return trimmed.Count == 0
                ? []
                : throw new RuleViolationException($"Only select fields have options. Clear the options of {label}.");
        }

        if (trimmed.Count == 0)
        {
            throw new RuleViolationException($"{label} needs at least one option to choose from.");
        }

        if (trimmed.Count > MaxOptions)
        {
            throw new RuleViolationException($"{label} can have up to {MaxOptions} options.");
        }

        if (trimmed.Any(option => option.Length is 0 or > OptionLength))
        {
            throw new RuleViolationException($"Each option of {label} needs 1 to {OptionLength} characters.");
        }

        if (trimmed.GroupBy(option => option, StringComparer.CurrentCultureIgnoreCase).FirstOrDefault(group => group.Count() > 1) is { } twice)
        {
            throw new RuleViolationException($"{label} has the option {twice.Key} twice. Remove one.");
        }

        return trimmed;
    }

    private static string Trimmed(string? text) => (text ?? string.Empty).Trim();

    private static string? Optional(string? text) => Trimmed(text) is { Length: > 0 } trimmed ? trimmed : null;
}
