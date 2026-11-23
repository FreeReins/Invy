# Phase 5: audience features

**Goal:** the household, hobby and business features in the design's ideas list.

**Design sections:** [Ideas by audience](../developers/design.md#ideas-by-audience),
[Data model](../developers/design.md#data-model) (status, quantity).

Each item below is a session of its own.

- [ ] Stock: low-stock thresholds on counted items, with notifications through the webhook and Home Assistant.
- [ ] Lending and check-out: lend to a person or site, due dates, reminders, and the `Lent` and `Returned` events.
- [ ] Reminders: warranty end and upkeep dates in notifications and the ICS feed (ChefBot's `CalendarEngine` pattern).
- [ ] Imports: CSV with column mapping, and Homebox backups. Bulk inserts through `COPY`.
- [ ] Moving mode: a destination room per box, and a printed manifest per box.
- [ ] Projects: reserve or use up items for a project.
- [ ] Docs for each, in `docs/using/`.

**Done when:** each item's tests pass and it has a docs page.

## Session log
