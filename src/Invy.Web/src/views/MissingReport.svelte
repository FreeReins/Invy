<!-- The audit discrepancy report (/views/missing): the things marked missing, by an audit or by hand, with where they
     belong and when they were last seen. It prints, and the CSV comes from the server. Shown behind the audits
     feature. It loads again when the items live topic says something changed, so a thing found drops off. -->
<script>
  import { untrack } from 'svelte';
  import { app, run, currentSpace, onLive } from '../lib/app.svelte.js';
  import { api } from '../lib/api.js';
  import { label, chainText } from '../lib/items.js';
  import { plural, fullDate } from '../lib/format.js';
  import { pendingClick } from '../lib/pending.js';
  import Printer from '@lucide/svelte/icons/printer';
  import Download from '@lucide/svelte/icons/download';
  import ArrowLeft from '@lucide/svelte/icons/arrow-left';
  import SearchCheck from '@lucide/svelte/icons/search-check';

  let page = $state(null);
  const space = $derived(currentSpace());
  const base = $derived(`/api/spaces/${app.spaceId}/reports/missing`);

  const load = () => run(async () => { page = await api('GET', base); });
  const more = () => run(async () => {
    const next = await api('GET', `${base}?after=${encodeURIComponent(page.next)}`);
    page = { ...page, results: [...page.results, ...next.results], next: next.next };
  });

  $effect(() => {
    void app.spaceId;
    untrack(() => { page = null; load(); });
  });
  $effect(() => onLive('items', () => load()));

  const seen = (result) => (result.details?.lastSeenAt ? fullDate(null, result.details.lastSeenAt) : 'Never');
</script>

<section class="report">
  <a class="button back no-print" href="/views"><ArrowLeft size={16} aria-hidden="true" />All views</a>
  <div class="row head">
    <div>
      <h2>Missing items: {space?.name}</h2>
      <p class="muted">Things an audit didn't find, or that were marked missing. Ticking one in a later audit, or marking it found, takes it off.</p>
    </div>
    <div class="actions no-print">
      <button type="button" class="primary with-icon" onclick={() => window.print()}><Printer size={16} aria-hidden="true" />Print or save as PDF</button>
      <a class="button with-icon" href="{base}.csv" download><Download size={16} aria-hidden="true" />Download CSV</a>
    </div>
  </div>

  {#if page === null}
    <div class="card"><span class="skeleton line"></span><span class="skeleton line"></span></div>
  {:else if page.results.length === 0}
    <div class="card empty">
      <span class="empty-icon" style="--area: var(--area-views)" aria-hidden="true"><SearchCheck size={26} /></span>
      <h3>Nothing is missing</h3>
      <p class="muted">Every audit found what Invy expected. Audit a box from its page on Browse.</p>
    </div>
  {:else}
    <p class="muted count" aria-live="polite">{plural(page.total ?? page.results.length, 'missing item')}</p>
    <div class="card table-wrap">
      <table>
        <thead><tr><th scope="col">Thing</th><th scope="col">Belongs in</th><th scope="col">Last seen</th></tr></thead>
        <tbody>
          {#each page.results as result (result.item.id)}
            <tr>
              <td><a href="/browse/{result.item.id}">{label(result.item)}</a></td>
              <td>{chainText(result.chain)}</td>
              <td>{seen(result)}</td>
            </tr>
          {/each}
        </tbody>
      </table>
    </div>
    {#if page.next}<button type="button" onclick={pendingClick(more)}>Show more</button>{/if}
  {/if}
</section>

<style>
  .report { margin-top: 1.25rem; }
  .head { align-items: flex-start; }
  .head h2 { margin: .5rem 0 .2rem; overflow-wrap: anywhere; }
  .empty h3 { margin: .25rem 0; }
  .count { margin: .25rem 0; }
  .table-wrap { overflow-x: auto; padding: 0; }
  table { width: 100%; border-collapse: collapse; font-size: .92rem; }
  th, td { text-align: left; padding: .45rem .7rem; border-top: 1px solid var(--line); vertical-align: top; }
  thead th { border-top: 0; }
  @media print { tr { break-inside: avoid; } .table-wrap { overflow: visible; } }
</style>
