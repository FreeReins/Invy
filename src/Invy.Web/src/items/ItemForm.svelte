<!-- Adds or changes an item: its name, marking, type, whether it holds items or is counted, its fields, tags and notes.
     Fields follow the chosen type; values of fields the type doesn't have are kept and shown as Other fields. A product
     looked up from its barcode fills in a new item's name, and its brand and barcode fields when the type has them. -->
<script>
  import { untrack } from 'svelte';
  import { run, toast } from '../lib/app.svelte.js';
  import { api } from '../lib/api.js';
  import { pending } from '../lib/pending.js';
  import { hasOptions } from '../lib/fields.js';
  import { display } from '../lib/items.js';
  import { has } from '../lib/features.js';
  import Modal from '../components/Modal.svelte';

  /**
   * spaceId; types: ItemTypeResponse[]; item: ItemResponse to change, or null to add; parent: where a new one goes;
   * product: a ProductResponse to fill a new one from; onsaved(item); onclose().
   */
  let { spaceId, types, item = null, parent = null, product = null, onsaved, onclose } = $props();

  const opened = untrack(() => item);
  const found = untrack(() => product);
  const productName = found ? (found.brand && !found.name.toLowerCase().includes(found.brand.toLowerCase()) ? `${found.brand} ${found.name}` : found.name) : '';
  let name = $state(opened?.name ?? productName);
  let marking = $state(opened?.marking ?? '');
  let typeId = $state(opened?.typeId ?? '');
  let holdsItems = $state(opened?.holdsItems ?? null);
  let counted = $state(opened?.quantity != null);
  let quantity = $state(opened?.quantity ?? 1);
  let unit = $state(opened?.unit ?? '');
  let lowStockAt = $state(opened?.lowStockAt ?? '');
  let values = $state({ ...(opened?.fields ?? {}) });
  let tags = $state((opened?.tags ?? []).join(', '));
  let notes = $state(opened?.notes ?? '');

  const type = $derived(types.find((candidate) => candidate.id === typeId) ?? null);
  const holds = $derived(holdsItems ?? type?.holdsItems ?? false);
  const own = $derived(new Set(type?.fields.map((field) => field.id) ?? []));
  // A looked-up product's brand and barcode go into the chosen type's fields for them, when those are still empty.
  $effect(() => {
    const fields = type?.fields ?? [];
    untrack(() => {
      if (!found) return;
      for (const field of fields) {
        if (values[field.id] != null && values[field.id] !== '') continue;
        if (field.kind === 'Barcode') values[field.id] = found.barcode;
        else if (field.key === 'brand' && field.kind === 'Text' && found.brand) values[field.id] = found.brand;
      }
    });
  });

  const others = $derived(Object.keys(values).filter((id) => !own.has(id) && values[id] != null));

  function toggleChoice(field, option, checked) {
    const chosen = new Set(values[field.id] ?? []);
    if (checked) chosen.add(option); else chosen.delete(option);
    values[field.id] = field.options.filter((choice) => chosen.has(choice));
    if (values[field.id].length === 0) values[field.id] = null;
  }

  /** The field values to send: empty ones left out, numbers as numbers. */
  function fields() {
    const sent = {};
    for (const [id, value] of Object.entries(values)) {
      if (value == null || value === '' || (Array.isArray(value) && value.length === 0)) continue;
      const field = type?.fields.find((candidate) => candidate.id === id);
      sent[id] = field && (field.kind === 'Number' || field.kind === 'Money') ? Number(value) : value;
    }
    return sent;
  }

  function submit(event) {
    event.preventDefault();
    const request = {
      name, marking: marking || null, typeId: typeId || null, parentId: parent?.id ?? null, holdsItems: holds,
      quantity: counted ? Number(quantity) : null, unit: counted && unit ? unit : null, fields: fields(),
      lowStockAt: counted && lowStockAt !== '' && lowStockAt != null ? Number(lowStockAt) : null,
      tags: tags.split(',').map((tag) => tag.trim()).filter(Boolean), notes: notes || null, version: opened?.version ?? 0,
    };
    return pending(event, () => run(async () => {
      const saved = opened
        ? await api('PUT', `/api/spaces/${spaceId}/items/${opened.id}`, request)
        : await api('POST', `/api/spaces/${spaceId}/items`, request);
      toast(opened ? `${saved.name} saved.` : `${saved.name} added${parent ? ` to ${parent.name}` : ''}.`);
      onsaved(saved);
    }));
  }
</script>

<Modal title={opened ? `Edit ${opened.name}` : parent ? `Add to ${parent.name}` : 'New item'} wide {onclose}>
  <form class="stack" onsubmit={submit}>
    <div class="pair">
      <label>Name <input bind:value={name} required maxlength="200" autocomplete="off" /></label>
      <label>Marking <input bind:value={marking} maxlength="40" autocomplete="off" placeholder="What's written on it" /></label>
    </div>
    <div class="pair">
      <label>Type
        <select bind:value={typeId}>
          <option value="">No type</option>
          {#each types as option (option.id)}<option value={option.id}>{option.name}</option>{/each}
        </select>
      </label>
      <label class="check"><input type="checkbox" class="switch" checked={holds} onchange={(e) => { holdsItems = e.currentTarget.checked; }} />
        <span>Holds items <span class="muted">Other things can go inside it</span></span></label>
    </div>

    <label class="check"><input type="checkbox" bind:checked={counted} />
      <span>Counted <span class="muted">Many of the same, such as 200 screws or 3.5 kg of filament</span></span></label>
    {#if counted}
      <div class="pair">
        <label>Amount <input type="number" bind:value={quantity} min="0" step="any" required /></label>
        <label>Unit <input bind:value={unit} maxlength="20" placeholder="kg" autocomplete="off" /></label>
      </div>
      {#if has('stock') || (lowStockAt !== '' && lowStockAt != null)}
        <label>Low stock at <span class="muted">Tell me when it gets down to this. Empty to stop watching it.</span>
          <input type="number" bind:value={lowStockAt} min="0" step="any" /></label>
      {/if}
    {/if}

    {#if type?.fields.length}
      <fieldset class="fields">
        <legend>{type.name} fields</legend>
        {#each type.fields as field (field.id)}
          {#if field.kind === 'LongText'}
            <label>{field.label} <textarea bind:value={values[field.id]} rows="3" maxlength="10000"></textarea></label>
          {:else if field.kind === 'Number' || field.kind === 'Money'}
            <label>{field.label}{field.unit ? ` (${field.unit})` : ''} <input type="number" step="any" bind:value={values[field.id]} /></label>
          {:else if field.kind === 'Date'}
            <label>{field.label} <input type="date" bind:value={values[field.id]} /></label>
          {:else if field.kind === 'Bool'}
            <label>{field.label}
              <select value={values[field.id] == null ? '' : String(values[field.id])}
                onchange={(e) => { const v = e.currentTarget.value; values[field.id] = v === '' ? null : v === 'true'; }}>
                <option value="">Not set</option><option value="true">Yes</option><option value="false">No</option>
              </select>
            </label>
          {:else if field.kind === 'Select'}
            <label>{field.label}
              <select bind:value={values[field.id]}>
                <option value={null}>Not set</option>
                {#each field.options as option (option)}<option value={option}>{option}</option>{/each}
              </select>
            </label>
          {:else if hasOptions(field.kind)}
            <fieldset class="choices">
              <legend>{field.label}</legend>
              {#each field.options as option (option)}
                <label class="choice"><input type="checkbox" checked={(values[field.id] ?? []).includes(option)}
                  onchange={(e) => toggleChoice(field, option, e.currentTarget.checked)} />{option}</label>
              {/each}
            </fieldset>
          {:else if field.kind === 'Item'}
            <p class="muted">{field.label}: {values[field.id] ? 'set' : 'not set'}. Picking another item comes with search.</p>
          {:else}
            <label>{field.label} <input type={field.kind === 'Url' ? 'url' : 'text'} bind:value={values[field.id]} maxlength="2000" autocomplete="off" /></label>
          {/if}
        {/each}
      </fieldset>
    {/if}

    {#if others.length}
      <div class="others">
        <h3>Other fields</h3>
        <p class="muted">Kept from another type or a removed field. Clear one to drop it.</p>
        <ul class="plain">
          {#each others as id (id)}
            <li class="row"><span>{display(null, values[id])}</span>
              <button type="button" class="small" onclick={() => { values[id] = null; }}>Clear</button></li>
          {/each}
        </ul>
      </div>
    {/if}

    <label>Tags <input bind:value={tags} placeholder="garden, summer" autocomplete="off" /></label>
    <label>Notes <textarea bind:value={notes} rows="3" maxlength="10000"></textarea></label>

    <div class="actions">
      <button type="submit" class="primary">{opened ? 'Save' : 'Add'}</button>
      <button type="button" onclick={onclose}>Cancel</button>
    </div>
  </form>
</Modal>

<style>
  .pair { display: grid; gap: .5rem; grid-template-columns: repeat(auto-fit, minmax(min(14rem, 100%), 1fr)); align-items: end; }
  .check { display: flex; gap: .6rem; align-items: center; color: var(--ink); font-size: .9rem; border: 0; margin: 0; padding: .3rem 0; cursor: pointer; }
  .check > span { display: grid; }
  fieldset { border: 0; margin: 0; padding: 0; min-width: 0; display: grid; gap: .5rem; }
  legend { font-size: .85rem; color: var(--muted); margin-bottom: .25rem; }
  .fields { border-top: 1px solid var(--line); padding-top: .5rem; }
  .choices { display: flex; flex-wrap: wrap; gap: .25rem .9rem; }
  .choices legend { width: 100%; }
  .choice { display: inline-flex; gap: .35rem; align-items: center; color: var(--ink); font-size: .9rem; }
  h3 { margin: .25rem 0; font-size: 1rem; }
</style>
