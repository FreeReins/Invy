<!-- Settings, This space: the space shown's name, kind and feature switches, which owners change and everyone else
     sees, then Remove examples (editors) and Show getting started again (each member's own card). Changing the kind
     changes only the hints and the getting-started steps, never the types. -->
<script>
  import { untrack } from 'svelte';
  import { app, run, toast, currentSpace, isOwner, canEdit, replaceSpace, loadSpaceChoices, toggleFeature, packOf } from '../lib/app.svelte.js';
  import { api } from '../lib/api.js';
  import { pending, pendingClick } from '../lib/pending.js';
  import { plural } from '../lib/format.js';
  import NoSpace from '../components/NoSpace.svelte';

  const space = $derived(currentSpace());
  const owner = $derived(isOwner());
  const path = $derived(`/api/spaces/${app.spaceId}`);
  const offered = $derived(app.spaceFeatures.filter((feature) => feature.available));

  let name = $state('');
  /** GettingStartedResponse: how many examples are left, and whether this person put the card away. */
  let card = $state(null);

  // Loads for each space picked. run() reads app.busy, which mustn't count.
  $effect(() => {
    const id = app.spaceId;
    untrack(() => {
      name = currentSpace()?.name ?? '';
      card = null;
      if (id) run(async () => {
        await loadSpaceChoices();
        card = await api('GET', `/api/spaces/${id}/getting-started`);
      });
    });
  });

  function rename(event) {
    event.preventDefault();
    return pending(event, () => run(async () => {
      const renamed = await api('PUT', path, { name: name.trim() });
      replaceSpace(renamed);
      toast(`Renamed to ${renamed.name}.`);
    }));
  }

  const setKind = (key) => run(async () => {
    const changed = await api('PUT', `${path}/kind`, { kind: key || null });
    replaceSpace(changed);
    card = await api('GET', `${path}/getting-started`);
    toast(`${changed.name} is now ${key ? `a ${packOf(key)?.name.toLowerCase()} space` : 'a space of no particular kind'}. Its types stay as they are.`);
  });

  async function setFeature(feature, box) {
    const draft = { features: [...space.features] };
    toggleFeature(draft, feature.key, box.checked);
    await run(async () => {
      replaceSpace(await api('PUT', `${path}/features`, { features: draft.features }));
      toast(`${feature.name} ${box.checked ? 'switched on' : 'switched off'} in ${space.name}.`);
    });
    box.checked = currentSpace().features.includes(feature.key); // back to the saved value if saving failed
  }

  const removeExamples = () => run(async () => {
    const { removed } = await api('DELETE', `${path}/examples`);
    card = await api('GET', `${path}/getting-started`);
    toast(removed ? `Removed ${plural(removed, 'example item')}.` : 'There were no examples left to remove.');
  });

  const showAgain = () => run(async () => {
    card = await api('POST', `${path}/getting-started/show`);
    toast('Getting started is back on the Find tab.');
  });
</script>

<h2>Kind and features</h2>
{#if !space}
  <NoSpace area="var(--area-settings)" />
{:else}
  {#if !owner}<p class="muted">Only an owner of {space.name} can change these.</p>{/if}

  {#if owner}
    <form class="inline" onsubmit={rename}>
      <label>Name <input bind:value={name} required maxlength="80" autocomplete="off" /></label>
      <button type="submit">Rename</button>
    </form>
  {:else}
    <p>Name: <strong>{space.name}</strong></p>
  {/if}

  <h3>Kind</h3>
  <label class="kind">What it's for
    <select value={space.preset ?? ''} disabled={!owner} onchange={(event) => setKind(event.currentTarget.value)}>
      {#each app.starterPacks as pack (pack.key)}<option value={pack.key}>{pack.name}</option>{/each}
      <option value="">None</option>
    </select>
  </label>
  <p class="muted hint">The kind picks the hints on empty screens and the getting-started steps. Changing it keeps every type.</p>

  <h3>Features</h3>
  <div class="card">
    {#each offered as feature (feature.key)}
      <label class="check">
        <input type="checkbox" class="switch" checked={space.features.includes(feature.key)} disabled={!owner}
               onchange={(event) => setFeature(feature, event.currentTarget)} />
        <span>{feature.name}<span class="muted line">{feature.description}</span></span>
      </label>
    {/each}
  </div>
  <p class="muted hint">A feature that is off is only hidden: its details stay, and come back when it is switched on.</p>

  {#if card}
    <h3>Getting started</h3>
    <div class="actions">
      {#if canEdit()}
        <button type="button" disabled={card.examplesLeft === 0} onclick={pendingClick(removeExamples)}>
          {card.examplesLeft ? `Remove examples (${card.examplesLeft})` : 'No examples left'}</button>
      {/if}
      {#if card.dismissed}<button type="button" onclick={pendingClick(showAgain)}>Show getting started again</button>{/if}
    </div>
    {#if !card.dismissed}<p class="muted hint">The getting-started card is on the Find tab until you dismiss it or every step is done.</p>{/if}
  {/if}
{/if}

<style>
  h3 { margin: 1.25rem 0 .4rem; font-size: 1.05rem; }
  .kind { max-width: 20rem; }
  .hint { font-size: .85rem; margin: .35rem 0 0; }
  .line { display: block; }
</style>
