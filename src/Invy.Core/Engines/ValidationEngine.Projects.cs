using Invy.Core.Managers.Contracts;
using Invy.Core.Managers.Contracts.Messages;
using Invy.Core.ResourceAccess.Contracts.Messages;

namespace Invy.Core.Engines;

/// <content>Projects: their names, and how much of an item one can set aside.</content>
internal sealed partial class ValidationEngine
{
    public const int ProjectNameLength = 100;

    public async Task<Project> ProjectAsync(Guid spaceId, Guid id, CancellationToken cancellationToken) =>
        await projects.FindAsync(spaceId, id, cancellationToken)
        ?? throw new NotFoundException("That project no longer exists. It may have been removed; go back and reload.");

    /// <summary>Sets a project's name and notes from the request.</summary>
    public static void ProjectDetails(Project project, ProjectRequest request)
    {
        var name = Trimmed(request.Name);
        project.Name = name.Length is > 0 and <= ProjectNameLength
            ? name
            : throw new RuleViolationException($"A project needs a name of 1 to {ProjectNameLength} characters.");
        var notes = Optional(request.Notes);
        project.Notes = notes is null || notes.Length <= NotesLength
            ? notes
            : throw new RuleViolationException($"Notes can have up to {NotesLength:N0} characters.");
    }

    /// <summary>
    /// Checks a project can set aside <paramref name="amount"/> of an item that already sets aside
    /// <paramref name="current"/>: a unique item whole, a counted one up to what other projects haven't.
    /// </summary>
    public static decimal Reservation(Project project, Item item, decimal amount, decimal current)
    {
        if (project.Status != ProjectStatus.Open)
        {
            throw new RuleViolationException($"{project.Name} is done. Open a new project to set things aside.");
        }

        if (amount < 0)
        {
            throw new RuleViolationException("Set aside 0 or more.");
        }

        if (amount > 0 && item.Status != ItemStatus.Active)
        {
            throw new RuleViolationException($"{item.Name} is {item.Status.ToString().ToLowerInvariant()}, so it can't be set aside.");
        }

        if (item.Quantity is not decimal quantity)
        {
            return amount is 0 or 1
                ? amount
                : throw new RuleViolationException($"{item.Name} is a single item: set it aside whole (1), or not at all (0).");
        }

        var free = quantity - (item.Reserved - current);
        return amount <= free
            ? decimal.Round(amount, 3)
            : throw new RuleViolationException(
                $"Only {free.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture)}{(item.Unit is null ? string.Empty : $" {item.Unit}")} of {item.Name} isn't set aside already.");
    }
}
