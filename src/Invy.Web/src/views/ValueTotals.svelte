<!-- What a space's things are worth: the total, by place at the top (from the containers' rollups) and by type.
     Shown on Views behind the values feature. Currencies aren't converted, so a total is a plain sum. -->
<script>
  import { untrack } from 'svelte';
  import { app, run, onLive } from '../lib/app.svelte.js';
  import { api } from '../lib/api.js';
  import { label } from '../lib/items.js';
  import { plural } from '../lib/format.js';
  import { money } from '../lib/views.js';

  let totals = $state(null);

  const load = () => run(async () => { totals = await api('GET', `/api/spaces/${app.spaceId}/reports/values`); });

  $effect(() => {
    void app.spaceId;
    untrack(() => { totals = null; load(); });
  });
  $effect(() => onLive('items', () => load()));
</script>

<section class="card totals" aria-labelledby="value-totals">
  <h3 id="value-totals">What it is worth</h3>
  {#if totals === null}
    <span class="skeleton line"></span><span class="skeleton line"></span>
  {:else}
    <p class="total"><strong>{money(totals.total)}</strong>
      <span class="muted">{plural(totals.items, 'item')}{totals.unvalued ? `, ${totals.unvalued} without a value` : ''}. Currencies are added as they are.</span></p>
    <div class="columns">
      <table>
        <caption>By place</caption>
        <thead><tr><th scope="col">Place</th><th scope="col" class="num">Items</th><th scope="col" class="num">Value</th></tr></thead>
        <tbody>
          {#each totals.byPlace as place (place.place?.id ?? 'top')}
            <tr>
              <td>{#if place.place}<a href="/browse/{place.place.id}">{label(place.place)}</a>{:else}Not in a place{/if}</td>
              <td class="num">{place.count}</td><td class="num">{money(place.value)}</td>
            </tr>
          {/each}
        </tbody>
      </table>
      <table>
        <caption>By type</caption>
        <thead><tr><th scope="col">Type</th><th scope="col" class="num">Items</th><th scope="col" class="num">Value</th></tr></thead>
        <tbody>
          {#each totals.byType as type (type.typeId ?? 'none')}
            <tr><td>{type.name}</td><td class="num">{type.count}</td><td class="num">{money(type.value)}</td></tr>
          {/each}
        </tbody>
      </table>
    </div>
  {/if}
</section>

<style>
  h3 { margin-bottom: .3rem; }
  .total { margin: 0 0 .75rem; display: flex; flex-wrap: wrap; gap: .25rem .75rem; align-items: baseline; }
  .total strong { font-size: 1.5rem; color: var(--area-views); }
  table { width: 100%; border-collapse: collapse; font-size: .92rem; }
  caption { text-align: left; font-weight: 600; padding-bottom: .3rem; }
  th, td { text-align: left; padding: .3rem .4rem; border-top: 1px solid var(--line); }
  thead th { border-top: 0; color: var(--muted); font-weight: 600; font-size: .85rem; }
  .num { text-align: right; white-space: nowrap; }
</style>
