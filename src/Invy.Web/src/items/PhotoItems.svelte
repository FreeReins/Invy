<!-- Add things from a photo: take or choose photos of an open box, the AI model lists what it sees (matched to the
     space's types and fields), and the person ticks, edits and keeps them. Nothing is added until "Add". -->
<script>
  import { run, toast } from '../lib/app.svelte.js';
  import { api } from '../lib/api.js';
  import { pendingClick } from '../lib/pending.js';
  import { photoRequest } from '../lib/photos.js';
  import { plural } from '../lib/format.js';
  import { display } from '../lib/items.js';
  import Modal from '../components/Modal.svelte';
  import Camera from '@lucide/svelte/icons/camera';

  /** spaceId; parent: the container they go in (ItemRef) or null for the Inbox; types: ItemTypeResponse[]; onadded(items); onclose(). */
  let { spaceId, parent = null, types, onadded, onclose } = $props();

  let files = $state(null);
  /** The rows to review: { key, keep, name, typeId, quantity, unit, fields }, or null before the model answers. */
  let rows = $state(null);
  let model = $state('');
  let key = 0;

  const kept = $derived(rows?.filter((row) => row.keep && row.name.trim()) ?? []);
  const typeOf = (typeId) => types.find((type) => type.id === typeId) ?? null;
  const where = $derived(parent ? parent.name : 'the Inbox');

  const look = () => run(async () => {
    const answer = await api('POST', `/api/spaces/${spaceId}/items/from-photo`, await photoRequest(files));
    model = answer.model;
    rows = answer.items.map((item) => ({
      key: key++, keep: true, name: item.name, typeId: item.typeId ?? '', quantity: item.quantity ?? '', unit: item.unit ?? '', fields: item.fields,
    }));
  });

  /** Field values only for the type still chosen: changing a row's type drops values that were for the old one. */
  function fieldsFor(row) {
    const type = typeOf(row.typeId);
    return Object.fromEntries(Object.entries(row.fields).filter(([id]) => type?.fields.some((field) => field.id === id)));
  }

  const add = () => run(async () => {
    const made = [];
    for (const row of kept) {
      const counted = row.quantity !== '' && row.quantity != null;
      made.push(await api('POST', `/api/spaces/${spaceId}/items`, {
        name: row.name.trim(), marking: null, typeId: row.typeId || null, parentId: parent?.id ?? null, holdsItems: null,
        quantity: counted ? Number(row.quantity) : null, unit: counted && row.unit ? row.unit : null,
        fields: fieldsFor(row), tags: [], notes: null, version: 0,
      }));
    }
    toast(`${plural(made.length, 'item')} added to ${where}.`);
    onadded(made);
  });

  function values(row) {
    const type = typeOf(row.typeId);
    return (type?.fields ?? []).filter((field) => row.fields[field.id] != null).map((field) => `${field.label}: ${display(field, row.fields[field.id])}`).join(' · ');
  }
</script>

<Modal title="Add from a photo" wide {onclose}>
  {#if rows === null}
    <div class="stack">
      <p class="muted">Photograph an open box, drawer or shelf. The AI model lists what it sees, and you choose what to add to {where}.</p>
      <label>Photos <span class="muted">Up to 4, of the same box</span>
        <input type="file" accept="image/*" capture="environment" multiple onchange={(e) => { files = e.currentTarget.files; }} />
      </label>
      <div class="actions">
        <button type="button" class="primary with-icon" disabled={!files?.length} onclick={pendingClick(look)}><Camera size={16} aria-hidden="true" />Look at the photo</button>
        <button type="button" onclick={onclose}>Cancel</button>
      </div>
    </div>
  {:else if rows.length === 0}
    <p>{model} didn't see anything it could name. Try a closer, brighter photo with the things spread out.</p>
    <div class="actions"><button type="button" onclick={() => { rows = null; }}>Try another photo</button></div>
  {:else}
    <p class="muted">{model} saw {plural(rows.length, 'thing')}. Untick what's wrong, fix names and types, then add the rest to {where}.</p>
    <ul class="plain review" aria-label="What the AI model saw">
      {#each rows as row, index (row.key)}
        <li class:off={!row.keep}>
          <input type="checkbox" bind:checked={row.keep} aria-label="Keep {row.name || `thing ${index + 1}`}" />
          <div class="fields">
            <label class="name">Name <input bind:value={row.name} maxlength="200" autocomplete="off" /></label>
            <label>Type
              <select bind:value={row.typeId}>
                <option value="">No type</option>
                {#each types as type (type.id)}<option value={type.id}>{type.name}</option>{/each}
              </select>
            </label>
            <label class="amount">Amount <input type="number" bind:value={row.quantity} min="0" step="any" placeholder="1" /></label>
            {#if values(row)}<p class="muted values">{values(row)}</p>{/if}
          </div>
        </li>
      {/each}
    </ul>
    <div class="actions">
      <button type="button" class="primary" disabled={kept.length === 0} onclick={pendingClick(add)}>Add {plural(kept.length, 'item')}</button>
      <button type="button" onclick={() => { rows = null; }}>Another photo</button>
    </div>
  {/if}
</Modal>

<style>
  .review { display: grid; gap: .5rem; margin: .5rem 0 1rem; }
  .review li { display: flex; gap: .6rem; align-items: flex-start; padding: .5rem; border: 1px solid var(--line); border-radius: .6rem; }
  .review li.off { opacity: .6; }
  .review li > input { margin-top: 1.6rem; flex: none; }
  .fields { display: flex; flex-wrap: wrap; gap: .5rem; flex: 1 1 auto; min-width: 0; }
  .fields label { display: grid; gap: .2rem; font-size: .85rem; }
  .name { flex: 1 1 12rem; }
  .amount input { width: 6rem; }
  .values { flex: 1 1 100%; margin: 0; font-size: .85rem; overflow-wrap: anywhere; }
</style>
