<!-- Moves an item: browse containers one level at a time, then "Move here". Everything inside the item goes with it.
     The item itself is left out, so it can't be moved into itself or anything inside it. -->
<script>
  import { untrack } from 'svelte';
  import { run, toast } from '../lib/app.svelte.js';
  import { api } from '../lib/api.js';
  import { pendingClick } from '../lib/pending.js';
  import { label } from '../lib/items.js';
  import Modal from '../components/Modal.svelte';
  import Box from '@lucide/svelte/icons/box';
  import ChevronRight from '@lucide/svelte/icons/chevron-right';

  /** spaceId; item: ItemResponse being moved; chain: its containers now (ItemRef[]); onmoved(item); onclose(). */
  let { spaceId, item, chain, onmoved, onclose } = $props();

  /** The containers opened so far, top first; empty at the top of the space. */
  let trail = $state(untrack(() => [...chain]));
  let containers = $state(null);
  let next = $state(null);

  const here = $derived(trail.at(-1) ?? null);

  $effect(() => {
    const id = here?.id ?? null;
    untrack(() => {
      containers = null;
      run(async () => {
        const page = await api('GET', `/api/spaces/${spaceId}/items${id ? `?parent=${id}` : ''}`);
        containers = page.items.filter((candidate) => candidate.holdsItems && candidate.id !== item.id);
        next = page.next;
      });
    });
  });

  const more = () => run(async () => {
    const page = await api('GET', `/api/spaces/${spaceId}/items?${here ? `parent=${here.id}&` : ''}after=${encodeURIComponent(next)}`);
    containers = [...containers, ...page.items.filter((candidate) => candidate.holdsItems && candidate.id !== item.id)];
    next = page.next;
  });

  const move = () => run(async () => {
    const moved = await api('POST', `/api/spaces/${spaceId}/items/${item.id}/move`, { parentId: here?.id ?? null });
    toast(`${item.name} moved to ${here?.name ?? 'the top'}.`);
    onmoved(moved);
  });

  const alreadyHere = $derived((here?.id ?? null) === (item.parentId ?? null));
</script>

<Modal title="Move {item.name}" {onclose}>
  <nav class="trail" aria-label="Where to">
    <button type="button" class="link" aria-current={trail.length === 0 ? 'location' : undefined} onclick={() => { trail = []; }}>Top</button>
    {#each trail as place, index (place.id)}
      <ChevronRight size={14} aria-hidden="true" />
      <button type="button" class="link" aria-current={index === trail.length - 1 ? 'location' : undefined}
        onclick={() => { trail = trail.slice(0, index + 1); }}>{place.name}</button>
    {/each}
  </nav>

  {#if containers === null}
    <span class="skeleton line"></span><span class="skeleton line"></span>
  {:else if containers.length === 0}
    <p class="muted">{here ? `Nothing inside ${here.name} holds items.` : 'No places at the top yet.'}</p>
  {:else}
    <ul class="plain" aria-label="Places inside {here?.name ?? 'the top'}">
      {#each containers as container (container.id)}
        <li><button type="button" class="open" onclick={() => { trail = [...trail, { id: container.id, name: container.name, marking: container.marking }]; }}>
          <Box size={16} aria-hidden="true" /><span class="clip">{label(container)}</span><ChevronRight size={16} aria-hidden="true" /></button></li>
      {/each}
    </ul>
    {#if next}<button type="button" class="small" onclick={pendingClick(more)}>Show more</button>{/if}
  {/if}

  <div class="actions foot">
    <button type="button" class="primary" disabled={alreadyHere} onclick={pendingClick(move)}>
      {alreadyHere ? `Already in ${here?.name ?? 'the top'}` : `Move to ${here?.name ?? 'the top'}`}</button>
    <button type="button" onclick={onclose}>Cancel</button>
  </div>
</Modal>

<style>
  .trail { display: flex; flex-wrap: wrap; align-items: center; gap: .2rem; margin-bottom: .6rem; }
  .link { border: 0; background: none; padding: .15rem .3rem; color: var(--accent); text-decoration: underline; }
  .link[aria-current] { color: var(--ink); text-decoration: none; font-weight: 600; }
  .open { display: flex; align-items: center; gap: .5rem; width: 100%; border: 0; background: none; text-align: left; padding: .45rem .25rem; color: var(--ink); }
  .open:hover { background: color-mix(in srgb, var(--area-browse) 8%, transparent); }
  .open .clip { flex: 1 1 auto; }
  .foot { margin-top: 1rem; }
</style>
