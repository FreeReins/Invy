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
| 4 Tailored spaces | [phase-4-tailored-spaces.md](phase-4-tailored-spaces.md) | Not started |
| 5 Views and reports | [phase-5-views-and-reports.md](phase-5-views-and-reports.md) | Not started |
| 6 Audience features | [phase-6-audience-features.md](phase-6-audience-features.md) | Not started |
| 7 Scale hardening | [phase-7-scale-hardening.md](phase-7-scale-hardening.md) | Not started |

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
