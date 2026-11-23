<!-- "Adding to Box F": where scanning and quick add put new things in this space, until changed. Change picks
     another container; Top goes back to the top of the space. -->
<script>
  import { app, stickyPlace, setStickyPlace, toast } from '../lib/app.svelte.js';
  import { label } from '../lib/items.js';
  import PlacePicker from './PlacePicker.svelte';
  import MapPin from '@lucide/svelte/icons/map-pin';

  let picking = $state(false);
  const place = $derived(stickyPlace());

  async function pick(here) {
    setStickyPlace(here);
    toast(here ? `New things go into ${here.name} now.` : 'New things go at the top now.');
    picking = false;
  }
</script>

<div class="sticky" role="group" aria-label="Where new things go">
  <MapPin size={18} aria-hidden="true" />
  <span class="what">Adding to <strong>{place ? label(place) : 'the top'}</strong></span>
  <span class="buttons">
    <button type="button" class="small" onclick={() => { picking = true; }}>Change</button>
    {#if place}<button type="button" class="small" onclick={() => pick(null)}>Top</button>{/if}
  </span>
</div>

{#if picking}
  <PlacePicker spaceId={app.spaceId} title="Where should new things go?" chain={place ? [place] : []}
    action={(here) => `Add to ${here?.name ?? 'the top'}`} onpick={pick} onclose={() => { picking = false; }} />
{/if}

<style>
  .sticky { display: flex; flex-wrap: wrap; align-items: center; gap: .5rem; padding: .55rem .75rem; border-radius: .6rem;
    border: 1px solid color-mix(in srgb, var(--area-scan) 40%, var(--line));
    background: color-mix(in srgb, var(--area-scan) 8%, var(--panel)); }
  .sticky > :global(svg) { color: var(--area-scan); flex: none; }
  .what { flex: 1 1 auto; min-width: 0; overflow-wrap: anywhere; }
  .buttons { display: flex; gap: .35rem; }
</style>
