<!-- The second step for a new space: the features it uses, as switches set to its kind's own, one plain line each.
     Only the features that are built are offered; the kind's others stay on, ready for when they are. -->
<script>
  import { app, packOf, toggleFeature } from '../lib/app.svelte.js';
  import SetupNav from '../components/SetupNav.svelte';
  import SpaceMade from '../components/SpaceMade.svelte';

  /** draft: see newDraft() in lib/spaces.svelte.js. inDialog: shown inside NewSpaceDialog rather than as a setup step. */
  let { draft = $bindable(), inDialog = false } = $props();

  const offered = $derived(app.spaceFeatures.filter((feature) => feature.available));
  const pack = $derived(packOf(draft.preset));
  const intro = $derived(pack
    ? `Switched on for a ${pack.name.toLowerCase()} space. Change any of them now, or later in Settings.`
    : 'Switch on what you need. You can change any of them later in Settings.');
</script>

{#snippet body()}
  <div class="card">
    {#each offered as feature (feature.key)}
      <label class="check">
        <input type="checkbox" class="switch" checked={draft.features.includes(feature.key)}
               onchange={(event) => toggleFeature(draft, feature.key, event.currentTarget.checked)} />
        <span>{feature.name}<span class="muted line">{feature.description}</span></span>
      </label>
    {/each}
  </div>
  <p class="muted note">Labels, scanning, search and the activity feed are always there.</p>
{/snippet}

{#if inDialog}
  <h3>What you'll use</h3>
  <p class="muted">{intro}</p>
  {@render body()}
{:else}
  <section class="gate">
    <h2>What you'll use</h2>
    {#if draft.made}
      <SpaceMade space={draft.made} />
      <SetupNav>Your space is ready.</SetupNav>
    {:else}
      <p class="muted">{intro}</p>
      {@render body()}
      <SetupNav>Next, its places.</SetupNav>
    {/if}
  </section>
{/if}

<style>
  .line { display: block; }
  .note { font-size: .85rem; }
</style>
