using System.Globalization;
using Invy.Core.Engines.Contracts.Messages;
using Invy.Core.ResourceAccess.Contracts.Messages;

namespace Invy.Core.Engines;

/// <summary>
/// Works out what to tell people and when: the daily reminders, and which old backups to delete. It is given the
/// settings and returns decisions, so every rule can be tested without a clock or a network. Reads and saves nothing.
/// </summary>
internal sealed class NotificationEngine
{
    /// <summary>The most items one notification lists by name; the rest are counted.</summary>
    public const int MaxListed = 20;

    /// <summary>
    /// The reminders for one day, in the order they should arrive: what is running low, loans due back, and reminder
    /// dates. A kind with nothing to say today is left out.
    /// </summary>
    /// <param name="today">The day the reminders are for.</param>
    /// <param name="facts">What the spaces with each feature on hold now.</param>
    public IReadOnlyList<Notification> Daily(DateOnly today, DailyFacts facts)
    {
        var daily = new List<Notification>();
        if (facts.Low.Count > 0)
        {
            daily.Add(List(NotificationTypes.LowStockDaily, "Running low", "running low", facts.Low.OrderBy(Order).ToList(),
                entry => $"{entry.Item.Name} ({Amount(entry.Item.Quantity - entry.Item.Reserved, entry.Item.Unit)} left)",
                entry => Facts(entry, ("left", entry.Item.Quantity - entry.Item.Reserved), ("lowStockAt", entry.Item.LowStockAt))));
        }

        var overdue = facts.Lent.Where(entry => entry.Item.DueOn is DateOnly due && due <= today)
            .OrderBy(entry => entry.Item.DueOn).ThenBy(Order).ToList();
        if (overdue.Count > 0)
        {
            daily.Add(List(NotificationTypes.Overdue, "Due back", "due back", overdue,
                entry => $"{entry.Item.Name} from {entry.Item.LentTo} ({(entry.Item.DueOn == today ? "today" : $"since {Day(entry.Item.DueOn!.Value)}")})",
                entry => Facts(entry, ("lentTo", entry.Item.LentTo), ("dueOn", entry.Item.DueOn))));
        }

        var dates = facts.Dates.Where(date => Remind(date, today)).OrderBy(date => date.On).ThenBy(date => date.Item.Name, StringComparer.CurrentCulture).ToList();
        if (dates.Count > 0)
        {
            daily.Add(List(NotificationTypes.Reminders, "Coming up", "to remember", dates,
                date => $"{date.Item.Name}: {date.Field.Label} {When(date.On, today)}",
                date => new Dictionary<string, object?>
                {
                    ["space"] = date.Space.Name,
                    ["spaceId"] = date.Space.Id,
                    ["itemId"] = date.Item.Id,
                    ["name"] = date.Item.Name,
                    ["field"] = date.Field.Label,
                    ["on"] = date.On,
                    ["upkeep"] = date.Field.RepeatMonths is not null,
                }));
        }

        return daily;
    }

    /// <summary>
    /// True when a reminder date is worth a line today: the heads-up day (its field's days ahead), the day itself, and
    /// for upkeep that is overdue, once a week until it is marked done.
    /// </summary>
    public static bool Remind(DueDate date, DateOnly today) =>
        date.Field.Remind is int ahead && (date.On == today.AddDays(ahead) || date.On == today
            || (date.Field.RepeatMonths is not null && date.On < today && (today.DayNumber - date.On.DayNumber) % 7 == 0));

    /// <summary>The alert sent right away when a counted item reaches its low-stock level.</summary>
    public Notification LowStock(Space space, Item item)
    {
        var left = item.Quantity - item.Reserved;
        var reserved = item.Reserved > 0 ? $", with {Amount(item.Reserved, item.Unit)} set aside for projects" : string.Empty;
        return new Notification(NotificationTypes.LowStock, $"{item.Name} is running low",
            $"{item.Name} in {space.Name} is down to {Amount(left, item.Unit)}{reserved}. Its low-stock level is {Amount(item.LowStockAt, item.Unit)}.",
            Facts(new SpaceItem(space, item), ("left", left), ("lowStockAt", item.LowStockAt)));
    }

    /// <summary>The sample sent from the Settings tab.</summary>
    public Notification Test() => new(NotificationTypes.Test, "Invy test", "Notifications from Invy are working.",
        new Dictionary<string, object?> { ["sentBy"] = "Settings" });

    /// <summary>An amount as text, such as "3.5 kg".</summary>
    public static string Amount(decimal? amount, string? unit) =>
        $"{(amount ?? 0).ToString("0.###", CultureInfo.InvariantCulture)}{(unit is null ? string.Empty : $" {unit}")}";

    private static string Day(DateOnly day) => day.ToString("d MMM yyyy", CultureInfo.InvariantCulture);

    private static string When(DateOnly on, DateOnly today) => (on.DayNumber - today.DayNumber) switch
    {
        0 => "is today",
        < 0 => $"was due {Day(on)}",
        var days => $"is on {Day(on)}, in {days} day{(days == 1 ? string.Empty : "s")}",
    };

    private static string Order(SpaceItem entry) => $"{entry.Space.Name}\u0000{entry.Item.Name}";

    /// <summary>One notification listing several things, the first <see cref="MaxListed"/> by name.</summary>
    private static Notification List<T>(string type, string title, string what, IReadOnlyList<T> entries, Func<T, string> line, Func<T, Dictionary<string, object?>> data)
    {
        var named = string.Join("; ", entries.Take(MaxListed).Select(line));
        var more = entries.Count > MaxListed ? $"; and {entries.Count - MaxListed} more" : string.Empty;
        var count = entries.Count == 1 ? $"1 thing is {what}" : $"{entries.Count} things are {what}";
        return new Notification(type, title, $"{count}: {named}{more}.", new Dictionary<string, object?>
        {
            ["count"] = entries.Count,
            ["items"] = entries.Take(MaxListed).Select(data).ToList(),
        });
    }

    private static Dictionary<string, object?> Facts(SpaceItem entry, params (string Key, object? Value)[] extra)
    {
        var facts = new Dictionary<string, object?>
        {
            ["space"] = entry.Space.Name,
            ["spaceId"] = entry.Space.Id,
            ["itemId"] = entry.Item.Id,
            ["name"] = entry.Item.Name,
            ["unit"] = entry.Item.Unit,
        };
        foreach (var (key, value) in extra)
        {
            facts[key] = value;
        }

        return facts;
    }

    /// <summary>The backups that fall outside the retention rules. The newest backup is always kept.</summary>
    public IReadOnlyList<StoredBackup> ExpiredBackups(IEnumerable<StoredBackup> files, BackupSettings settings, DateTimeOffset now)
    {
        var newestFirst = files.OrderByDescending(file => file.CreatedAt).ToList();
        return newestFirst
            .Where((file, index) => index > 0 && (
                (settings.KeepCount > 0 && index >= settings.KeepCount) ||
                (settings.KeepDays > 0 && file.CreatedAt < now.AddDays(-settings.KeepDays))))
            .ToList();
    }

    /// <summary>The file name for a backup made at <paramref name="localNow"/>.</summary>
    public string BackupName(DateTimeOffset localNow) =>
        $"invy-backup-{localNow.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture)}.json.gz";
}
