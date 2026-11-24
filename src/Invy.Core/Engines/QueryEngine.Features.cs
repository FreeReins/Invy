using System.Globalization;
using Invy.Core.Managers.Contracts;
using Invy.Core.Managers.Contracts.Messages;
using Invy.Core.ResourceAccess.Contracts.Messages;

namespace Invy.Core.Engines;

/// <content>The space features' search terms: <c>is:low</c>, <c>is:overdue</c>, <c>lent:</c>, <c>going:</c> and <c>due</c>.</content>
internal sealed partial class QueryEngine
{
    private sealed partial class BuildState
    {
        /// <summary>The day the search is made on, for overdue loans and dates ahead.</summary>
        private DateOnly Today => DateOnly.FromDateTime(now.DateTime);

        /// <summary>
        /// <c>due&lt;30d</c>: a loan due back, or a reminder date, on or before 30 days from today (overdue included);
        /// <c>due&gt;30d</c>: after that; or a date range, such as <c>due:2026-12-01..2026-12-31</c>.
        /// </summary>
        private QueryNode ReadDue(Term term)
        {
            Range<DateOnly> range;
            if (DurationPattern().Match(term.Value) is { Success: true } duration)
            {
                var amount = int.Parse(duration.Groups["amount"].Value, CultureInfo.InvariantCulture);
                var until = duration.Groups["unit"].Value switch
                {
                    "d" => Today.AddDays(amount),
                    "w" => Today.AddDays(7 * amount),
                    "m" => Today.AddMonths(amount),
                    _ => Today.AddYears(Math.Min(amount, 1000)),
                };
                range = term.Op switch
                {
                    "<" or "<=" => new Range<DateOnly>(null, false, until, true),
                    ">" or ">=" => new Range<DateOnly>(until, false, null, false),
                    _ => throw new RuleViolationException($"Use < or > with a time ahead: due<{term.Value} for sooner, due>{term.Value} for later."),
                };
            }
            else
            {
                range = DateRange(term);
            }

            var reminders = Fields().Where(field => field.Kind == FieldKind.Date && field.Remind is not null).Select(field => field.Id).ToList();
            return reminders.Count == 0
                ? new LoanDue(range)
                : new AnyOf([new LoanDue(range), new FieldRange(reminders, null, range)]);
        }
    }
}
