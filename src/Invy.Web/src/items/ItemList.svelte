<!-- A list of items (ItemSummary[]), each a link to its page, with what's inside a container or a counted item's amount.
     "Show more" asks for the next page when there is one. -->
<script>
  import { amount, label } from '../lib/items.js';
  import { plural } from '../lib/format.js';
  import { pendingClick } from '../lib/pending.js';
  import Box from '@lucide/svelte/icons/box';
  import Package from '@lucide/svelte/icons/package';
  import ChevronRight from '@lucide/svelte/icons/chevron-right';

  /** items: ItemSummary[]. name: what the list is, for screen readers. onmore: loads the next page, or null. */
  let { items, name, onmore = null } = $props();
</script>

<ul class="items" aria-label={name}>
  {#each items as item (item.id)}
    <li>
      <a href="/browse/{item.id}">
        <span class="icon" aria-hidden="true">{#if item.holdsItems}<Box size={18} />{:else}<Package size={18} />{/if}</span>
        <span class="clip name">{label(item)}</span>
        {#if item.holdsItems}
          <span class="muted count">{plural(item.contents, 'item')}</span>
        {:else if item.quantity != null}
          <span class="muted count">{amount(item)}</span>
        {/if}
        <ChevronRight size={16} aria-hidden="true" />
      </a>
    </li>
  {/each}
</ul>
{#if onmore}
  <button type="button" class="more" onclick={pendingClick(onmore)}>Show more</button>
{/if}

<style>
  .items { list-style: none; margin: 0; padding: 0; }
  .items li + li { border-top: 1px solid var(--line); }
  .items a { display: flex; align-items: center; gap: .6rem; padding: .55rem .25rem; color: var(--ink); text-decoration: none; min-width: 0; }
  .items a:hover { background: color-mix(in srgb, var(--area-browse) 8%, transparent); }
  .items a:focus-visible { outline: 2px solid var(--accent); outline-offset: -2px; }
  .icon { display: inline-flex; color: var(--area-browse); flex: none; }
  .name { flex: 1 1 auto; }
  .count { flex: none; }
  .more { margin-top: .5rem; }
</style>
