<!-- Adds or changes one item type: its name, colour, whether it holds items, and its fields in order. Saving sends the
     whole type with the version it was opened at, so someone else's change in the meantime isn't overwritten. A field
     keeps its id when renamed, so items keep their values. readonly shows the same thing to viewers without inputs.
     Suggest fields asks the AI model for fields that suit the type's name; the ticked ones are added as rows to edit. -->
<script>
  import { untrack } from 'svelte';
  import { run, ask, toast, aiReady } from '../lib/app.svelte.js';
  import { api } from '../lib/api.js';
  import { pending, pendingClick } from '../lib/pending.js';
  import { KINDS, COLOURS, kindLabel, hasOptions, hasUnit, canSort, keyFor } from '../lib/fields.js';
  import Modal from '../components/Modal.svelte';
  import ArrowUp from '@lucide/svelte/icons/arrow-up';
  import ArrowDown from '@lucide/svelte/icons/arrow-down';
  import Trash from '@lucide/svelte/icons/trash';
  import Plus from '@lucide/svelte/icons/plus';
  import { has } from '../lib/features.js';
  import Sparkles from '@lucide/svelte/icons/sparkles';

  /** type: ItemTypeResponse, or null for a new one. onsaved(type), onremoved(id), onclose(). */
  let { spaceId, type, readonly = false, onsaved, onremoved, onclose } = $props();

  let nextRow = 0;
  const row = (field) => ({
    row: nextRow++, id: field?.id ?? null, label: field?.label ?? '', key: field?.key ?? '', kind: field?.kind ?? 'Text',
    unit: field?.unit ?? '', options: (field?.options ?? []).join('\n'), required: field?.required ?? false,
    sortable: field?.sortable ?? false, keyTouched: Boolean(field),
    remind: field?.remind ?? '', repeatMonths: field?.repeatMonths ?? '',
  });

  // The form starts from the type as it was opened and is edited from there.
  const opened = untrack(() => type);
  let name = $state(opened?.name ?? '');
  let colour = $state(opened?.colour ?? null);
  let holdsItems = $state(opened?.holdsItems ?? false);
  let fields = $state((opened?.fields ?? []).map(row));

  /** Fields the AI model suggested ({ field, keep }), or null when none were asked for. */
  let suggested = $state(null);

  const suggestFields = () => run(async () => {
    const answer = await api('POST', `/api/spaces/${spaceId}/types/suggest-fields`, {
      name: name.trim(), existingKeys: fields.map((field) => field.key.trim()).filter(Boolean),
    });
    suggested = answer.fields.map((field) => ({ field, keep: true }));
    if (suggested.length === 0) toast(`${answer.model} had no fields to suggest for ${name.trim()}.`);
  });

  function addSuggested() {
    for (const entry of suggested.filter((candidate) => candidate.keep)) fields.push({ ...row(entry.field), id: null });
    suggested = null;
  }

  const title = $derived(readonly ? type.name : type ? `Edit ${type.name}` : 'New type');

  function labelChanged(field) {
    if (!field.keyTouched) field.key = keyFor(field.label);
  }

  function move(index, by) {
    const [field] = fields.splice(index, 1);
    fields.splice(index + by, 0, field);
  }

  function request() {
    return {
      name: name.trim(), icon: type?.icon ?? null, colour, holdsItems, version: type?.version ?? 0,
      fields: fields.map((field) => ({
        id: field.id, key: field.key.trim(), label: field.label.trim(), kind: field.kind,
        unit: hasUnit(field.kind) && field.unit.trim() ? field.unit.trim() : null,
        options: hasOptions(field.kind) ? field.options.split('\n').map((option) => option.trim()).filter(Boolean) : [],
        required: field.required, sortable: canSort(field.kind) && field.sortable,
        remind: field.kind === 'Date' && field.remind !== '' && field.remind != null ? Number(field.remind) : null,
        repeatMonths: field.kind === 'Date' && field.remind !== '' && field.remind != null && field.repeatMonths !== '' && field.repeatMonths != null
          ? Number(field.repeatMonths) : null,
      })),
    };
  }

  function submit(event) {
    event.preventDefault();
    return pending(event, () => run(async () => {
      const saved = type
        ? await api('PUT', `/api/spaces/${spaceId}/types/${type.id}`, request())
        : await api('POST', `/api/spaces/${spaceId}/types`, request());
      toast(`${saved.name} saved.`);
      onsaved(saved);
    }));
  }

  const remove = () => run(async () => {
    const confirmed = await ask({
      title: `Remove ${type.name}?`,
      message: 'Items of this type keep their values, and you can give them another type later.',
      confirmLabel: 'Remove type', danger: true,
    });
    if (!confirmed) return;
    await api('DELETE', `/api/spaces/${spaceId}/types/${type.id}`);
    toast(`${type.name} removed.`);
    onremoved(type.id);
  });
</script>

<Modal {title} wide {onclose}>
  {#if readonly}
    <div class="stack">
      {#if type.holdsItems}<p class="muted">Other items can go inside a {type.name}.</p>{/if}
      {#if type.fields.length === 0}
        <p class="muted">{type.name} has no fields of its own.</p>
      {:else}
        <ul class="plain">
          {#each type.fields as field (field.id)}
            <li>
              <strong>{field.label}</strong> <span class="muted">· {kindLabel(field.kind)}{field.unit ? ` in ${field.unit}` : ''}</span>
              {#if field.options.length}<span class="muted options">{field.options.join(', ')}</span>{/if}
              <span class="muted options">Search with {field.key}:</span>
            </li>
          {/each}
        </ul>
      {/if}
      <p class="muted">Only editors and owners of this space can change types.</p>
    </div>
  {:else}
    <form class="stack" onsubmit={submit}>
      <label>Name <input bind:value={name} required maxlength="80" autocomplete="off" /></label>

      <fieldset class="colours">
        <legend>Colour</legend>
        <label class="colour"><input type="radio" name="colour" value={null} bind:group={colour} /><span class="dot none"></span>Default</label>
        {#each COLOURS as option (option.value)}
          <label class="colour" style="--dot: {option.value}">
            <input type="radio" name="colour" value={option.value} bind:group={colour} /><span class="dot"></span>{option.name}
          </label>
        {/each}
      </fieldset>

      <label class="check holds">
        <input type="checkbox" class="switch" bind:checked={holdsItems} />
        <span>Holds items <span class="muted">Places and containers, such as a room, shelf or box.</span></span>
      </label>

      <h3>Fields</h3>
      {#if fields.length === 0}
        <p class="muted">No fields yet. Every item already has a name, marking, notes, tags and photos.</p>
      {/if}
      <ol class="fields">
        {#each fields as field, index (field.row)}
          <li class="card field">
            <fieldset>
              <legend class="sr-only">Field {index + 1}{field.label ? `: ${field.label}` : ''}</legend>
              <div class="field-grid">
                <label>Field name <input bind:value={field.label} oninput={() => labelChanged(field)} required maxlength="80" autocomplete="off" /></label>
                <label>Search name <input bind:value={field.key} oninput={() => { field.keyTouched = true; }} required maxlength="40"
                  pattern="[a-z][a-z0-9_]*" autocomplete="off" spellcheck="false" /></label>
                <label>Kind
                  <select bind:value={field.kind}>
                    {#each KINDS as kind (kind.kind)}<option value={kind.kind}>{kind.label}</option>{/each}
                  </select>
                </label>
                {#if hasUnit(field.kind)}
                  <label>{field.kind === 'Money' ? 'Currency' : 'Unit'} <input bind:value={field.unit} maxlength="20" autocomplete="off"
                    placeholder={field.kind === 'Money' ? 'USD' : 'kg'} /></label>
                {/if}
              </div>
              {#if field.kind === 'Date' && (has('reminders') || field.remind !== '')}
                <div class="field-grid">
                  <label>Remind, days ahead <input type="number" bind:value={field.remind} min="0" max="365" step="1" placeholder="Don't remind" /></label>
                  <label>Repeats every, months <input type="number" bind:value={field.repeatMonths} min="1" max="120" step="1"
                    placeholder="Doesn't repeat" disabled={field.remind === '' || field.remind == null} /></label>
                </div>
              {/if}
              {#if hasOptions(field.kind)}
                <label>Choices, one per line <textarea bind:value={field.options} rows="3" required></textarea></label>
              {/if}
              <div class="row field-foot">
                <div class="actions">
                  <label class="check"><input type="checkbox" bind:checked={field.required} /> Ask for it</label>
                  {#if canSort(field.kind)}
                    <label class="check"><input type="checkbox" bind:checked={field.sortable} /> Sort and compare</label>
                  {/if}
                </div>
                <div class="row-actions">
                  <button type="button" class="icon" aria-label="Move {field.label || 'field'} up" disabled={index === 0}
                    onclick={() => move(index, -1)}><ArrowUp size={18} /></button>
                  <button type="button" class="icon" aria-label="Move {field.label || 'field'} down" disabled={index === fields.length - 1}
                    onclick={() => move(index, 1)}><ArrowDown size={18} /></button>
                  <button type="button" class="icon remove" aria-label="Remove {field.label || 'field'}"
                    onclick={() => fields.splice(index, 1)}><Trash size={18} /></button>
                </div>
              </div>
            </fieldset>
          </li>
        {/each}
      </ol>
      {#if suggested?.length}
        <div class="card suggested">
          <h3>Suggested fields</h3>
          <ul class="plain">
            {#each suggested as entry (entry.field.key)}
              <li><label class="check"><input type="checkbox" bind:checked={entry.keep} />
                <span><strong>{entry.field.label}</strong> <span class="muted">· {kindLabel(entry.field.kind)}{entry.field.unit ? ` in ${entry.field.unit}` : ''}{entry.field.options.length ? `: ${entry.field.options.join(', ')}` : ''}</span></span></label></li>
            {/each}
          </ul>
          <div class="actions">
            <button type="button" class="primary" disabled={!suggested.some((entry) => entry.keep)} onclick={addSuggested}>Add the ticked fields</button>
            <button type="button" onclick={() => { suggested = null; }}>Not these</button>
          </div>
        </div>
      {/if}
      <div class="actions">
        <button type="button" class="with-icon" onclick={() => fields.push(row(null))}><Plus size={16} aria-hidden="true" />Add a field</button>
        {#if aiReady('SuggestTypeFields')}
          <button type="button" class="with-icon" disabled={!name.trim()} onclick={pendingClick(suggestFields)}><Sparkles size={16} aria-hidden="true" />Suggest fields</button>
        {/if}
      </div>
      {#if type && fields.length < type.fields.length}
        <p class="muted">Items keep the values of removed fields; they show as Other fields.</p>
      {/if}

      <div class="row foot">
        <div class="actions">
          <button type="submit" class="primary">Save</button>
          <button type="button" onclick={onclose}>Cancel</button>
        </div>
        {#if type}<button type="button" class="danger" onclick={pendingClick(remove)}>Remove type</button>{/if}
      </div>
    </form>
  {/if}
</Modal>

<style>
  h3 { margin: .5rem 0 0; font-size: 1rem; }
  fieldset { border: 0; margin: 0; padding: 0; min-width: 0; }
  legend { font-size: .85rem; color: var(--muted); margin-bottom: .25rem; }
  .colours { display: flex; flex-wrap: wrap; gap: .25rem .75rem; }
  .colours legend { width: 100%; }
  .colour { display: inline-flex; align-items: center; gap: .3rem; color: var(--ink); font-size: .9rem; cursor: pointer; }
  .dot { width: .9rem; height: .9rem; border-radius: 50%; background: var(--dot); }
  .dot.none { background: var(--area-types); }
  .holds, .check { color: var(--ink); font-size: .9rem; border: 0; margin: 0; cursor: pointer; }
  .holds > span { display: grid; gap: .1rem; }
  .fields { list-style: none; margin: 0; padding: 0; display: grid; gap: .5rem; }
  .field { margin: 0; }
  .field-grid { display: grid; gap: .5rem; grid-template-columns: repeat(auto-fit, minmax(min(10rem, 100%), 1fr)); margin-bottom: .5rem; }
  .field-foot { align-items: center; margin-top: .25rem; }
  .field-foot .check { padding: .2rem 0; margin: 0; align-items: center; }
  .field-foot .check input { margin-top: 0; }
  .options { display: block; }
  .foot { align-items: center; margin-top: .5rem; }
  .suggested { margin: 0; }
  .suggested h3 { margin: 0 0 .25rem; }
</style>
