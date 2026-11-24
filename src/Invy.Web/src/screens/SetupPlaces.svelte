<!-- The last step for a new space: the places it starts with, as chips to tick from its kind's suggestions plus any
     typed in, and whether to add a few example items. All of it can be skipped: finishing with nothing ticked makes
     an empty space. In setup, Finish makes the space and opens Find. -->
<script>
  import { packOf, draftPlaces } from '../lib/app.svelte.js';
  import { listWords } from '../lib/format.js';
  import SetupNav from '../components/SetupNav.svelte';
  import SpaceMade from '../components/SpaceMade.svelte';
  import Plus from '@lucide/svelte/icons/plus';
  import X from '@lucide/svelte/icons/x';

  /** draft: see newDraft() in lib/spaces.svelte.js. inDialog: shown inside NewSpaceDialog rather than as a setup step. */
  let { draft = $bindable(), inDialog = false } = $props();

  const id = `places-${Math.random().toString(36).slice(2, 9)}`;
  const pack = $derived(packOf(draft.preset));
  const suggested = $derived(pack?.places?.suggest ?? []);
  const placeType = $derived(pack?.places?.type ?? null);
  let typed = $state('');

  const known = (name) => [...suggested, ...draft.ownPlaces].some((place) => place.toLowerCase() === name.toLowerCase());

  function tick(name, on) {
    draft.places = on ? [...draft.places, name] : draft.places.filter((place) => place !== name);
  }

  /** Adds a typed place, ticking the suggestion instead when it is one. */
  function add(event) {
    event.preventDefault();
    const name = typed.trim();
    if (!name) return;
    const suggestion = suggested.find((place) => place.toLowerCase() === name.toLowerCase());
    if (suggestion) { if (!draft.places.includes(suggestion)) tick(suggestion, true); }
    else if (!known(name)) draft.ownPlaces = [...draft.ownPlaces, name];
    typed = '';
  }

  const status = $derived.by(() => {
    const count = draftPlaces(draft).length;
    if (!count && !draft.examples) return 'Nothing ticked: the space starts empty.';
    return `${count ? `${count} ${count === 1 ? 'place' : 'places'}` : 'No places'}${draft.examples ? ', with examples' : ''}.`;
  });
</script>

{#snippet body()}
  {#if suggested.length}
    <fieldset class="places">
      <legend>Likely places{placeType ? `, made as a ${placeType.toLowerCase()} each` : ''}</legend>
      <div class="chips">
        {#each suggested as place (place)}
          <label class="tick-chip"><input type="checkbox" checked={draft.places.includes(place)}
                 onchange={(event) => tick(place, event.currentTarget.checked)} />{place}</label>
        {/each}
      </div>
    </fieldset>
  {/if}
  {#if draft.ownPlaces.length}
    <ul class="chips own" aria-label="Your own places">
      {#each draft.ownPlaces as place (place)}
        <li class="tick-chip on">{place}<button type="button" class="icon" aria-label="Remove {place}"
            onclick={() => { draft.ownPlaces = draft.ownPlaces.filter((other) => other !== place); }}><X size={14} /></button></li>
      {/each}
    </ul>
  {/if}
  <!-- Not a form: inside NewSpaceDialog this sits in the dialog's own form. Enter adds the place. -->
  <div class="add">
    <label for="{id}-own">Your own place</label>
    <div class="add-row">
      <input id="{id}-own" bind:value={typed} maxlength="200" autocomplete="off" placeholder="Such as Spare room"
             onkeydown={(event) => { if (event.key === 'Enter') add(event); }} />
      <button type="button" class="with-icon" onclick={add}><Plus size={16} aria-hidden="true" />Add</button>
    </div>
  </div>
  {#if pack?.examples.length}
    <label class="check examples">
      <input type="checkbox" bind:checked={draft.examples} />
      <span>Add a few example items
        <span class="muted line">{listWords(pack.examples)}, to try things out. Remove them all at once later.</span></span>
    </label>
  {/if}
{/snippet}

{#if inDialog}
  <h3>Places</h3>
  <p class="muted">Where things are kept, at the top of the space. Skip this if you'd rather add them as you go.</p>
  {@render body()}
{:else}
  <section class="gate wide">
    <h2>Places</h2>
    {#if draft.made}
      <SpaceMade space={draft.made} />
      <SetupNav>Your space is ready. Finish opens it.</SetupNav>
    {:else}
      <p class="muted">Where things are kept, at the top of your space. Skip this if you'd rather add them as you go.</p>
      {@render body()}
      <SetupNav>{status} Finish makes the space.</SetupNav>
    {/if}
  </section>
{/if}

<style>
  .places { border: 0; margin: .5rem 0 0; padding: 0; min-width: 0; }
  legend { font-size: .85rem; color: var(--muted); padding: 0; }
  .chips { gap: .4rem; }
  ul.chips { list-style: none; padding: 0; margin: .5rem 0 0; }
  /* A chip that is a checkbox: ticked ones fill with the accent colour. */
  .tick-chip { display: inline-flex; align-items: center; gap: .35rem; padding: .3rem .7rem; border: 1px solid var(--line);
    border-radius: 1rem; background: var(--panel); color: var(--ink); font-size: .9rem; cursor: pointer; }
  .tick-chip:hover { border-color: var(--accent); }
  .tick-chip:has(input:checked), .tick-chip.on { border-color: var(--accent); background: var(--accent-bg); }
  .tick-chip:has(input:focus-visible) { outline: 2px solid var(--accent); outline-offset: 2px; }
  .tick-chip input { margin: 0; accent-color: var(--accent); }
  .tick-chip .icon { width: 1.4rem; height: 1.4rem; padding: 0; margin-right: -.4rem; }
  .add { display: grid; gap: .15rem; margin-top: .9rem; font-size: .85rem; color: var(--muted); }
  .add-row { display: flex; gap: .5rem; }
  .add-row input { flex: 1; }
  .examples { margin-top: .9rem; border-top: 1px solid var(--line); }
  .line { display: block; }
</style>
