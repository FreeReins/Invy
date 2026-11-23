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
| 2 Capture | [phase-2-capture.md](phase-2-capture.md) | In progress |
| 3 AI and MCP | [phase-3-ai-and-mcp.md](phase-3-ai-and-mcp.md) | Not started |
| 4 Views and reports | [phase-4-views-and-reports.md](phase-4-views-and-reports.md) | Not started |
| 5 Audience features | [phase-5-audience-features.md](phase-5-audience-features.md) | Not started |
| 6 Scale hardening | [phase-6-scale-hardening.md](phase-6-scale-hardening.md) | Not started |

Phases 1 to 3 are in order. Phases 4, 5 and 6 only need phase 3 and can be done in any order. Large phases are split
into sessions (1a, 1b, …) in their file.

## Decisions made along the way

Add one line per decision that changes `design.md`, newest last, and update `design.md` in the same commit.

- 2026-11-22: one install, many spaces; unique and counted items; v1 capture by labels, AI photos, barcodes and MCP.
- 2026-11-23: an item's value is its type's first Money field times its amount (1 when unique); a container's rollup is the count and value total of everything inside it at any depth, currencies not converted.
- 2026-11-23: label codes are unique across the install, so a scan finds its space; claiming one without a name makes a container called "Box <code>".
