<!-- Settings, This space, Import: upload a CSV file or Homebox's CSV export, check where each column goes (with the
     file's first rows), pick the container and type, and start. It runs in the background; the list shows how far
     each import got and the rows it skipped. Editors import; everyone sees the list. -->
<script>
  import { untrack } from 'svelte';
  import { app, run, toast, canEdit, onLive } from '../lib/app.svelte.js';
  import { api } from '../lib/api.js';
  import { pending, pendingClick } from '../lib/pending.js';
  import { base64 } from '../lib/photos.js';
  import { fullDate } from '../lib/format.js';
  import PlacePicker from '../items/PlacePicker.svelte';
  import Upload from '@lucide/svelte/icons/upload';

  const TARGETS = [
    ['name', 'Name'], ['marking', 'Marking'], ['quantity', 'Amount'], ['unit', 'Unit'], ['tags', 'Tags'],
    ['notes', 'Notes'], ['note', 'A line in the notes'], ['location', 'Location (Garage / Shelf 2)'], ['type', 'Type, by name'],
    ['ignore', 'Leave out'],
  ];
  const MAX_MB = 20;

  let imports = $state(null);
  let types = $state([]);
  /** The upload being checked: an ImportResponse in Draft, with its mapping as { [column]: target }. */
  let draft = $state(null);
  let mapping = $state({});
  let typeId = $state('');
  let place = $state(null);
  let picking = $state(false);
  let file = $state(null);

  const base = $derived(`/api/spaces/${app.spaceId}/imports`);
  const type = $derived(types.find((candidate) => candidate.id === typeId) ?? null);
  const running = $derived(imports?.some((entry) => entry.status === 'Queued' || entry.status === 'Running') ?? false);

  const load = () => run(async () => {
    [imports, types] = await Promise.all([api('GET', base), api('GET', `/api/spaces/${app.spaceId}/types`)]);
  });
  $effect(() => {
    void app.spaceId;
    untrack(() => { draft = null; imports = null; load(); });
  });
  $effect(() => onLive('items', () => { if (running) load(); }));
  // While one runs, check on it every couple of seconds.
  $effect(() => {
    if (!running) return undefined;
    const timer = setInterval(() => load(), 2000);
    return () => clearInterval(timer);
  });

  function upload(event) {
    event.preventDefault();
    if (!file) return undefined;
    if (file.size > MAX_MB * 1024 * 1024) {
      toast(`That file is over ${MAX_MB} MB. Split it and import each part.`);
      return undefined;
    }
    return pending(event, () => run(async () => {
      draft = await api('POST', base, { name: file.name, data: await base64(file) });
      mapping = Object.fromEntries(draft.mapping.map((entry) => [entry.column, entry.target]));
      typeId = '';
      place = null;
    }));
  }

  function start(event) {
    event.preventDefault();
    const request = {
      mapping: draft.headers.map((_, column) => ({ column, target: mapping[column] ?? 'ignore' })),
      parentId: place?.id ?? null, typeId: typeId || null,
    };
    return pending(event, () => run(async () => {
      await api('POST', `${base}/${draft.id}/start`, request);
      toast(`Importing ${draft.fileName}. It carries on if you leave this page.`);
      draft = null;
      await load();
    }));
  }

  const remove = (entry) => run(async () => {
    await api('DELETE', `${base}/${entry.id}`);
    if (draft?.id === entry.id) draft = null;
    await load();
  });

  function retarget() {
    // A field of the old type can't stay mapped once the type changes.
    for (const [column, target] of Object.entries(mapping)) {
      if (target.startsWith('field:') && !type?.fields.some((field) => `field:${field.id}` === target)) mapping[column] = 'note';
    }
  }
</script>

<h2>Import</h2>
<p class="muted">Bring in a list from a spreadsheet as a CSV file, or Homebox's CSV export. Locations become containers, nested inside the place you pick.</p>

{#if canEdit() && !draft}
  <form class="card upload" onsubmit={upload}>
    <label>CSV file <input type="file" accept=".csv,text/csv" required onchange={(event) => { file = event.currentTarget.files?.[0] ?? null; }} /></label>
    <button type="submit" class="primary with-icon"><Upload size={16} aria-hidden="true" />Upload and check</button>
  </form>
{/if}

{#if draft}
  <form class="card stack" onsubmit={start}>
    <h3>{draft.fileName} <span class="chip info">{draft.kind === 'Homebox' ? 'Homebox export' : 'CSV'}</span></h3>
    <p class="muted">{draft.total} rows. Check where each column goes; the first rows are shown to help.</p>
    <div class="pair">
      <label>Type for every row <select bind:value={typeId} onchange={retarget}>
        <option value="">No type</option>
        {#each types as option (option.id)}<option value={option.id}>{option.name}</option>{/each}
      </select></label>
      <div class="place">
        <span class="muted">Put them in</span>
        <span><strong>{place?.name ?? 'The top of the space'}</strong>
          <button type="button" class="small" onclick={() => { picking = true; }}>Choose</button></span>
      </div>
    </div>
    <div class="table-wrap">
      <table>
        <thead>
          <tr>{#each draft.headers as header, column (column)}<th scope="col">{header || `Column ${column + 1}`}</th>{/each}</tr>
          <tr>
            {#each draft.headers as header, column (column)}
              <td><select aria-label="Where {header || `column ${column + 1}`} goes" bind:value={mapping[column]}>
                {#each TARGETS as [target, text] (target)}<option value={target}>{text}</option>{/each}
                {#each type?.fields ?? [] as field (field.id)}<option value="field:{field.id}">{field.label} field</option>{/each}
              </select></td>
            {/each}
          </tr>
        </thead>
        <tbody>
          {#each draft.sample as sampleRow, index (index)}
            <tr>{#each draft.headers as _, column (column)}<td>{sampleRow[column] ?? ''}</td>{/each}</tr>
          {/each}
        </tbody>
      </table>
    </div>
    <div class="actions">
      <button type="submit" class="primary">Import {draft.total} rows</button>
      <button type="button" onclick={pendingClick(() => remove(draft))}>Cancel</button>
    </div>
  </form>
{/if}

<h3>Imports</h3>
{#if imports === null}
  <div class="card"><span class="skeleton line"></span></div>
{:else if imports.filter((entry) => entry.status !== 'Draft').length === 0}
  <p class="muted">Nothing imported yet.</p>
{:else}
  <ul class="card plain list" aria-label="Imports">
    {#each imports.filter((entry) => entry.status !== 'Draft') as entry (entry.id)}
      <li>
        <div class="row">
          <span><strong>{entry.fileName}</strong> <span class="muted">{fullDate(null, entry.createdAt)}</span></span>
          <span class="chip" class:good={entry.status === 'Done'} class:warn={entry.status === 'Failed'}>{entry.status}</span>
        </div>
        <p class="muted">{entry.done} of {entry.total} rows read, {entry.added} items added.</p>
        {#if entry.problems.length}
          <details>
            <summary>{entry.problems.length} {entry.problems.length === 1 ? 'row was' : 'rows were'} skipped</summary>
            <ul>{#each entry.problems as problem, index (index)}<li>{problem.row ? `Row ${problem.row}: ` : ''}{problem.problem}</li>{/each}</ul>
          </details>
        {/if}
        {#if canEdit() && entry.status !== 'Running'}
          <button type="button" class="small" onclick={pendingClick(() => remove(entry))}>Remove from the list</button>
        {/if}
      </li>
    {/each}
  </ul>
{/if}

{#if picking}
  <PlacePicker spaceId={app.spaceId} title="Where the imported things go" action={(here) => ({ text: `Put them in ${here?.name ?? 'the top'}`, disabled: false })}
    onpick={async (here) => { place = here; picking = false; }} onclose={() => { picking = false; }} />
{/if}

<style>
  h3 { margin: 1.25rem 0 .4rem; font-size: 1.05rem; }
  .upload { display: flex; flex-wrap: wrap; gap: .75rem; align-items: end; }
  .pair { display: grid; gap: .5rem; grid-template-columns: repeat(auto-fit, minmax(min(14rem, 100%), 1fr)); align-items: end; }
  .place { display: grid; gap: .2rem; font-size: .9rem; }
  .table-wrap { overflow-x: auto; }
  table { border-collapse: collapse; font-size: .85rem; }
  th, td { text-align: left; padding: .35rem .5rem; border-top: 1px solid var(--line); vertical-align: top; white-space: nowrap; max-width: 16rem; overflow: hidden; text-overflow: ellipsis; }
  td select { min-width: 9rem; }
  .list li { padding: .6rem 0; display: grid; gap: .25rem; }
  .list li + li { border-top: 1px solid var(--line); }
  .list p { margin: 0; }
</style>
