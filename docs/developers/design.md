# Invy design

Invy is a self-hosted inventory for the things you store: what you have, where it is now, and where it has been. It
is a sibling of [ChefBot](https://github.com/FreeReins/chef-bot) and copies its architecture, look, deployment and
habits, so the two feel like one family.

This page records decisions. It changes only when a decision changes. Progress lives in
[the roadmap](../roadmap/README.md).

## Who it's for

- **Households:** "which box has the Christmas lights?", moving house, lending things out, insurance lists.
- **Hobbyists:** parts bins and stock levels, collections with their own fields, tools and their upkeep.
- **Small businesses:** asset tags, serial numbers, checking things out to people, stocktakes, several sites.

## Principles

1. **Less typing.** Labels, scanning, photos read by AI, barcodes and an assistant over MCP come before forms. Every
   form field is optional except the name.
2. **Everything is an item.** A shed, a shelf, a U-Haul box marked "ABC", the box marked "F" inside it, and the
   baseball inside that are all items. Any item can hold other items, as deep as needed.
3. **People shape their data.** Item types and their fields are defined per space, including the type itself.
   Starter packs give a head start and can be changed freely.
4. **History is kept.** Every change is an event on the item's timeline.
5. **Built for millions of items.** Indexed search, keyset paging and no per-descendant writes when a box moves.
6. **ChefBot's habits:** your data stays on your server, plain-sentence messages, settings that save themselves,
   accessible to WCAG 2.2 AA, and docs in the same pull request as the change.

## What carries over from ChefBot

| Area | Carried over | Invy change |
|---|---|---|
| Projects | `src/Invy.Core`, `src/Invy.Api`, `src/Invy.Web`, `tests/Invy.Core.Tests`, `Invy.slnx` | none |
| Core layering | Managers → Engines → ResourceAccess, with Utilities. Only `Managers/Contracts/` and `CoreRegistration` are public. One commit per manager call through `IMessageAccess.CommitAsync`. `ArchitectureTests`, `FileSizeTests`, `JsonShapeTests`, `SchemaTests`, `MigrationTests` | none |
| HTTP | Wolverine HTTP endpoints, each a one-line call to one manager. Exceptions mapped to ProblemDetails | routes are under `/api/spaces/{spaceId}/…` |
| Messaging | Wolverine durable local queues on PostgreSQL with the EF outbox. `[StickyHandler]`, `[MessageIdentity("invy.<name>.v1")]`, the same retry and dead-letter policies | new queues `rollups`, `search`, `images`, `ai` |
| Live updates | Server-sent events at `/api/live` that send topic names only | topics are per space |
| Sign-in | Cookie sessions, `AdminPolicy` (whoever runs the install), API tokens for `/mcp`, lockout, invites | adds space roles (below) |
| Database | Bundled PostgreSQL 17 started by the app (`Database:Mode=Bundled`, or `External`), EF Core migrations applied at start | port 54339. Needs the `ltree` and `pg_trgm` extensions in both the image and the Windows zip |
| Backups | Nightly gzip JSON through `BackupAccess.Tables`, one-click restore | must stream in batches (millions of rows) |
| AI | `IAiAccess`: Anthropic SDK (default `claude-opus-5-5`) or any OpenAI-compatible service (Ollama, LM Studio). A model per task, JSON schema answers | new tasks (below) |
| MCP | `ModelContextProtocol.AspNetCore`, stateless HTTP at `/mcp`, per-person tokens (`invy_…`), tools that each call one manager | new tools (below) |
| Notifications | Webhook and Home Assistant, ICS calendar feed, scheduled jobs that schedule their own next run | reminders for warranties, upkeep, lent items, low stock |
| Web | Svelte 5 in plain JS, Vite, Lucide icons, `styles.css` tokens with `light-dark()`, header, tab row and phone bottom bar, PWA, Playwright with axe | own accent and area colours. Logo is the Lucide `Boxes` icon |
| Deployment | One container with `/data`, ports 8080 and 8443, Podman Quadlet with auto-update, Docker Compose, Windows MSI, service and portable exe, GHCR, GitHub releases | none |
| Docs | MkDocs Material at `/help` and on GitHub Pages, ChefBot's section layout and `docs/AGENTS.md` rules | none |

The one deliberate departure: ChefBot searches with `ILIKE`. Invy uses real indexes (see [Search](#search)).

## Spaces and roles

One install holds many **spaces** (for example Home, Workshop, Shop). Each space has its own items, types, views and
labels. A person can belong to several spaces and picks one in the header.

| Role | Can |
|---|---|
| Owner | everything, including members and deleting the space |
| Editor | add, change, move and remove items, types and views |
| Viewer | search and look |

The install's admin (ChefBot's `AdminPolicy`) runs the server: HTTPS, backups, AI models, logs. Being admin doesn't
grant access to a space. `ValidationEngine` checks the role on every manager call, and every space-owned query in the
accessors filters by `space_id`.

## Tailored spaces

Every space has a **kind** and a set of **features**. The kind is the starter pack it was made with. It sets the starting
types, features, suggested places, examples, getting-started steps and empty-state hints. Features are switched on and off per
space, so any space can use any feature: a home can track pantry stock, and a shop can use moving mode.

### Setup steps

First-run setup keeps steps 1 and 2. Steps 3 to 5 set up the first space, and **New space** runs the same three
steps in its modal.

| # | Step | Asks |
|---|---|---|
| 1 | Your account | The admin account |
| 2 | Secure connection | HTTPS, skipped when it's off |
| 3 | Your space | A name, and one of the kinds as cards (icon, one line, a few of its types) |
| 4 | What you'll use | The kind's feature switches, already set, one plain line each |
| 5 | Places | Likely places for the kind as chips to tick, your own, and **Add a few example items**. Skippable |

Answers stay in a draft while moving between steps, and the space is made once, when the last step finishes.

### Kinds

| Kind | Pack | Starts with |
|---|---|---|
| Home | `types-home.json` | `values`, `barcodes`, `lending`, `reminders` |
| Hobby | `types-hobby.json` | `barcodes`, `stock`, `reminders`, `projects` |
| Collection | `types-collection.json` | `values`, `lending` |
| Business | `types-business.json` | `values`, `audits`, `barcodes`, `stock`, `lending`, `reminders` |

A pack file holds everything about its kind, so adding a kind means adding one file. These fields are optional and
only add to the format: `icon`, `features`, `places` (`type` and `suggest` names), `examples` (items with a type, a place and
contents), `gettingStarted` (step keys) and `hints` (text per tab). `spaces.preset` holds the kind. Changing the kind
later changes only the hints and getting-started steps, never the types.

### Features

| Key | Shows | Built in phase |
|---|---|---|
| `values` | Value rollups and totals, the insurance report | 1, 5 |
| `audits` | The audit walk, Scan's **Audit** mode, stocktakes | 2 |
| `barcodes` | Product lookup when adding (the admin's install switch still applies) | 2 |
| `stock` | Low-stock thresholds and alerts | 6 |
| `lending` | Lending and check-out, due dates | 6 |
| `reminders` | Warranty, upkeep and expiry dates in notifications and the calendar feed | 6 |
| `moving` | Destination rooms and box manifests | 6 |
| `projects` | Reserving or using up items for a project | 6 |

- Keys are listed in `Utilities/SpaceFeatures.cs` and checked by `ValidationEngine`. A key is offered only once its
  feature is built (`Available`). The checklist for adding one is the `add-space-feature` skill.
- A feature that is off is **hidden, not blocked**. Its data and fields stay, the API and MCP tools still work, and
  switching it back on shows everything again. Its scheduled work (reminders, alerts) skips the space.
- Labels, scanning, Find, Browse, Types and Activity are always on. AI capture shows whenever the install has a
  model. Neither is a switch.

### After setup

- **Places** from step 5 are top-level containers of the pack's place type, made in the same commit as the space.
- **Example items** carry `items.is_example`. Editing one clears it. An example's place that wasn't ticked in step 5 is
  made as an example too. **Remove examples** deletes the examples that are left, and their events, in one statement,
  keeping any example that still holds something of the person's own.
- **Getting started** is a card on **Find** with the kind's steps (for a home: add your rooms, print labels, pack a box,
  try a photo, invite someone). Step keys are listed in `Utilities/GettingStartedSteps.cs`; a step that belongs to a
  feature (`values`, `audit`) is left off while that feature is off. Each step is worked out from data that already
  exists, never stored. Each member can dismiss
  it (`space_members.getting_started_dismissed`), and it hides once every step is done.
- **Hints:** empty Browse, Scan, Types and Find use the kind's `hints` text, and generic text when there is none.
- **Settings → This space:** name, kind, feature switches (owners change them, others see them), **Remove examples** and
  **Show getting started again**. **Types → Add from another pack** copies one type from any pack into the space.

## Data model

Every space-owned table carries `space_id`. Ids are `Guid.CreateVersion7()`.

| Table | Columns | Notes |
|---|---|---|
| `spaces` | id, name, preset, features (text[]), created | `preset` is the kind; `features` the keys switched on |
| `space_members` | space_id, account_id, role, getting_started_dismissed | |
| `item_types` | id, space_id, name, icon, colour, holds_items, fields (jsonb), version | All user-defined |
| `items` | id, space_id, type_id, name, marking, label_code, parent_id, path (`ltree`), depth, holds_items, quantity, unit, fields (jsonb), tags (text[]), notes, is_example, status, last_seen_at, created, updated, version, search (generated `tsvector`) | Places, containers and things |
| `item_values` | space_id, item_id, field_id, num, date, text | A typed copy of sortable field values, written in the same transaction as the item |
| `item_events` | id, space_id, item_id, at, actor, kind, from_parent, to_parent, qty_delta, data (jsonb) | Append-only timeline |
| `labels` | code, space_id, item_id (null until claimed), printed_at | QR labels |
| `views` | id, space_id, owner_id (null = shared), name, query (jsonb), display, columns, sort, pinned | Saved searches |
| `attachments` | id, space_id, item_id, kind, name, content_type, path, sha256, bytes, has_thumbnail, created_by, created_at | Files go under `Data:Root/files/<space>/<sha256>.<ext>`, not in the database, with a `.thumb.jpg` beside photos. Backups copy each file once into `<backups>/files/` |
| From ChefBot | accounts, API tokens, AI models and routes, settings, schedule state, the Wolverine schema | |

**Unique and counted items.** `quantity` is null for a unique item (a baseball, a drill with a serial number). For a
counted item it holds the amount, with an optional `unit` (200 screws, 3.5 kg of filament). A quantity change is an
event with `qty_delta`.

**Markings and labels.** `marking` is whatever is written on the thing ("ABC", "F"). `label_code` is Invy's short
code (`7K3Q`) printed in the QR label. Search finds both. A code is unique across the whole install, not just its
space, so `/l/<code>` finds the label's space by itself and opens it for anyone who is a member there. Scanning an
unclaimed label makes a container named "Box" and the code (`Box 7K3Q`) in the sticky place, unless a name or type is
given first.

**Status:** `Active`, `Missing`, `Lent`, `Disposed`, `Archived`. Disposed and archived items are left out of search
unless asked for (`is:disposed`).

### Field definitions

`item_types.fields` is a list of field definitions:

| Property | Meaning |
|---|---|
| `id` | Stable id. Values are stored by id, so renaming a field changes no items |
| `key`, `label` | The query name (`brand`) and the shown name (**Brand**) |
| `kind` | text, longtext, number, money, date, bool, select, multiselect, url, barcode, person, item |
| `unit`, `options`, `required` | Per kind. `required` is a hint shown when filling in, never a block on saving |
| `sortable` | Sortable fields are also written to `item_values`, for ranges and sorting |

Changing a type never rewrites items. Values for fields that no longer exist are kept and shown as **Other fields**.
Moving an item to another type keeps every value.

### Nesting and moves

`parent_id` is the container the item is in now. `path` is the `ltree` of ancestor ids (`shed.shelf2.abc.f`), so
"everything in the shed" is one indexed `path <@` query.

Moving an item writes one `Moved` event (`from_parent`, `to_parent`) and updates the subtree's `path` in one SQL
statement. Nothing is written for the items inside, so moving a box of 10,000 parts costs the same event as moving
one baseball. Moving an item into itself or into one of its own descendants is refused.

### Timeline

Event kinds: `Created`, `Moved`, `Changed` (field diff in `data`), `QuantityChanged`, `Seen`, `Lent`, `Returned`,
`PhotoAdded`, `Missing`, `Found`, `Disposed`, `Restored`. `actor` records the person, the token (MCP) or the AI task
that made the change.

- **An item's timeline** is its own events.
- **Where it has been** combines its own `Moved` events with those of its ancestors at the time, which gives its
  location on any date ("on 3 May the baseball was in Shed › Box ABC › Box F").
- **A container's timeline** is events where it is `from_parent` or `to_parent`: what went in and out.
- **Activity** is the space-wide feed, newest first.

Indexes are on `(item_id, at)`, `(to_parent, at)`, `(from_parent, at)`, and BRIN on `at`. The table can be
partitioned by month later without changing the accessor's contract.

## Search

People type into one box. `QueryEngine` parses the text into a query tree. `ItemAccess` turns the tree into SQL, so the
syntax never reaches the database as text.

```text
drill type:tool in:"Shed" brand:dewalt price>50 seen>1y qty<5 is:container has:photo tag:garden
```

| Part | Meaning | Index |
|---|---|---|
| plain words | name, marking, label code, notes, tags, text fields | GIN on `search`, plus `pg_trgm` GIN on name and marking for typos and partial words |
| `type:` | item type by name | btree `(space_id, type_id)` |
| `in:` | anywhere inside a container, by name, marking or code | GiST on `path` |
| `<field>:value` | exact field value | GIN `jsonb_path_ops` on `fields` |
| `<field>>n`, `<n`, `..` | ranges on number, money and date fields | btree on `item_values` |
| `seen>1y`, `added<30d` | not seen in a year, added in the last 30 days | btree on `last_seen_at`, `created` |
| `is:` / `has:` | container, counted, missing, lent, disposed / photo, barcode, label | partial indexes |

Results use keyset paging (never OFFSET). Totals above 10,000 are shown as estimates. Per-container counts and value
totals are kept up to date by a handler on the `rollups` queue, not counted on every page load. An item's value is its
type's first Money field times its amount (1 for a unique item); a container's count and total cover everything inside
it at any depth. Currencies aren't converted: a total is a plain sum.

### Views

A view is a saved query plus how to show it:

| Display | Shows |
|---|---|
| Table | chosen columns, sortable |
| Cards | photos first |
| Tree | results under their containers |
| Board | grouped by a select field (for example condition or status) |
| Timeline | the events of the matching items |

Views are personal or shared with the space. Pinned views appear on **Find**. Live topics keep an open view current.

## Capture with less typing

| Method | How it works |
|---|---|
| Blank labels | Print a sheet of QR labels before anything exists. Scanning an unclaimed label creates the item in one tap, inside the container you're in. The QR holds `https://<host>/l/<code>` |
| Scan to move | Scan an item, then scan where it's going. Done |
| Quick add | "baseball, glove, 2 bats" in a container creates three items, with quantities read from the text |
| Sticky place | "Adding to Box F" stays until changed, on every capture screen |
| Photo with AI | Photograph an open box. AI lists what it sees, matched to the space's types and fields. You tick what to keep |
| Nameplate photo | Photograph a label or nameplate to fill in brand, model and serial |
| Barcode | Scan a UPC or EAN and fill in name, brand and image from an online lookup that an admin turns on: Open Food Facts, then UPCitemdb's free trial. Only the barcode is sent |
| Inbox | Anything captured without a place or type goes to the Inbox to tidy later |
| Assistant | "Where's my baseball?" or "Put the glove in box F" through MCP, by voice or text |
| Last seen | Opening or scanning a container marks it and what's directly in it as seen: one `Seen` event on the container, the contents' `last_seen_at` set in one statement, nothing again within 10 minutes |
| Audit walk | Scan a container, tick what's there, and the rest becomes missing. Gives a discrepancy list |

Scanning uses the browser's `BarcodeDetector`, with a bundled `zxing-wasm` fallback. Nothing is loaded from a CDN.

## AI tasks

Each task can use its own model, as in ChefBot (`AiTask`, `AiRouteEntity`).

| Task | In | Out |
|---|---|---|
| `ItemsFromPhoto` | a photo, the space's types and fields | suggested items with type, quantity and field values |
| `ItemDetailsFromPhoto` | a photo of a label or nameplate, the item's type | field values (brand, model, serial) |
| `SuggestTypeFields` | a type name ("Lego sets") | suggested fields (set number, theme, pieces) |
| `SuggestType` | Inbox items, on a background sweep | a type for each, to accept or ignore |

AI answers are always suggestions that a person confirms. Nothing AI suggests is saved on its own.

The Inbox sweep keeps its answer on the item, in `suggested_type_id` and `type_suggested_at`, without changing the
item's version. It runs on the `ai` queue two minutes after things land in the Inbox, and again when someone presses
**Suggest types**. Each item is asked about once, unless someone asks again; ignoring a suggestion clears the id but
keeps the time. Accepting one changes the type as an edit does, with a `Changed` event.

## MCP tools

| Group | Tools |
|---|---|
| Finding | `find_items`, `where_is`, `get_item`, `list_contents`, `item_history` |
| Changing | `add_items`, `update_item`, `move_items`, `adjust_quantity`, `mark_seen` |
| Types and views | `list_types`, `define_type`, `list_views`, `run_view` (the views tools come with views) |
| Spaces | `list_spaces` |

Every tool takes an optional `space`, a space's name or id. Without one, a tool uses the token owner's only space;
someone in several spaces is told their names, so the assistant asks which. Tools honour space roles. A tool calls
one manager, after `McpCaller` reads the token's owner and the `space`. `where_is` answers with each match's
containers as text, `update_item` takes field values as text by search name, and `move_items` moves several things
into one container in one commit.

## Web screens

| Tab | Purpose |
|---|---|
| **Find** | The search box, recent items and pinned views. The start screen |
| **Browse** | The tree of places and containers, loaded one level at a time |
| **Scan** | Camera: open, move, claim labels, audit walk |
| **Views** | Saved views |
| **Activity** | The space's timeline |
| **Types** | Item types and their fields |
| **People** | Members and roles |
| **Settings** | This space (kind and features), spaces, labels and printing, AI assistants, barcode lookup, notifications, backups, HTTPS, logs |

Before the tabs come the setup steps (see [Tailored spaces](#setup-steps)). A feature that is off hides its tabs,
buttons and sections.

On phones the bottom bar shows **Find**, **Browse**, **Scan** (in the middle), **Views** and **More**.

The item page shows the location chain (*Shed › Shelf 2 › Box ABC › Box F*), contents, fields, photos and the
timeline. Long lists scroll virtually. Search runs as you type, 150 ms after the last key.

## Ideas by audience

**Households:** moving mode (pack boxes, give each box a destination room, print a manifest per box), seasonal
rotation, lending with reminders, warranty, receipt and manual attachments, an insurance report (PDF or CSV, with
photos and values, totalled by room).

**Hobbyists:** parts bins with low-stock thresholds and notifications, collections with custom fields and value
totals, projects that reserve or use up items, tool upkeep dates in the calendar feed.

**Small businesses:** asset tags and serial numbers, check-out and check-in to a person or site, assigned-to and
condition fields, purchase date and depreciation, several sites, scheduled stocktakes, CSV export, the HTTP API.

**Imports:** CSV, and Homebox backups (as ChefBot imports Mealie).

**Starter packs:** `config/types-home.json`, `config/types-hobby.json`, `config/types-collection.json` and
`config/types-business.json`, one per kind (see [Tailored spaces](#tailored-spaces)). Each idea above hides behind a
feature switch, so a space shows only the ones it uses.

## Scale targets

- 5 million items and 50 million events in one install.
- Search p95 under 200 ms and an item timeline under 100 ms, on a Raspberry Pi 5-class machine.
- Moving a container costs the same whatever it holds, apart from one subtree `path` update.
- Backups and imports stream in batches and never load a whole table into memory. Imports use `COPY`.

Phase 7 adds a seed tool and a benchmark that check these numbers.
