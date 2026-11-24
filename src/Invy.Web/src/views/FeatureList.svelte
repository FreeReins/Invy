<!-- A built-in list on the Views tab for a space feature (/views/low-stock, /views/loans, /views/reminders,
     /views/moving): a heading, what it shows, the rows, and an empty state. It prints. It loads again when the items
     live topic says something changed. -->
<script>
  import { untrack } from 'svelte';
  import { app, run, onLive } from '../lib/app.svelte.js';
  import { api } from '../lib/api.js';
  import Printer from '@lucide/svelte/icons/printer';
  import ArrowLeft from '@lucide/svelte/icons/arrow-left';

  /**
   * title; about: one line on what it lists; path: the API path under the space; rows(answer): the rows to show;
   * empty: { title, text } for no rows; row: a snippet for one row; icon: the empty state's icon component.
   */
  let { title, about, path, rows, empty, row, icon: Icon } = $props();

  let answer = $state(null);
  const shown = $derived(answer ? rows(answer) : []);

  const load = () => run(async () => { answer = await api('GET', `/api/spaces/${app.spaceId}/${path}`); });
  $effect(() => {
    void app.spaceId;
    untrack(() => { answer = null; load(); });
  });
  $effect(() => onLive('items', () => load()));
</script>

<section class="report">
  <a class="button back no-print" href="/views"><ArrowLeft size={16} aria-hidden="true" />All views</a>
  <div class="row head">
    <div>
      <h2>{title}</h2>
      <p class="muted">{about}</p>
    </div>
    <div class="actions no-print">
      <button type="button" class="with-icon" onclick={() => window.print()}><Printer size={16} aria-hidden="true" />Print or save as PDF</button>
    </div>
  </div>

  {#if answer === null}
    <div class="card"><span class="skeleton line"></span><span class="skeleton line"></span></div>
  {:else if shown.length === 0}
    <div class="card empty">
      <span class="empty-icon" style="--area: var(--area-views)" aria-hidden="true"><Icon size={26} /></span>
      <h3>{empty.title}</h3>
      <p class="muted">{empty.text}</p>
    </div>
  {:else}
    <div class="card">
      <ul class="plain rows" aria-label={title}>
        {#each shown as entry, index (index)}<li>{@render row(entry, load)}</li>{/each}
      </ul>
    </div>
    {#if answer.more}<p class="muted">Only the first {shown.length} are listed. Search to see them all.</p>{/if}
  {/if}
</section>

<style>
  .report { margin-top: 1.25rem; }
  .head { align-items: flex-start; }
  .head h2 { margin: .5rem 0 .2rem; overflow-wrap: anywhere; }
  .empty h3 { margin: .25rem 0; }
  .rows li { padding: .5rem 0; display: flex; flex-wrap: wrap; justify-content: space-between; align-items: center; gap: .3rem .75rem; }
  .rows li + li { border-top: 1px solid var(--line); }
  @media print { .rows li { break-inside: avoid; } }
</style>
