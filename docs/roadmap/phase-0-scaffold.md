# Phase 0: scaffold

**Goal:** an empty Invy that builds, tests, runs and deploys exactly like ChefBot, with no meal-planning code left,
and the agent tooling in place before any feature work.

**Source:** ChefBot at `../chef-bot` (relative to this repository). Copy a file, then rename `ChefBot` → `Invy`,
`chefbot` → `invy` and `chef-bot` → `invy`. Never edit the ChefBot checkout.

**Design sections:** [What carries over from ChefBot](../developers/design.md#what-carries-over-from-chefbot),
[Spaces and roles](../developers/design.md#spaces-and-roles).

**Out of scope:** any inventory feature (items, types, search). Those start in phase 1.

## 0a Docs, roadmap and agent tooling

- [x] `docs/developers/design.md` and `docs/roadmap/` (this folder).
- [x] `AGENTS.md`, `CLAUDE.md` (`@AGENTS.md`) and `docs/AGENTS.md`.
- [x] `.claude/hooks/dotnet-format.sh` and `.claude/hooks/guard.sh`.
- [ ] `.claude/settings.json` registering the hooks, plus an allowlist for build, test and read-only commands. The user adds this file themselves, because agents may not grant their own permissions.
- [x] Skills copied from ChefBot: `add-stored-entity`, `add-core-operation`, `add-web-screen`, `update-docs`, `tune-agents-md`. New skills: `add-field-kind`, `add-mcp-tool`, `add-ai-task`, `phase`.
- [x] `.waza.yaml` token budgets, `evals/waza.sh`, `evals/Containerfile` and `evals/agents-md/` with Invy tasks.
- [x] Offline token check: `evals/waza.sh tokens check AGENTS.md docs/AGENTS.md docs/roadmap/README.md .claude/skills/*/SKILL.md --strict` passes (12/12), and `evals/waza.sh preflight` passes for both suites.

## 0b Backend skeleton

- [x] Delete `src/Website/`, `src/Invy.sln` and `src/.dockerignore` (the old ASP.NET MVC template).
- [x] Root files from ChefBot: `Invy.slnx`, `Directory.Build.props`, `Directory.Packages.props`, `global.json`, `stylecop.json`, `.editorconfig`, `dotnet-tools.json`, `.gitignore`, `LICENSE` (GPLv3), `CONTRIBUTING.md`, `SECURITY.md`, `SUPPORT.md`.
- [x] `src/Invy.Core`: keep accounts, sessions, API tokens, settings, AI models and routes, backups, notifications, scheduled jobs, messaging and `PublicWeb`. Remove everything about recipes, plans, pantry, shopping, cookbooks, calendar meals, achievements and history. `MaintenanceManager` and `AccountManager` stay.
- [x] `src/Invy.Api`: keep `Database/` (port 54339), `Https/`, `Logging/`, `Live/`, `Mcp/` host with no tools yet, `Sessions.cs`, `ApiTokenAuthentication.cs`, `WebPage.cs`, `PortableApp.cs`, `WindowsEventLog.cs`. Rename `ChefBotStartup` → `InvyStartup` and `ChefBotExceptionHandler` → `InvyExceptionHandler`.
- [x] One fresh `Baseline` migration (don't copy ChefBot's migrations). Turn on `ltree` and `pg_trgm` in it with `CREATE EXTENSION IF NOT EXISTS`.
- [x] `tests/Invy.Core.Tests`: keep `ArchitectureTests`, `FileSizeTests`, `JsonShapeTests`, `SchemaTests`, `MigrationTests`, `AccountTests`, `ApiTokenTests`, `AiSettingsTests`, `ReminderAndBackupTests` (backup half), `ScheduledJobTests`, `TestCore` and the fakes those need.
- [x] `dev.sh` from ChefBot (container `invy-dev-db`, ports 8080/8443 with the 5080/5443 fallback).
- [x] Check the copied skills' paths and examples still exist. Fix the skill, not the code.

**Done when:** `dotnet build Invy.slnx -v q -nologo` gives no warnings, `dotnet run --project tests/Invy.Core.Tests`
passes, and `grep -ri chefbot src tests` finds nothing.

## 0c Web skeleton

- [x] `src/Invy.Web` from ChefBot: `package.json`, Vite and Svelte config, `index.html`, `public/` (manifest, `sw.js` with cache `invy-page-v1`, `theme.js` with key `invy-theme`), `styles.css`, `components/`, `lib/`, `screens/` (setup, sign-in, join, reset), and `settings/` panels for login, appearance, AI assistants, backups, HTTPS, logs and notifications.
- [x] Invy's colours: new `--accent` and `--accent-2` in `styles.css` (keep the contrast at 4.5:1 in both themes), the logo mark using the Lucide `Boxes` icon, and `--area-*` colours for the tabs in [Web screens](../developers/design.md#web-screens).
- [x] Tabs: **Find** (empty state for now) and **Settings**. The rest come with their phases.
- [x] `src/Invy.Web/AGENTS.md` from ChefBot's, renamed. Playwright tests and fixtures cut down to the remaining screens.

**Done when:** `./dev.sh web` builds without warnings and `./dev.sh web-test` passes in all three projects.
Screenshots from `./dev.sh web-shot /find` look like ChefBot with Invy's colours.

## 0d Deploy and CI

- [x] `Dockerfile` from ChefBot. Keep `ltree.*` and `pg_trgm.*` under `share/extension` and `lib` in the `postgres` stage.
- [x] `deploy/podman` (`install.sh`, `uninstall.sh`, `invy.env.example`), `deploy/compose/compose.yaml`, `deploy/windows` (keep the `ltree` and `pg_trgm` files when trimming the EDB zip).
- [x] `.github/workflows/release.yml`, `docs.yml` and `links.yml`, issue templates, `FUNDING.yml`. Image `ghcr.io/freereins/invy`.
- [x] `mkdocs.yml` (logo `material/package-variant-closed`, `exclude_docs` covers `AGENTS.md`, `requirements.txt` and `roadmap/`), `docs/requirements.txt`, and ChefBot's getting-started, admin and troubleshooting pages that still apply, renamed.
- [x] `README.md` showcase in ChefBot's style: what Invy does, get started, links.

**Done when:** `podman build -t localhost/invy .` succeeds. A container run with a `/data` volume answers `/health`,
the setup wizard and sign-in work, `/help` opens, and `/mcp` accepts a token. The strict MkDocs build from
`docs/AGENTS.md` is clean.

## Session log

- 2026-11-22 (0a): design, roadmap, AGENTS.md, CLAUDE.md, hooks, nine skills and evals written. Token check and preflight pass. Half-done: `.claude/settings.json` waits for the user. Next: 0b, deleting `src/Website`.
- 2026-11-22 (0b): backend copied from ChefBot and trimmed to accounts, sessions, tokens, settings, AI models and routes, backups, inbox, notifications, scheduled jobs, messaging and the MCP host; fresh `Baseline` migration with `ltree` and `pg_trgm`; DB port 54339; 80 tests pass, no warnings, no `chefbot` in src or tests. `AiTask` is empty and `AiFeaturesResponse` is now a list of ready tasks; the ICS calendar feed and daily reminder types come back with reminders; pre-migration database adoption dropped. CONTRIBUTING.md's web section still describes ChefBot's page. The app wasn't started against a database. Next: 0c, copying `src/Invy.Web` from ChefBot.
- 2026-11-22 (0c): `src/Invy.Web` copied from ChefBot and cut to setup (account, HTTPS), sign-in, join, reset, the Find tab (empty state) and Settings (login, appearance, AI assistants, notifications, HTTPS, backups, logs); blue `--accent`, amber `--accent-2`, `--area-*` for all eight design tabs (contrast checked), Lucide `Boxes` mark; offline outbox and snapshot dropped, so offline actions just fail; tests cut to `a11y`, `keyboard` and `screenshot` (128 pass, 4 skipped by project); `./dev.sh web` has no warnings. Run `./dev.sh` commands bare from the repo root, or auto mode blocks them. CONTRIBUTING.md's web section still describes ChefBot's page. Next: 0d, the `Dockerfile` from ChefBot.
- 2026-11-22 (0d): `Dockerfile` (fails the build if ltree or pg_trgm is missing), `.dockerignore`, `deploy/` (podman, compose, Windows with new MSI `UpgradeCode` and component GUID so it never replaces ChefBot, extension files checked in `build.ps1`), workflows and issue templates, `mkdocs.yml` and 26 pages rewritten for Invy, README with Find screenshots. `podman build -t localhost/invy .` succeeds; a container on spare ports answered `/health`, setup, sign-in, `/help` and `/mcp` (200 with a token, 401 without), with ltree 1.3 and pg_trgm 1.6 installed; strict MkDocs builds clean. Smoke tests go to `https://localhost:<mapped port>` with `curl -k`: with HTTPS on, **Always use HTTPS** starts on and HTTP redirects to `Https__PublicPort`. Not done: `docs/reference/api.md` (AGENTS.md points at it), CONTRIBUTING.md's web section, a People tab (invites only through the API). Next: tick 0a's `.claude/settings.json` box once the user confirms the file, then phase 1.
