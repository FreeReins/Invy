<!-- One item's page: where it is (the chain of containers), its details and fields, what's inside it, and its
     timeline with where it has been. Editors add, change, move, count and dispose from here. -->
<script>
  import { onMount } from 'svelte';
  import { run, ask, toast, canEdit } from '../lib/app.svelte.js';
  import { api } from '../lib/api.js';
  import { pendingClick } from '../lib/pending.js';
  import { amount, display, label, chainText } from '../lib/items.js';
  import { fullDate } from '../lib/format.js';
  import ItemList from './ItemList.svelte';
  import ItemForm from './ItemForm.svelte';
  import MoveDialog from './MoveDialog.svelte';
  import QuickAdd from './QuickAdd.svelte';
  import EventLine from './EventLine.svelte';
  import ChevronRight from '@lucide/svelte/icons/chevron-right';
  import Plus from '@lucide/svelte/icons/plus';
  import Minus from '@lucide/svelte/icons/minus';
  import Move from '@lucide/svelte/icons/move';
  import SquarePen from '@lucide/svelte/icons/square-pen';

  /** spaceId; id: the item's id. */
  let { spaceId, id } = $props();

  let detail = $state(null);
  let types = $state([]);
  let contents = $state(null);
  let history = $state(null);
  /** 'edit', 'add', 'move' or null. */
  let dialog = $state(null);

  const item = $derived(detail?.item);
  const type = $derived(types.find((candidate) => candidate.id === item?.typeId) ?? null);
  const shown = $derived(type?.fields.filter((field) => item?.fields[field.id] != null) ?? []);
  const others = $derived(Object.keys(item?.fields ?? {}).filter((fieldId) => !type?.fields.some((field) => field.id === fieldId)));
  const gone = $derived(item && (item.status === 'Disposed' || item.status === 'Archived'));

  async function load() {
    const base = `/api/spaces/${spaceId}`;
    const [found, allTypes, past] = await Promise.all([
      api('GET', `${base}/items/${id}`), api('GET', `${base}/types`), api('GET', `${base}/items/${id}/history`),
    ]);
    detail = found;
    types = allTypes;
    history = past;
    contents = found.item.holdsItems ? await api('GET', `${base}/items?parent=${id}`) : null;
  }

  const reload = () => run(load);
  onMount(() => { reload(); });

  const more = () => run(async () => {
    const page = await api('GET', `/api/spaces/${spaceId}/items?parent=${id}&after=${encodeURIComponent(contents.next)}`);
    contents = { items: [...contents.items, ...page.items], next: page.next };
  });

  const count = (delta) => run(async () => {
    await api('POST', `/api/spaces/${spaceId}/items/${id}/quantity`, { delta });
    await load();
  });

  const dispose = () => run(async () => {
    const confirmed = await ask({
      title: `Dispose of ${item.name}?`,
      message: 'It leaves its place and search, but its history is kept and you can restore it.',
      confirmLabel: 'Dispose', danger: true,
    });
    if (!confirmed) return;
    await api('POST', `/api/spaces/${spaceId}/items/${id}/dispose`);
    toast(`${item.name} disposed of.`);
    await load();
  });

  const restore = () => run(async () => {
    await api('POST', `/api/spaces/${spaceId}/items/${id}/restore`);
    toast(`${item.name} restored.`);
    await load();
  });

  function done() {
    dialog = null;
    reload();
  }
</script>

{#if !detail}
  <div class="card"><span class="skeleton line"></span><span class="skeleton line"></span><span class="skeleton line"></span></div>
{:else}
  <nav class="chain" aria-label="Where it is">
    <a href="/browse">Top</a>
    {#each detail.chain as place (place.id)}
      <ChevronRight size={14} aria-hidden="true" /><a href="/browse/{place.id}">{label(place)}</a>
    {/each}
    <ChevronRight size={14} aria-hidden="true" /><span aria-current="page">{label(item)}</span>
  </nav>

  <section class="card head">
    <div class="row">
      <div class="title">
        <h2>{item.name}</h2>
        <div class="chips">
          {#if item.marking}<span class="chip">Marked {item.marking}</span>{/if}
          {#if type}<span class="chip info">{type.name}</span>{/if}
          {#if item.status !== 'Active'}<span class="chip warn">{item.status}</span>{/if}
          {#each item.tags as tag (tag)}<span class="chip">#{tag}</span>{/each}
        </div>
      </div>
      {#if canEdit()}
        <div class="actions">
          {#if gone}
            <button type="button" onclick={pendingClick(restore)}>Restore</button>
          {:else}
            <button type="button" class="with-icon" onclick={() => { dialog = 'edit'; }}><SquarePen size={16} aria-hidden="true" />Edit</button>
            <button type="button" class="with-icon" onclick={() => { dialog = 'move'; }}><Move size={16} aria-hidden="true" />Move</button>
            <button type="button" onclick={pendingClick(dispose)}>Dispose</button>
          {/if}
        </div>
      {/if}
    </div>

    {#if item.quantity != null}
      <div class="amount">
        <span class="muted">Amount</span>
        {#if canEdit() && !gone}
          <button type="button" class="icon" aria-label="One fewer" disabled={item.quantity < 1} onclick={pendingClick(() => count(-1))}><Minus size={18} /></button>
        {/if}
        <strong aria-live="polite">{amount(item)}</strong>
        {#if canEdit() && !gone}
          <button type="button" class="icon" aria-label="One more" onclick={pendingClick(() => count(1))}><Plus size={18} /></button>
        {/if}
      </div>
    {/if}

    {#if shown.length || others.length}
      <dl class="fields">
        {#each shown as field (field.id)}
          <div><dt>{field.label}</dt><dd>{#if field.kind === 'Url'}<a href={item.fields[field.id]} target="_blank" rel="noopener noreferrer">{item.fields[field.id]}</a>{:else if field.kind === 'Item'}<a href="/browse/{item.fields[field.id]}">Open the item</a>{:else}{display(field, item.fields[field.id])}{/if}</dd></div>
        {/each}
        {#each others as fieldId (fieldId)}
          <div><dt>Other field</dt><dd>{display(null, item.fields[fieldId])}</dd></div>
        {/each}
      </dl>
    {/if}
    {#if item.notes}<p class="notes">{item.notes}</p>{/if}
  </section>

  {#if item.holdsItems}
    <section class="card">
      <div class="row">
        <h3>Inside {item.name}</h3>
        {#if canEdit() && !gone}
          <button type="button" class="small with-icon" onclick={() => { dialog = 'add'; }}><Plus size={14} aria-hidden="true" />Add an item</button>
        {/if}
      </div>
      {#if canEdit() && !gone}<QuickAdd {spaceId} parent={item} onadded={reload} />{/if}
      {#if contents?.items.length}
        <ItemList items={contents.items} name="Inside {item.name}" onmore={contents.next ? more : null} />
      {:else}
        <p class="muted">Nothing inside yet.</p>
      {/if}
    </section>
  {/if}

  <section class="card">
    <h3>Where it has been</h3>
    {#if history.places.length}
      <ol class="places">
        {#each history.places as stay, index (index)}
          <li>
            <span>{chainText(stay.chain)}</span>
            <span class="muted">{stay.until ? `${fullDate(null, stay.from)} until ${fullDate(null, stay.until)}` : `since ${fullDate(null, stay.from)}`}</span>
          </li>
        {/each}
      </ol>
    {:else}
      <p class="muted">No moves recorded.</p>
    {/if}
    <h3>Timeline</h3>
    <ul class="plain timeline">
      {#each history.events as event (event.id)}<li><EventLine {event} current={item.id} /></li>{/each}
    </ul>
  </section>

  {#if dialog === 'edit'}
    <ItemForm {spaceId} {types} {item} onsaved={done} onclose={() => { dialog = null; }} />
  {:else if dialog === 'add'}
    <ItemForm {spaceId} {types} parent={item} onsaved={done} onclose={() => { dialog = null; }} />
  {:else if dialog === 'move'}
    <MoveDialog {spaceId} {item} chain={detail.chain} onmoved={done} onclose={() => { dialog = null; }} />
  {/if}
{/if}

<style>
  .chain { display: flex; flex-wrap: wrap; align-items: center; gap: .25rem; margin: 1.25rem 0 .25rem; font-size: .9rem; }
  .chain [aria-current] { font-weight: 600; }
  .head h2 { margin: 0 0 .2rem; overflow-wrap: anywhere; }
  .title { min-width: 0; }
  .row { align-items: center; }
  h3 { margin: .25rem 0 .5rem; font-size: 1.05rem; }
  .amount { display: flex; align-items: center; gap: .5rem; margin-top: .6rem; }
  .fields { display: grid; gap: .4rem 1.5rem; grid-template-columns: repeat(auto-fit, minmax(min(12rem, 100%), 1fr)); margin: .75rem 0 0; }
  .fields dt { font-size: .8rem; color: var(--muted); }
  .fields dd { margin: 0; overflow-wrap: anywhere; }
  .notes { white-space: pre-wrap; margin: .75rem 0 0; }
  .places { margin: 0 0 1rem; padding-left: 1.25rem; display: grid; gap: .3rem; }
  .places li span { display: block; }
  .timeline li { padding: .45rem 0; }
</style>
