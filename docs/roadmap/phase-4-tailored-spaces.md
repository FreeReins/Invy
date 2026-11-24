# Phase 4: tailored spaces

**Goal:** setup asks what the first space is for, and from then on the space shows what it's used for and hides what
it isn't. A home, a workshop, a collection and a shop each start differently, and any of them can switch on a feature
from another.

**Design sections:** [Tailored spaces](../developers/design.md#tailored-spaces),
[Spaces and roles](../developers/design.md#spaces-and-roles), [Web screens](../developers/design.md#web-screens).

**Out of scope:** the features that later phases build (value reports, lending, stock, reminders, moving, projects).
This phase adds the switches and their defaults. Each later feature hides behind its own switch through the
`add-space-feature` skill when it's built. Setup steps 1 (account) and 2 (HTTPS) stay as they are.

## 4a Kinds and features in Core

- [x] Migration `AddSpaceFeatures`: `spaces.features text[]`, `items.is_example bool` (partial index on
  `(space_id) where is_example`), `space_members.getting_started_dismissed bool`. Existing spaces get their kind's
  default features, and spaces made with no pack get none.
- [x] `Utilities/SpaceFeatures.cs`: the feature catalogue (key, name, one-line description, `Available`). For now
  `values`, `audits` and `barcodes` are available. `stock`, `lending`, `reminders`, `moving` and `projects` are listed
  but not offered. `ValidationEngine.Features` refuses unknown keys with a plain sentence.
- [x] Starter packs gain optional `icon`, `features`, `places`, `examples`, `gettingStarted` and `hints`
  (`StarterPackAccess` and its JSON shape). Fill them in for home, hobby and business. New `config/types-collection.json`
  (Display case, Binder and Set hold items, plus Collectible), and the hobby pack's Collectible moves into it.
- [x] `CreateSpaceRequest` gains `Features` (null means the kind's defaults), `Places` and `Examples`.
  `SpaceManager.CreateSpaceAsync` makes the places (as containers of the pack's place type) and the examples
  (`IsExample`) in the same commit. `SpaceResponse` and `StarterPackResponse` gain the new fields, which are only added.
- [x] Operations, following `add-core-operation`: `PUT /api/spaces/{id}/features` (owner), `PUT /api/spaces/{id}/kind`
  (owner, changes only the hints and checklist, never the types), `DELETE /api/spaces/{id}/examples` (editor),
  `GET /api/spaces/{id}/getting-started` (worked out from existing data) and `POST .../getting-started/dismiss` (the
  caller's own membership). Editing an example clears `IsExample`.
- [x] TestCore tests: create with a kind (default and chosen features, places, examples), unknown feature and kind
  refused, a viewer refused on features, remove examples (and only examples), getting-started flags, dismiss for one
  member only, and the migration default for an old space.

## 4b Setup steps and feature switches in the web

- [x] `SETUP_STEPS` gains **Your space**, **What you'll use** and **Places** after HTTPS, with screens
  `SetupSpace.svelte`, `SetupFeatures.svelte` and `SetupPlaces.svelte`, and answers kept in `app.spaceDraft`. The space is made
  when the last step finishes, which then opens Find. Coming back to a step afterwards shows the space that was made
  and links to Settings.
- [x] `NewSpaceDialog.svelte` runs the same three steps inside the modal, using the same step components.
- [x] `lib/features.js`: `has(key)` for the current space. The audit walk, Scan's Audit mode and barcode lookup in
  Add are hidden when their feature is off. Value totals are hidden when `values` is off.
- [x] Web tests (Playwright with axe, APIs stubbed): the five setup steps for each kind, going back and forward keeps
  the draft, the new-space modal, and the audit and barcode UI hidden or shown by feature. Add a11y rows for each new
  state (`add-web-screen`).

## 4c After setup

- [x] A getting-started card on **Find**: the kind's steps, each ticked from the API, with **Remove examples** while
  examples are left, and **Dismiss**. It refreshes on the space's live topics and hides when every step is done.
- [x] Empty states on Browse, Scan, Types and Find use the kind's `hints` when the pack has them.
- [x] Settings → **This space** (`settings/SpacePanel.svelte`): name, kind, feature switches (owners change, others
  see), **Remove examples**, **Show getting started again**.
- [x] Types → **Add from another pack**: copy one type from any starter pack into this space.
- [x] Web tests for the card, hints, the panel (owner and viewer) and Add from another pack.
- [x] Docs (`update-docs`): `getting-started/first-run.md`, `using/spaces.md`, `using/types-and-fields.md` (the
  collection pack, Add from another pack), `reference/api.md`, `admin/upgrading.md` and
  `developers/data-model.md` (the migration), with screenshots taken by the web tests.

**Done when:** everything here is checked automatically, with no steps done by hand. The build has no warnings. The
TUnit tests and the web tests (with axe) above pass. The strict docs build is clean.

## Session log

- 2026-11-24 (4a): kinds and features in Core: `AddSpaceFeatures` migration (old spaces get their kind's defaults from
  `AddSpaceFeatures.Defaults`), `SpaceFeatures` and `GettingStartedSteps` catalogues, pack fields and the collection
  pack, places and examples made with the space, the features/kind/examples/getting-started endpoints plus
  `GET /api/space-features` and `POST .../getting-started/show` (for 4c), `SpaceKindTests`. Checked once against a
  throwaway PostgreSQL. API rows added to `docs/reference/api.md`; the other docs wait for 4c. Next: 4b, the setup
  steps in the web.
- 2026-11-24 (4b, 4c): setup steps Your space, What you'll use and Places (`SetupSpace`, `SetupFeatures`,
  `SetupPlaces`, draft in `app.spaceDraft`, space made on Finish), the same steps in `NewSpaceDialog`,
  `lib/features.js` hiding Audit, Scan's Audit mode, barcode lookup and the new Worth lines on the item page. Then
  the getting-started card on Find (refreshed by a new `items` live topic), kind hints on empty Browse, Scan, Types
  and Find, Settings → Kind and features (`SpacePanel`), Types → Add from another pack with
  `POST /api/spaces/{id}/types/from-pack`, and web tests in `spaces.spec.js` plus a11y rows. Docs written
  (first-run, spaces, types-and-fields, api, upgrading, new `developers/data-model.md`); strict builds clean.
  Half-done: the docs box asks for screenshots taken by the web tests, but no docs page has screenshots and there is
  no pipeline for them. Next: decide on docs screenshots with the user, then tick the docs box and set phase 4 Done.
- 2026-11-24 (4c): docs screenshots pipeline: `./dev.sh docs-shots` runs `tests/docs-shots.spec.js` (light, wide) and
  moves the shots into `docs/images/`; first-run, spaces and types-and-fields show six of them. The web test stubs'
  starter packs and feature catalogue now match `config/types-*.json` and `SpaceFeatures.cs`. Phase 4 done.
  Next: phase 5, 6 or 7 (any order).
