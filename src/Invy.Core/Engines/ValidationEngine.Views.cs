using Invy.Core.Managers.Contracts;
using Invy.Core.Managers.Contracts.Messages;
using Invy.Core.ResourceAccess.Contracts.Messages;

namespace Invy.Core.Engines;

/// <content>Saved views.</content>
internal sealed partial class ValidationEngine
{
    /// <summary>The longest name a view can have.</summary>
    public const int ViewNameLength = 80;

    /// <summary>How many views one person can make in a space.</summary>
    public const int ViewsPerPerson = 100;

    /// <summary>How many columns a table view can have.</summary>
    public const int ViewColumnLimit = 12;

    /// <summary>The columns every table view can show, besides fields.</summary>
    public static readonly IReadOnlyList<string> ViewBuiltInColumns = ["name", "type", "place", "quantity", "value", "seen", "added", "label"];

    /// <summary>The columns a table view shows when none are chosen.</summary>
    public static readonly IReadOnlyList<string> ViewDefaultColumns = ["name", "type", "place"];

    /// <summary>
    /// A view the person can see: their own, or one shared with the space. Someone else's personal view is reported
    /// as not there.
    /// </summary>
    public async Task<View> ViewAsync(Guid spaceId, Guid userId, Guid id, CancellationToken cancellationToken)
    {
        var view = await views.FindAsync(spaceId, id, cancellationToken);
        return view is not null && (view.Shared || view.OwnerId == userId)
            ? view
            : throw new NotFoundException("That view doesn't exist, or it is someone else's. Pick another from the Views tab.");
    }

    /// <summary>True when someone with this role can change or remove the view: its maker, or an editor for a shared one.</summary>
    public static bool CanChangeView(View view, Guid userId, SpaceRole role) =>
        view.Shared ? role >= SpaceRole.Editor : view.OwnerId == userId;

    /// <summary>A view's name, trimmed.</summary>
    public static string ViewName(string? name)
    {
        var trimmed = (name ?? string.Empty).Trim();
        return trimmed.Length is > 0 and <= ViewNameLength
            ? trimmed
            : throw new RuleViolationException($"A view needs a name of 1 to {ViewNameLength} characters.");
    }

    /// <summary>A display Invy knows.</summary>
    public static ViewDisplay ViewDisplay(ViewDisplay display) =>
        Enum.IsDefined(display)
            ? display
            : throw new RuleViolationException("Show a view as a table, cards, a tree, a board or a timeline.");

    /// <summary>A table's columns, lower case and without repeats; the defaults when none are given.</summary>
    public static List<string> ViewColumns(IReadOnlyList<string>? columns, IReadOnlyList<ItemType> types)
    {
        var chosen = (columns ?? []).Select(column => (column ?? string.Empty).Trim().ToLowerInvariant())
            .Where(column => column.Length > 0).Distinct().ToList();
        if (chosen.Count == 0)
        {
            return [.. ViewDefaultColumns];
        }

        if (chosen.Count > ViewColumnLimit)
        {
            throw new RuleViolationException($"A table can show up to {ViewColumnLimit} columns.");
        }

        var fields = types.SelectMany(type => type.Fields).Select(field => field.Key.ToLowerInvariant()).ToHashSet();
        var unknown = chosen.FirstOrDefault(column => !ViewBuiltInColumns.Contains(column) && !fields.Contains(column));
        return unknown is null
            ? chosen
            : throw new RuleViolationException(
                $"There is no column called {unknown}. Use {string.Join(", ", ViewBuiltInColumns)}, or a field's search name from the Types tab.");
    }

    /// <summary>For a board, the select field to group by (by search name); null for other displays.</summary>
    public static string? ViewGroupBy(ViewDisplay display, string? groupBy, IReadOnlyList<ItemType> types)
    {
        if (display != Managers.Contracts.Messages.ViewDisplay.Board)
        {
            return null;
        }

        var key = (groupBy ?? string.Empty).Trim();
        if (key.Length == 0)
        {
            throw new RuleViolationException("A board needs a select field to group by, such as condition or status.");
        }

        return types.SelectMany(type => type.Fields).FirstOrDefault(field => string.Equals(field.Key, key, StringComparison.OrdinalIgnoreCase)) switch
        {
            { Kind: FieldKind.Select } field => field.Key,
            null => throw new RuleViolationException($"No field has the search name {key}. Group a board by a select field from the Types tab."),
            var field => throw new RuleViolationException($"{field.Label} isn't a select field. Group a board by a field with a fixed list of options."),
        };
    }
}
