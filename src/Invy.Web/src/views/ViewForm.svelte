<!-- Makes or changes a saved view: its name, search, display, table columns, sort, board grouping, and whether it is
     shared and pinned to Find. Editors and owners can share; anyone can keep a view to themselves. -->
<script>
  import { untrack } from 'svelte';
  import { app, run, canEdit } from '../lib/app.svelte.js';
  import { api } from '../lib/api.js';
  import { has } from '../lib/features.js';
  import { pending } from '../lib/pending.js';
  import { DISPLAYS, BUILT_IN, DEFAULT_COLUMNS, fieldColumns, boardFields } from '../lib/views.js';
  import Modal from '../components/Modal.svelte';

  /**
   * view: the ViewResponse to change, or null for a new one. start: what a new one starts from (a copy of a view, or
   * { query } from Find). types: the space's types. onsaved(view), onclose().
   */
  let { view = null, start = null, types, onsaved, onclose } = $props();

  // The form starts from what it was opened with; later changes to the props don't reset what was typed.
  const editing = untrack(() => view);
  const from = untrack(() => view ?? start ?? {});
  let name = $state(editing ? editing.name : from.name ? `${from.name} (my copy)` : '');
  let query = $state(from.query ?? '');
  let shown = $state(from.display ?? 'Table');
  let columns = $state(from.columns?.length ? [...from.columns] : [...DEFAULT_COLUMNS]);
  let sort = $state(from.sort ?? '');
  let groupBy = $state(from.groupBy ?? '');
  let shared = $state(editing ? editing.shared : false);
  let pinned = $state(from.pinned ?? false);

  const offered = $derived([...BUILT_IN.filter((column) => !column.feature || has(column.feature)), ...fieldColumns(types)]);
  const groups = $derived(boardFields(types));
  const hint = $derived(DISPLAYS.find((entry) => entry.key === shown)?.hint ?? '');

  function toggle(key, on) {
    columns = on ? [...columns, key] : columns.filter((column) => column !== key);
  }

  function save(event) {
    event.preventDefault();
    const body = {
      name, query, display: shown, columns: shown === 'Table' ? columns : [], sort: sort || null,
      groupBy: shown === 'Board' ? groupBy || null : null, shared, pinned,
    };
    return pending(event, () => run(async () => {
      const base = `/api/spaces/${app.spaceId}/views`;
      onsaved(view ? await api('PUT', `${base}/${view.id}`, body) : await api('POST', base, body));
    }));
  }
</script>

<Modal title={view ? `Change ${view.name}` : 'New view'} wide {onclose}>
  <form class="stack" onsubmit={save}>
    <label>Name
      <input bind:value={name} required maxlength="100" placeholder="Not seen in a year" autocomplete="off" />
    </label>
    <label>Search
      <input bind:value={query} placeholder="type:tool seen>1y" autocomplete="off" spellcheck="false" aria-describedby="view-query-hint" />
    </label>
    <p id="view-query-hint" class="muted">Written as in the Find box. It runs again each time, so seen&gt;1y always counts back from today. Empty finds everything.</p>

    <label>Show as
      <select bind:value={shown} aria-describedby="view-display-hint">
        {#each DISPLAYS as entry (entry.key)}
          <option value={entry.key} disabled={entry.key === 'Board' && groups.length === 0}>{entry.label}</option>
        {/each}
      </select>
    </label>
    <p id="view-display-hint" class="muted">{hint}{groups.length === 0 ? ' A board needs a choice field on a type.' : ''}</p>

    {#if shown === 'Table'}
      <fieldset>
        <legend>Columns</legend>
        <div class="options">
          {#each offered as column (column.key)}
            <label class="option"><input type="checkbox" checked={columns.includes(column.key)}
              onchange={(event) => toggle(column.key, event.currentTarget.checked)} />{column.label}</label>
          {/each}
        </div>
      </fieldset>
    {/if}
    {#if shown === 'Board'}
      <label>Group by
        <select bind:value={groupBy} required>
          <option value="" disabled>Choose a field</option>
          {#each groups as group (group.key)}<option value={group.key}>{group.label}</option>{/each}
        </select>
      </label>
    {/if}
    {#if shown !== 'Timeline'}
      <label>Order
        <select bind:value={sort}>
          <option value="">Best match, or by name without words</option>
          <option value="name">Name</option>
          <option value="recent">Recently changed</option>
        </select>
      </label>
    {/if}

    <label class="check"><input type="checkbox" class="switch" bind:checked={pinned} />
      <span>Pin to Find<span class="muted block">{shared ? 'It shows on everyone’s Find.' : 'It shows on your Find.'}</span></span></label>
    <label class="check"><input type="checkbox" class="switch" bind:checked={shared} disabled={!canEdit()} />
      <span>Share with everyone in this space<span class="muted block">{canEdit()
        ? 'Editors and owners can change a shared view.' : 'Editors and owners share views. This one is just for you.'}</span></span></label>

    <div class="actions">
      <button type="submit" class="primary">{view ? 'Save' : 'Make view'}</button>
      <button type="button" onclick={onclose}>Cancel</button>
    </div>
  </form>
</Modal>

<style>
  fieldset { border: 1px solid var(--line); border-radius: .4rem; padding: .5rem .75rem; margin: 0; }
  legend { font-size: .85rem; color: var(--muted); padding: 0 .25rem; }
  .options { display: flex; flex-wrap: wrap; gap: .3rem 1rem; }
  .option { display: inline-flex; align-items: center; gap: .35rem; color: var(--ink); font-size: .9rem; }
  .check { color: var(--ink); font-size: .95rem; margin: 0; }
  .block { display: block; }
  .stack > p.muted { margin-top: -.35rem; }
</style>
