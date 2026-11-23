# Phase 1: core inventory

**Goal:** people can make spaces, define types, add items inside items to any depth, move them, see each item's
timeline, and find anything by search.

**Design sections:** [Spaces and roles](../developers/design.md#spaces-and-roles),
[Data model](../developers/design.md#data-model), [Search](../developers/design.md#search) (not Views),
[Web screens](../developers/design.md#web-screens).

**Out of scope:** labels and scanning (phase 2), AI and MCP (phase 3), saved views (phase 4), lending and stock
alerts (phase 5).

## 1a Spaces and types

- [x] `Space`, `SpaceMember` entities and a `SpaceManager` (create, rename, members, roles). Follow `add-stored-entity`, then `add-core-operation`.
- [x] Role checks in `ValidationEngine.Spaces.cs`, and a test for each role.
- [x] `ItemType` with field definitions in jsonb, through `TypeManager`. `ValidationEngine` checks field kinds, unique keys and select options.
- [x] Starter packs `config/types-home.json`, `types-hobby.json` and `types-business.json`, chosen when a space is created.
- [x] Web: the space picker in the header, the **Types** tab with its field editor, and **People**. Follow `add-web-screen`.

## 1b Items, nesting and the timeline

- [x] `Item` and `ItemEvent` entities, `ItemAccess` with the `ltree` path, and an `ItemManager` with create, update, move, adjust quantity, dispose and restore.
- [x] Move: one event plus one subtree `path` update. Refuse a move into the item itself or its own subtree. Tests cover a three-level move (ABC › F › baseball).
- [x] `item_values` written for sortable fields in the same transaction.
- [x] A `TimelineEngine` builds "where it has been" from the item's moves and its ancestors' moves. Tests cover locations on past dates.
- [x] Quick add: a parser engine reads "baseball, glove, 2 bats" into items. Inbox: items with no place and no type.
- [x] Web: **Browse** (tree, one level at a time), the item page (chain, contents, fields, timeline), and **Activity**.

## 1c Search

- [x] `QueryEngine` parses the syntax in the design's Search table into a query tree, with tests for every row of that table.
- [x] `ItemAccess.SearchAsync` builds SQL from the tree, with keyset paging. Migration with the `tsvector`, trigram, jsonb, `path` and `item_values` indexes.
- [x] `rollups` queue handler for container counts and value totals, run after items change.
- [x] Web: **Find**, with search as you type (150 ms), recent items and virtual scrolling.
- [x] Docs: `docs/using/` pages for spaces, types, items and search, and `docs/reference/api.md` rows.
- [x] Remove the "Until phase … follow ChefBot's …" fallbacks from the skills' examples (`add-stored-entity`, `add-core-operation`, `add-web-screen`) now that the Invy classes exist.

**Done when:** the build and the tests pass, `./dev.sh web-test` passes, and in a dev run you can make the shed ›
box ABC › box F › baseball chain, move box F to the garage, find "baseball" and see where it has been.

## Session log

- 2026-11-23 (1a): spaces with members and roles (`SpaceManager`, `ValidationEngine.SpaceRoleAsync`), item types with jsonb fields and a version check (`TypeManager`), starter packs `config/types-{home,hobby,business}.json` picked at create, and the web space picker, **Types** (field editor) and **People** tabs; `docs/using/spaces.md` and `types-and-fields.md` written early for the in-app help links. Table and column names follow the existing PascalCase (`Spaces`, `SpaceId`), not design.md's snake_case. Owners rename and delete a space on **People**, not yet in Settings. Left for 1c's docs box: `docs/reference/api.md` (doesn't exist yet) and the `docs/developers/data-model.md` the add-stored-entity skill names. Next: 1b, `Item` and `ItemEvent` entities with `ItemAccess` and the `ltree` path.
- 2026-11-23 (1b): `Items` (ltree `Path` of the ancestors' ids and its own, as 32-hex labels), `ItemEvents` and `ItemValues`; `ItemManager` with create, update (Changed event with a before/after list), move (one event, plus one subtree `UPDATE` that `ItemAccess.MoveContentsAsync` runs in a transaction it opens and `MessageAccess.CommitAsync` finishes; checked in a dev run with SQL logging: BEGIN, UPDATE, INSERT event, UPDATE item, COMMIT), adjust quantity, dispose (container must be empty), restore, quick add (`QuickAddEngine`) and the Inbox (no parent, no type, not a container); `TimelineEngine` for where it has been; web **Browse** (`/browse`, `/browse/{id}` item page with chain, contents, fields, quick add, move dialog, timeline) and **Activity**; `docs/using/items.md` and `timeline.md`. Fixed a 1a bug the dev run found: Wolverine couldn't resolve the new managers (500 on every space call); `ResolveInvyServicesFromContainer` now registers every `I*Manager`. Item-kind field values can't be picked in the form yet (needs search). Next: 1c, `QueryEngine` parsing the Search table into a query tree.
- 2026-11-23 (1c): `QueryEngine` reads the Search table (plus OR, `-`, quotes, name:/marking:/notes:, dates and times ago) into an `ItemQuery` tree with names resolved to ids; `SearchSql` turns it into parameterised SQL (prefix tsquery on a generated `Search` tsvector, ILIKE and `<%` trigram for half-typed and misspelt words), keyset paging for relevance, name and recent, a count capped at 10,000 with the planner's estimate above it; `AddSearch` migration with the tsvector, trigram, jsonb_path_ops, tags, item_values and partial indexes. Rollups: `ItemsChanged` on the `rollups` queue recounts `ContentCount`/`ContentValue` of every container above a change (value = first Money field × amount, per the decision). Web **Find**: search as you type (150 ms), recently changed when empty, `VirtualList`, unreadable searches explained under the box. Docs: `using/search.md`, `reference/api.md` (every endpoint), README screenshots retaken. Done when checked in a dev run against PostgreSQL 17 through the API (chain, move F to the garage, find "baseball", where it has been), not by clicking through the page. Tags are now kept in lower case. Next: phase 2 (capture).
