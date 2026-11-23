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

## Data model

Every space-owned table carries `space_id`. Ids are `Guid.CreateVersion7()`.

| Table | Columns | Notes |
|---|---|---|
| `spaces` | id, name, preset, created | |
| `space_members` | space_id, account_id, role | |
| `item_types` | id, space_id, name, icon, colour, holds_items, fields (jsonb), version | All user-defined |
| `items` | id, space_id, type_id, name, marking, label_code, parent_id, path (`ltree`), depth, holds_items, quantity, unit, fields (jsonb), tags (text[]), notes, status, last_seen_at, created, updated, version, search (generated `tsvector`) | Places, containers and things |
| `item_values` | space_id, item_id, field_id, num, date, text | A typed copy of sortable field values, written in the same transaction as the item |
| `item_events` | id, space_id, item_id, at, actor, kind, from_parent, to_parent, qty_delta, data (jsonb) | Append-only timeline |
| `labels` | code, space_id, item_id (null until claimed), printed_at | QR labels |
| `views` | id, space_id, owner_id (null = shared), name, query (jsonb), display, columns, sort, pinned | Saved searches |
| `attachments` | id, space_id, item_id, kind, path, sha256, bytes | Files go under `Data:Root/files/<space>/`, not in the database |
| From ChefBot | accounts, API tokens, AI models and routes, settings, schedule state, the Wolverine schema | |

**Unique and counted items.** `quantity` is null for a unique item (a baseball, a drill with a serial number). For a
counted item it holds the amount, with an optional `unit` (200 screws, 3.5 kg of filament). A quantity change is an
event with `qty_delta`.

**Markings and labels.** `marking` is whatever is written on the thing ("ABC", "F"). `label_code` is Invy's short
code (`7K3Q`) printed in the QR label. Search finds both.

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
totals are kept up to date by a handler on the `rollups` queue, not counted on every page load.

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
| Barcode | Scan a UPC or EAN and fill in name, brand and image from an online lookup that you turn on |
| Inbox | Anything captured without a place or type goes to the Inbox to tidy later |
| Assistant | "Where's my baseball?" or "Put the glove in box F" through MCP, by voice or text |
| Last seen | Opening or scanning a container marks it and what's directly in it as seen |
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

## MCP tools

| Group | Tools |
|---|---|
| Finding | `find_items`, `where_is`, `get_item`, `list_contents`, `item_history` |
| Changing | `add_items`, `update_item`, `move_items`, `adjust_quantity`, `mark_seen` |
| Types and views | `list_types`, `define_type`, `list_views`, `run_view` |
| Spaces | `list_spaces` |

Every tool takes an optional `space`, defaulting to the token owner's default space. Tools honour space roles.

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
| **Settings** | Spaces, labels and printing, AI assistants, barcode lookup, notifications, backups, HTTPS, logs |

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

**Starter packs:** `config/types-home.json`, `config/types-hobby.json` and `config/types-business.json`, chosen when a
space is created.

## Scale targets

- 5 million items and 50 million events in one install.
- Search p95 under 200 ms and an item timeline under 100 ms, on a Raspberry Pi 5-class machine.
- Moving a container costs the same whatever it holds, apart from one subtree `path` update.
- Backups and imports stream in batches and never load a whole table into memory. Imports use `COPY`.

Phase 6 adds a seed tool and a benchmark that check these numbers.
