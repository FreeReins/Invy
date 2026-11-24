# Phase 5: views and reports

**Goal:** saved searches become live views on the home screen, and reports can be printed or exported.

**Design sections:** [Views](../developers/design.md#views), [Ideas by audience](../developers/design.md#ideas-by-audience) (the reports).

## Work

- [x] `View` entity and `ViewManager`: personal or shared, pinned, with a saved search (text, read again each run), display, columns and sort.
- [x] Web **Views** tab with the table, cards, tree, board and timeline displays. Pinned views show on **Find**. Live topics refresh open views.
- [x] Value totals by container and by type, from the rollups, behind the `values` feature.
- [x] Reports: an insurance inventory (PDF and CSV, with photos and values by room; behind `values`), an audit discrepancy report (behind `audits`), and a CSV export of any view.
- [x] MCP tools `list_views` and `run_view`, following `add-mcp-tool` (moved here from phase 3, which had no views yet).
- [x] Docs: `docs/using/views.md` and `docs/using/reports.md`, and the two tools in `docs/admin/ai-assistants.md`.

**Done when:** the build, the tests and the web tests pass, including a web test where a pinned "not seen in a year"
view updates when a stubbed live topic says an item was scanned.

## Session log

- 2026-11-24 (5): `View` table (`AddViews` migration, backed up), `ViewManager` with list/create/update/remove/run, `/api/spaces/{spaceId}/views` endpoints (checked against a real PostgreSQL), `list_views` and `run_view` MCP tools, `ViewTests`, API and assistant docs. Search paging moved to `Managers/SearchPages.cs`, shared by search and views. Docs box half-done: `ai-assistants.md` is updated, `views.md` and `reports.md` aren't written. The Timeline display has no endpoint for the matching items' events yet. Next: the web **Views** tab (box 2), adding a timeline endpoint for the Timeline display.
- 2026-11-24 (5): Web **Views** tab (`tabs/Views.svelte`, `views/ViewForm.svelte`, `views/ViewResults.svelte`, `lib/views.js`) with table, cards, tree, board and timeline; pinned views on **Find** (`components/PinnedViews.svelte`) and **Save as a view**; open and pinned views reload on the `items` live topic, which marking an item seen now sends. New `GET …/views/{viewId}/timeline` and `details` (fields, dates, photo) on a view's results, checked against a real PostgreSQL. `tests/views.spec.js` covers the Done-when live test. `docs/using/views.md` written; `reports.md` isn't, so the docs box stays open. Next: value totals by container and by type behind `values` (box 3).
- 2026-11-24 (5): `ReportManager` and `ReportEngine`: value totals by place at the top (from the rollups) and by type, the insurance inventory by room, missing items, and CSV of each and of any view (`/api/spaces/{spaceId}/reports/…`, `…/views/{viewId}/export.csv`), checked against a real PostgreSQL. Reports print from the browser (decision recorded). Web: totals and report links under **Reports** on **Views**, `/views/insurance` and `/views/missing` print pages, **Export CSV** on a view. `docs/using/reports.md` written. Phase done. Next: phase 6 or 7.
