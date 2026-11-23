# Phase 1: core inventory

**Goal:** people can make spaces, define types, add items inside items to any depth, move them, see each item's
timeline, and find anything by search.

**Design sections:** [Spaces and roles](../developers/design.md#spaces-and-roles),
[Data model](../developers/design.md#data-model), [Search](../developers/design.md#search) (not Views),
[Web screens](../developers/design.md#web-screens).

**Out of scope:** labels and scanning (phase 2), AI and MCP (phase 3), saved views (phase 4), lending and stock
alerts (phase 5).

## 1a Spaces and types

- [ ] `Space`, `SpaceMember` entities and a `SpaceManager` (create, rename, members, roles). Follow `add-stored-entity`, then `add-core-operation`.
- [ ] Role checks in `ValidationEngine.Spaces.cs`, and a test for each role.
- [ ] `ItemType` with field definitions in jsonb, through `TypeManager`. `ValidationEngine` checks field kinds, unique keys and select options.
- [ ] Starter packs `config/types-home.json`, `types-hobby.json` and `types-business.json`, chosen when a space is created.
- [ ] Web: the space picker in the header, the **Types** tab with its field editor, and **People**. Follow `add-web-screen`.

## 1b Items, nesting and the timeline

- [ ] `Item` and `ItemEvent` entities, `ItemAccess` with the `ltree` path, and an `ItemManager` with create, update, move, adjust quantity, dispose and restore.
- [ ] Move: one event plus one subtree `path` update. Refuse a move into the item itself or its own subtree. Tests cover a three-level move (ABC › F › baseball).
- [ ] `item_values` written for sortable fields in the same transaction.
- [ ] A `TimelineEngine` builds "where it has been" from the item's moves and its ancestors' moves. Tests cover locations on past dates.
- [ ] Quick add: a parser engine reads "baseball, glove, 2 bats" into items. Inbox: items with no place and no type.
- [ ] Web: **Browse** (tree, one level at a time), the item page (chain, contents, fields, timeline), and **Activity**.

## 1c Search

- [ ] `QueryEngine` parses the syntax in the design's Search table into a query tree, with tests for every row of that table.
- [ ] `ItemAccess.SearchAsync` builds SQL from the tree, with keyset paging. Migration with the `tsvector`, trigram, jsonb, `path` and `item_values` indexes.
- [ ] `rollups` queue handler for container counts and value totals, run after items change.
- [ ] Web: **Find**, with search as you type (150 ms), recent items and virtual scrolling.
- [ ] Docs: `docs/using/` pages for spaces, types, items and search, and `docs/reference/api.md` rows.
- [ ] Remove the "Until phase … follow ChefBot's …" fallbacks from the skills' examples (`add-stored-entity`, `add-core-operation`, `add-web-screen`) now that the Invy classes exist.

**Done when:** the build and the tests pass, `./dev.sh web-test` passes, and in a dev run you can make the shed ›
box ABC › box F › baseball chain, move box F to the garage, find "baseball" and see where it has been.

## Session log
