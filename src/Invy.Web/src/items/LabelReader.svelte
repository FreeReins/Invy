<!-- Read a label: a photo of a nameplate, box or receipt, from which the AI model reads the item's fields (brand,
     model, serial). The person ticks the values to keep; only "Fill in" saves them, as an edit of the item. -->
<script>
  import { run, toast } from '../lib/app.svelte.js';
  import { api } from '../lib/api.js';
  import { pendingClick } from '../lib/pending.js';
  import { photoRequest } from '../lib/photos.js';
  import { display } from '../lib/items.js';
  import Modal from '../components/Modal.svelte';
  import ScanText from '@lucide/svelte/icons/scan-text';

  /** spaceId; item: ItemResponse; type: its ItemTypeResponse; onsaved(item); onclose(). */
  let { spaceId, item, type, onsaved, onclose } = $props();

  let files = $state(null);
  /** { field, value, keep } for each value read, or null before the model answers. */
  let found = $state(null);
  let model = $state('');

  const kept = $derived(found?.filter((entry) => entry.keep) ?? []);

  const read = () => run(async () => {
    const answer = await api('POST', `/api/spaces/${spaceId}/items/${item.id}/details-from-photo`, await photoRequest(files));
    model = answer.model;
    found = type.fields.filter((field) => answer.fields[field.id] != null).map((field) => ({
      field, value: answer.fields[field.id], keep: item.fields[field.id] == null,
    }));
  });

  const fill = () => run(async () => {
    const fields = { ...item.fields };
    for (const entry of kept) fields[entry.field.id] = entry.value;
    const saved = await api('PUT', `/api/spaces/${spaceId}/items/${item.id}`, {
      name: item.name, marking: item.marking, typeId: item.typeId, parentId: null, holdsItems: item.holdsItems,
      quantity: item.quantity, unit: item.unit, fields, tags: item.tags, notes: item.notes, version: item.version,
    });
    toast(`${saved.name} saved.`);
    onsaved(saved);
  });
</script>

<Modal title="Read a label on {item.name}" {onclose}>
  {#if found === null}
    <div class="stack">
      <p class="muted">Photograph its label, nameplate, box or receipt. The AI model reads {type.name} fields such as {type.fields.slice(0, 3).map((field) => field.label).join(', ')}, and you choose what to keep.</p>
      <label>Photos <span class="muted">Up to 4</span>
        <input type="file" accept="image/*" capture="environment" multiple onchange={(e) => { files = e.currentTarget.files; }} />
      </label>
      <div class="actions">
        <button type="button" class="primary with-icon" disabled={!files?.length} onclick={pendingClick(read)}><ScanText size={16} aria-hidden="true" />Read it</button>
        <button type="button" onclick={onclose}>Cancel</button>
      </div>
    </div>
  {:else if found.length === 0}
    <p>{model} couldn't read any {type.name} fields in that photo. Try a sharper, closer photo of the label.</p>
    <div class="actions"><button type="button" onclick={() => { found = null; }}>Try another photo</button></div>
  {:else}
    <p class="muted">{model} read these. Values it would replace are unticked.</p>
    <ul class="plain read" aria-label="What the AI model read">
      {#each found as entry (entry.field.id)}
        <li>
          <label class="check"><input type="checkbox" bind:checked={entry.keep} />
            <span><strong>{entry.field.label}</strong>: {display(entry.field, entry.value)}
              {#if item.fields[entry.field.id] != null}<span class="muted">(now {display(entry.field, item.fields[entry.field.id])})</span>{/if}</span>
          </label>
        </li>
      {/each}
    </ul>
    <div class="actions">
      <button type="button" class="primary" disabled={kept.length === 0} onclick={pendingClick(fill)}>Fill in {kept.length === 1 ? '1 field' : `${kept.length} fields`}</button>
      <button type="button" onclick={() => { found = null; }}>Another photo</button>
    </div>
  {/if}
</Modal>

<style>
  .read { margin: .5rem 0 1rem; }
  .read span { overflow-wrap: anywhere; }
</style>
