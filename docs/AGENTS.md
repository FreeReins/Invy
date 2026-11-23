# AGENTS.md for the documentation

`docs/` is Invy's user and developer documentation, in plain Markdown. The same files are read on GitHub, published
to GitHub Pages (`.github/workflows/docs.yml`), and built into the image at `/help`, where the app's **?** button and
"Guide:" links open them. This file, `requirements.txt` and `roadmap/` aren't published (`exclude_docs` in
`mkdocs.yml`).

`roadmap/` and `developers/design.md` are the plan for building Invy, written for agents and contributors. Change a
phase file's checkboxes and session log as work finishes. Change `design.md` only when a decision changes, and add
that decision to `roadmap/README.md`.

## Check your work

```sh
python3 -m venv /tmp/invy-docs && /tmp/invy-docs/bin/pip install -q -r docs/requirements.txt   # once
/tmp/invy-docs/bin/mkdocs build --strict -d /tmp/invy-docs/site        # must print no WARNING
SITE_URL= /tmp/invy-docs/bin/mkdocs build --strict -d /tmp/invy-docs/help   # the /help variant
```

Use the scratchpad instead of `/tmp` when one is given. A docs change is done when the strict build is clean. Don't
build the container image to check docs.

## Writing rules

- **Audience first.** `getting-started/` and `using/` are for anyone using Invy, `admin/` for whoever runs it,
  `troubleshooting/` is symptom first, and `developers/` explains design and links to CONTRIBUTING.md for rules.
- **One task per page.** Open with one or two sentences on who it's for and what they'll have done. Then numbered
  steps, then "If something goes wrong" linking to `troubleshooting/`.
- **Plain words, short sentences.** UI labels in **bold**, exactly as the page shows them (read the `.svelte` file).
  Commands ready to copy, with Podman and Docker Compose versions where they differ.
- **Facts from the code, never from memory.** Defaults from `ResourceAccess/Contracts/Messages/*Settings.cs`, routes
  from `src/Invy.Api/Endpoints/`, labels from `src/Invy.Web/src/`, error wording from the `throw` sites.
- **Links:** relative between `.md` files inside `docs/`, always to a file, never to a directory. Anything outside
  `docs/` gets an absolute `https://github.com/FreeReins/invy/blob/main/…` link.
- **Only syntax both GitHub and MkDocs render:** tables, fenced code, Mermaid and GitHub alerts (`> [!NOTE]`).
  No `!!!` admonitions, tabs or definition lists.
- **Say it once.** Link to the page that owns a fact instead of repeating it.
- **Every audience is first class:** a household of one, a hobbyist and a business all use the same pages.

## Adding a page

1. Write it in the right folder, named in the reader's words (`labels-and-scanning.md`).
2. Add it to `nav` in `mkdocs.yml`, and to `docs/index.md` if people would look for it there.
3. If a Settings section or screen is about it, point its `help` path at the page.

Renaming or moving a page breaks `/help` links in the app: search `src/Invy.Web/src` for the old path.

## Which page covers what

| When this changes in the code | Update |
|---|---|
| An endpoint (`src/Invy.Api/Endpoints/`) | `reference/api.md` |
| A setting in `appsettings.json` or `invy.env.example` | `admin/settings.md`, plus the page for that feature |
| Setup wizard | `getting-started/first-run.md` |
| Spaces, members, roles | `using/spaces.md`, `admin/people-and-accounts.md` |
| Types and fields, field kinds, starter packs | `using/types-and-fields.md` |
| Items, nesting, moves, timeline, quick add, Inbox | `using/items.md`, `using/timeline.md` |
| Search syntax | `using/search.md` |
| Views and reports | `using/views.md`, `using/reports.md` |
| Labels, scanning, audits | `using/labels-and-scanning.md`, `using/audits.md` |
| AI tasks and MCP tools | `admin/ai-assistants.md` |
| Notifications, reminders, calendar feed | `admin/notifications.md`, `using/reminders.md` |
| HTTPS, backups, upgrades | `admin/https-and-certificates.md`, `admin/backups-and-restore.md`, `admin/upgrading.md` |
| A new table or schema change | `admin/upgrading.md`, `developers/data-model.md` |
| Ports, volumes, `deploy/`, Dockerfile | `getting-started/install-*.md`, `developers/building-and-releasing.md` |
| A design decision | `developers/design.md` and the decisions list in `roadmap/README.md` |
| Something Invy can't do, measured limits | `reference/limits.md` |
