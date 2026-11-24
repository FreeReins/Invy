---
name: add-space-feature
description: "Put a Invy feature behind a per-space switch (values, audits, barcodes, stock, lending, reminders, moving, projects): the catalogue entry, kind defaults, hidden UI, paused scheduled work, the Settings switch, a getting-started step, tests and docs. USE FOR: new space feature, feature switch, hide a feature in spaces that don't use it, change which kinds turn a feature on. DO NOT USE FOR: install-wide admin settings, the feature's own endpoints (add-core-operation) or screens (add-web-screen)."
---

# Add a space feature

`<key>` is the feature's key, for example `lending`. A space's switched-on keys are in `spaces.features`. The
kind (starter pack) only picks the defaults. A feature that is off is **hidden, not blocked**: its data, fields,
API and MCP tools stay, and only its UI and scheduled work stop.

1. **Catalogue**: in `src/Invy.Core/Utilities/SpaceFeatures.cs`, the key, name and one plain line for the switch
   ("Lend things to people and get reminded when they're due"), with `Available = true` once the feature works.
   Never rename a key: stored spaces hold it.
2. **Defaults**: add the key to the `features` list of each `config/types-*.json` kind that should start with it.
   Spaces that already exist keep their switches.
3. **Hide the UI**: wrap each tab, button, item-page section and Scan mode in `has('<key>')` from
   `src/Invy.Web/src/lib/features.js`. Search for the feature's components so none are missed.
4. **Scheduled work**: jobs and handlers that notify (reminders, alerts) skip spaces without the key. Read it
   through the space accessor, never by asking another manager.
5. **Getting started**: if the feature has a first thing to do, add a step key to the kind's `gettingStarted` list
   and work it out in the getting-started query from data that already exists.
6. **Tests**: a TestCore test showing the scheduled work skips a space that has the feature off, and a web test
   showing the UI hidden and shown (stubbed space in `src/Invy.Web/tests/data.js`).
7. **Docs**: the feature's row in the features table in `docs/using/spaces.md`, and a line in the feature's own page
   saying which switch shows it.

## Troubleshooting

- The switch doesn't show in setup or Settings: `Available` is still false.
- "There is no feature called …": the key is in a pack file but not in `SpaceFeatures`.
