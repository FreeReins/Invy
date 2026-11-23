<!-- One timeline entry as a sentence, with links to the items it names: "Owen moved Box F from Box ABC to Garage". -->
<script>
  import { sentence, changeText, label } from '../lib/items.js';
  import { ago, fullDate } from '../lib/format.js';

  /** event: ItemEventResponse. current: the id of the item whose page this is, shown in bold instead of linked. */
  let { event, current = null } = $props();
</script>

<p class="line">
  {#each sentence(event) as part, index (index)}{#if part.item}{#if part.item.id === current}<strong>{label(part.item)}</strong>{:else}<a href="/browse/{part.item.id}">{label(part.item)}</a>{/if}{:else}{part.text}{/if}{/each}
  <time class="muted" datetime={event.at} title={fullDate(null, event.at)}>{ago(event.at)}</time>
</p>
{#if event.changes.length}
  <ul class="changes muted">
    {#each event.changes as change, index (index)}<li>{changeText(change)}</li>{/each}
  </ul>
{/if}

<style>
  .line { margin: 0; overflow-wrap: anywhere; }
  time { margin-left: .4rem; white-space: nowrap; }
  .changes { margin: .2rem 0 0; padding-left: 1.2rem; font-size: .85rem; }
</style>
