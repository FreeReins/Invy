using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Invy.Core.Engines.Contracts.Messages;
using Invy.Core.ResourceAccess.Contracts.Messages;

namespace Invy.Core.Engines;

/// <summary>
/// Writes a space's calendar feed (RFC 5545) for calendar apps to subscribe to: reminder dates and loans' due dates
/// as all-day events, each reminder with an alarm its field's days ahead. Makes and checks the feed's secret, as
/// ChefBot's meal plan feed does. Reads and saves nothing.
/// </summary>
internal sealed class CalendarEngine
{
    /// <summary>The longest line, in UTF-8 bytes, before it is folded onto the next (RFC 5545 section 3.1).</summary>
    private const int LineOctets = 75;

    /// <summary>A new secret for the feed's address.</summary>
    public static string NewToken() => Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();

    /// <summary>True when the space's feed is on and <paramref name="token"/> is its secret, compared in constant time.</summary>
    public static bool Matches(Space space, string? token) =>
        space.CalendarToken is { Length: > 0 } secret && token is not null
        && CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(secret), Encoding.UTF8.GetBytes(token.Trim()));

    /// <summary>The feed: each reminder date and each loan's due date.</summary>
    /// <param name="space">The space the feed is for.</param>
    /// <param name="dates">Its reminder dates.</param>
    /// <param name="loans">Its items lent out with a due date.</param>
    /// <param name="site">Invy's own address, ending in a slash, for links to items.</param>
    /// <param name="now">When the feed is written, for each event's timestamp.</param>
    public string Write(Space space, IEnumerable<DueDate> dates, IEnumerable<Item> loans, Uri site, DateTimeOffset now)
    {
        var lines = new List<string>
        {
            "BEGIN:VCALENDAR",
            "VERSION:2.0",
            "PRODID:-//Invy//Reminders//EN",
            "CALSCALE:GREGORIAN",
            "METHOD:PUBLISH",
            $"X-WR-CALNAME:{Escape($"Invy: {space.Name}")}",
            $"X-WR-CALDESC:{Escape($"Warranty, upkeep and loan dates in {space.Name}.")}",
            "REFRESH-INTERVAL;VALUE=DURATION:PT6H",
            "X-PUBLISHED-TTL:PT6H",
        };

        var stamp = now.UtcDateTime.ToString("yyyyMMdd'T'HHmmss'Z'", CultureInfo.InvariantCulture);
        foreach (var date in dates.OrderBy(date => date.On).ThenBy(date => date.Item.Name, StringComparer.Ordinal))
        {
            var upkeep = date.Field.RepeatMonths is not null;
            lines.AddRange(Event(
                $"{date.Item.Id}-{date.Field.Id}", date.On, $"{date.Item.Name}: {date.Field.Label}",
                upkeep ? $"Upkeep for {date.Item.Name}. Mark it done in Invy to move it on." : $"{date.Field.Label} for {date.Item.Name}.",
                new Uri(site, $"browse/{date.Item.Id}"), stamp, date.Field.Remind));
        }

        foreach (var item in loans.Where(item => item.DueOn is not null).OrderBy(item => item.DueOn).ThenBy(item => item.Name, StringComparer.Ordinal))
        {
            lines.AddRange(Event(
                $"{item.Id}-loan", item.DueOn!.Value, $"{item.Name} due back from {item.LentTo}",
                $"{item.Name} was lent to {item.LentTo}.", new Uri(site, $"browse/{item.Id}"), stamp, 0));
        }

        lines.Add("END:VCALENDAR");
        var text = new StringBuilder();
        foreach (var line in lines)
        {
            Fold(text, line);
        }

        return text.ToString();
    }

    /// <summary>Escapes text for a TEXT value: backslashes, semicolons, commas and line breaks (RFC 5545 section 3.3.11).</summary>
    public static string Escape(string text) => text
        .Replace("\\", "\\\\", StringComparison.Ordinal)
        .Replace(";", "\\;", StringComparison.Ordinal)
        .Replace(",", "\\,", StringComparison.Ordinal)
        .Replace("\r\n", "\\n", StringComparison.Ordinal)
        .Replace("\n", "\\n", StringComparison.Ordinal);

    /// <summary>Writes one content line, folded so no line is longer than 75 bytes, never splitting a character.</summary>
    public static void Fold(StringBuilder text, string line)
    {
        var used = 0;
        foreach (var rune in line.EnumerateRunes())
        {
            var size = rune.Utf8SequenceLength;
            if (used + size > LineOctets)
            {
                text.Append("\r\n ");
                used = 1;
            }

            text.Append(rune.ToString());
            used += size;
        }

        text.Append("\r\n");
    }

    private static IEnumerable<string> Event(string uid, DateOnly on, string summary, string description, Uri link, string stamp, int? daysAhead)
    {
        yield return "BEGIN:VEVENT";
        yield return $"UID:{uid}@invy";
        yield return $"DTSTAMP:{stamp}";
        yield return $"DTSTART;VALUE=DATE:{Day(on)}";
        yield return $"DTEND;VALUE=DATE:{Day(on.AddDays(1))}";
        yield return $"SUMMARY:{Escape(summary)}";
        yield return $"DESCRIPTION:{Escape($"{description}\n{link.AbsoluteUri}")}";
        yield return $"URL:{link.AbsoluteUri}";
        yield return "TRANSP:TRANSPARENT";
        if (daysAhead is int days)
        {
            yield return "BEGIN:VALARM";
            yield return "ACTION:DISPLAY";
            yield return $"DESCRIPTION:{Escape(summary)}";
            yield return $"TRIGGER:-P{days}D";
            yield return "END:VALARM";
        }

        yield return "END:VEVENT";
    }

    private static string Day(DateOnly day) => day.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
}
