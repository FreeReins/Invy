# Reminders and upkeep

This page is for anyone who wants to be reminded of dates on their things: when a warranty ends, when a tool needs
a service, when something expires. It shows how to make a date field remind you, how upkeep repeats, and how to put
the dates in your calendar.

Reminders show while the space has the **Reminders** feature on (**Settings → Kind and features**). See
[Spaces](spaces.md#features).

## Make a date field remind you

1. On **Types**, edit the type, such as *Appliance*.
2. On a **Date** field, such as *Warranty until*, enter **Remind, days ahead**: how many days before the date to
   remind you. 0 reminds on the day.
3. For upkeep that comes round again, such as *Next service*, also enter **Repeats every, months**.
4. Press **Save**.

The Home and Business kinds start with *Warranty until* reminding 30 days ahead, and the Hobby kind's *Tool* has a
*Next service* that repeats every 12 months. Fill the date in on each thing as usual.

## When you're reminded

The daily reminders (type `reminders`) mention a date:

- on the heads-up day (its days ahead), and
- on the day itself, and
- for upkeep that's overdue, once a week until it's marked done.

**Views → Coming up** lists dates in the next 30 days, and upkeep that's overdue. In search, `due<30d` finds things
with a reminder date, or a loan due back, in the next 30 days (overdue included); `due:2026-12-01..2026-12-31` finds a
range.

## Mark upkeep done

Press **Done today** next to the date on the thing's page, or in **Coming up**. The date moves on to today plus the
field's repeat, such as twelve months from today, and the timeline records the change. A warranty date doesn't
repeat, so it has no **Done today**.

## The calendar feed

Each space can publish its reminder dates and loans' due dates as a calendar to subscribe to in Google Calendar,
Apple Calendar or Outlook. Each date is an all-day event, and a reminder date has an alert its days ahead.

1. An owner opens **Settings → Kind and features** and presses **Turn on the calendar feed**.
2. Press **Copy address**, and add it to your calendar app as a subscription (*From URL* or *Subscribe*).

Calendar apps can't sign in, so the address carries a secret: anyone with it can read the dates. **New address**
makes a new secret (the old address stops working), and **Turn off** stops the feed. Calendar apps fetch the feed
every few hours.

## If something goes wrong

- **Remind, days ahead isn't in the type editor.** The field isn't a **Date**, or **Reminders** is off for the space.
- **"Only a date field that repeats can be marked done".** Set **Repeats every, months** on the field first.
- **The calendar shows nothing.** The calendar app may not have fetched it yet; most refresh every few hours.
