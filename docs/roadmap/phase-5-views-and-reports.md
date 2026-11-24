# Phase 5: views and reports

**Goal:** saved searches become live views on the home screen, and reports can be printed or exported.

**Design sections:** [Views](../developers/design.md#views), [Ideas by audience](../developers/design.md#ideas-by-audience) (the reports).

## Work

- [ ] `View` entity and `ViewManager`: personal or shared, pinned, with a saved query tree, display, columns and sort.
- [ ] Web **Views** tab with the table, cards, tree, board and timeline displays. Pinned views show on **Find**. Live topics refresh open views.
- [ ] Value totals by container and by type, from the rollups, behind the `values` feature.
- [ ] Reports: an insurance inventory (PDF and CSV, with photos and values by room; behind `values`), an audit discrepancy report (behind `audits`), and a CSV export of any view.
- [ ] MCP tools `list_views` and `run_view`, following `add-mcp-tool` (moved here from phase 3, which had no views yet).
- [ ] Docs: `docs/using/views.md` and `docs/using/reports.md`, and the two tools in `docs/admin/ai-assistants.md`.

**Done when:** the build, the tests and the web tests pass, including a web test where a pinned "not seen in a year"
view updates when a stubbed live topic says an item was scanned.

## Session log
