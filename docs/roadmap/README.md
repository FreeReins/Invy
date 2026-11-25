# Invy roadmap

One file per phase. Each phase file stands on its own, so a fresh agent session needs only this page, `AGENTS.md`
and one phase file. Run a phase with `/phase <id>` (`.claude/skills/phase/SKILL.md`). The design behind every phase is
[design.md](../developers/design.md). Read only the sections a phase file links to.

| Phase | File | Status |
|---|---|---|
| 0a Docs, roadmap, agent tooling | [phase-0-scaffold.md](phase-0-scaffold.md#0a-docs-roadmap-and-agent-tooling) | Done |
| 0b Backend skeleton | [phase-0-scaffold.md](phase-0-scaffold.md#0b-backend-skeleton) | Done |
| 0c Web skeleton | [phase-0-scaffold.md](phase-0-scaffold.md#0c-web-skeleton) | Done |
| 0d Deploy and CI | [phase-0-scaffold.md](phase-0-scaffold.md#0d-deploy-and-ci) | Done |
| 1 Core inventory | [phase-1-core-inventory.md](phase-1-core-inventory.md) | Done |
| 2 Capture | [phase-2-capture.md](phase-2-capture.md) | Done |
| 3 AI and MCP | [phase-3-ai-and-mcp.md](phase-3-ai-and-mcp.md) | Done |
| 4 Tailored spaces | [phase-4-tailored-spaces.md](phase-4-tailored-spaces.md) | Done |
| 5 Views and reports | [phase-5-views-and-reports.md](phase-5-views-and-reports.md) | Done |
| 6 Audience features | [phase-6-audience-features.md](phase-6-audience-features.md) | Done |
| 7 Scale hardening | [phase-7-scale-hardening.md](phase-7-scale-hardening.md) | In progress |

Phases 1 to 4 are in order. Phases 5, 6 and 7 only need phase 4 and can be done in any order. A feature that phases 5
and 6 build hides behind its space feature switch (the `add-space-feature` skill). Large phases are split
into sessions (1a, 1b, …) in their file.

## Decisions made along the way

Add one line per decision that changes `design.md`, newest last, and update `design.md` in the same commit.

- 2026-11-22: one install, many spaces; unique and counted items; v1 capture by labels, AI photos, barcodes and MCP.
- 2026-11-23: an item's value is its type's first Money field times its amount (1 when unique); a container's rollup is the count and value total of everything inside it at any depth, currencies not converted.
- 2026-11-23: label codes are unique across the install, so a scan finds its space; claiming one without a name makes a container called "Box <code>".
- 2026-11-23: marking a container seen writes one `Seen` event on it, not one per item inside, and nothing again within 10 minutes.
- 2026-11-23: attachments are stored by SHA-256 with SkiaSharp thumbnails on the images queue; backups copy each file once into `<backups>/files/`, kept when old backups go.
- 2026-11-23: barcode lookup tries Open Food Facts, then UPCitemdb's free trial, through `PublicWeb`; its switch is in the security settings (`barcodeLookup`).
- 2026-11-24: the Inbox type sweep stores its suggestion on the item (`suggested_type_id`, `type_suggested_at`), runs two minutes after things land in the Inbox and on **Suggest types**, and asks about each item once unless asked again.
- 2026-11-24: spaces have a kind (home, hobby, collection, business) that sets their starting types, features, places, examples, getting-started steps and hints. Features are per space; one that is off is hidden, not blocked, and any space can switch on any feature.
- 2026-11-24: an MCP tool without `space` uses the token owner's only space and asks which when they have several; `list_views` and `run_view` move to phase 5 with views.
- 2026-11-24: removing examples keeps an example that holds something of the person's own; an example's place that wasn't ticked is made as an example; getting-started steps of a feature that is off are left off the card.
- 2026-11-24: a view keeps its search as text, read again each run so `seen>1y` stays relative (a stored tree would freeze the date); it keeps its maker in `owner_id` with a `shared` flag, and pinning is per view.
- 2026-11-24: reports are printed from the browser (a print page with **Print or save as PDF**, as label sheets are), so Invy bundles no PDF library or fonts; their CSV comes from the server, streamed in batches.
- 2026-11-24: phase 6 (design.md, Audience features): stock is `items.low_stock_at`, alerting once on crossing and daily, against the amount less project reservations; lending is `lent_to`, `lent_at`, `due_on`, whole items only; reminders are Date fields with `remind` and `repeatMonths`, plus a per-space secret calendar feed; imports take CSV or Homebox's CSV into a chosen container, in background `COPY` batches; moving is `destination_id`, a printed manifest and **Arrived**; projects are `projects` and `project_items`; each feature has MCP tools that read and change it.
- 2026-11-24: security design review (design.md, Security hardening): API tokens gain expiry, last use and read-only (enforced by an MCP filter, refused unless a tool is marked read-only), and a reset revokes them; sign-in refusals are uniform with a decoy hash; known-device cookies bypass lockout; per-address limits on sign-in forms; a cross-site check on unsafe methods; AI keys sealed with Data Protection; private backup files; sign out everywhere; labels in other spaces look missing.
- 2026-11-26: phase 7 (design.md, Nesting and moves, Search, Scale targets): paths and depths move to a narrow `item_paths` table so a subtree move doesn't rewrite every index of `items`; rollups add up each container's direct children, deepest first; search keeps relevance order for every match, and the 200 ms target covers searches whose words match up to about 20,000 things (a name-order fallback for big matches was measured and dropped as slower); near spellings are searched for only when the words match nothing as typed.
