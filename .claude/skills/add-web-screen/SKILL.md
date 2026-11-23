---
name: add-web-screen
description: "Add a tab or screen to Invy's Svelte page: tab list, routing, area colour, test stubs, accessibility rows, screenshots. USE FOR: new tab, new view, new screen state, screenshot the web page. DO NOT USE FOR: backend endpoints (add-core-operation), small edits inside one component."
---

# Add a web screen

All paths are under `src/Invy.Web/`.

1. **Component** `src/tabs/<Tab>.svelte`: load in `onMount(() => { run(() => load()); })` with `api()` from
   `lib/api.js`. Keep state local unless another screen needs it (then the area's `lib/<area>.svelte.js`, re-exported by `lib/app.svelte.js`). An empty state
   uses `<div class="card empty">` with `.empty-icon`. Use `<h2>` for the tab title and `<h3>` below it.
2. **Tab**: in `src/App.svelte` add an import, a `TABS` entry (Lucide icon from `@lucide/svelte/icons/<name>`;
   check `node_modules/@lucide/svelte/dist/icons`), and `TAB_VIEWS`. In `src/lib/addresses.js` add the key to
   `TAB_KEYS` and the address comment. A role-only tab: copy ChefBot's `adminOnly`/`ADMIN_TABS`.
3. **Colour**: `--area-<tab>` in `src/styles.css` (the design's tabs have one already), 4.5:1 in both themes.
4. **Stubs**: a shape in `tests/data.js` mirroring the C# record, a default route in `defaults()` in
   `tests/fixtures.js`, and `VIEWS` rows in `tests/a11y.spec.js`. Routes match the path only; a stub that depends
   on query values is a function: `(request) => new URL(request.url()).searchParams...`.
5. **Check**: `./dev.sh web`, `./dev.sh web-test --grep "<tab>"`, then the full `./dev.sh web-test`.

## Example

`src/tabs/Find.svelte` (an empty state); for a list with stubs, ChefBot's `tabs/History.svelte`.

## Screenshot

`./dev.sh web-shot /<tab>`, then read `src/Invy.Web/shots/<tab>-*.png` (answered from the test stubs). Delete
`shots/` afterwards.

## Docs

Document the screen in its page under `docs/` (the table in `docs/AGENTS.md` says which), following the
`update-docs` skill. A new Settings section gets a `help` entry in `SECTIONS` (`src/tabs/Settings.svelte`) pointing at
its guide; elsewhere use `<HelpLink page="…">` from `src/components/HelpLink.svelte`.

## Troubleshooting

- Svelte drops a space before `{#if}`; put the separator inside an expression: ``{` · ${text}`}``.
- A keyed `{#each}` throws on duplicate keys when older pages repeat items.
- Entrance animations are fine (axe waits). Respect `prefers-reduced-motion`.
