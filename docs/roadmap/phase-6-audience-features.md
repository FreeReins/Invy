# Phase 6: audience features

**Goal:** the household, hobby and business features in the design's ideas list.

**Design sections:** [Ideas by audience](../developers/design.md#ideas-by-audience),
[Data model](../developers/design.md#data-model) (status, quantity).

Each item below is a session of its own. Each feature hides behind its space feature switch, following
`add-space-feature`: set `Available` in `SpaceFeatures` and add its getting-started step when it's built.

**Design sections (also):** [Tailored spaces](../developers/design.md#features).

- [ ] Stock (`stock`): low-stock thresholds on counted items, with notifications through the webhook and Home Assistant.
- [ ] Lending and check-out (`lending`): lend to a person or site, due dates, reminders, and the `Lent` and `Returned` events.
- [ ] Reminders (`reminders`): warranty end and upkeep dates in notifications and the ICS feed (ChefBot's `CalendarEngine` pattern).
- [ ] Imports: CSV with column mapping, and Homebox backups. Bulk inserts through `COPY`.
- [ ] Moving mode (`moving`): a destination room per box, and a printed manifest per box.
- [ ] Projects (`projects`): reserve or use up items for a project.
- [ ] Docs for each, in `docs/using/`.

**Done when:** the build has no warnings, each item's TUnit and web tests pass (including its feature switched off),
and the strict docs build is clean with a page for each.

## Session log
