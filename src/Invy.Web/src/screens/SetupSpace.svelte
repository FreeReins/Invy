<!-- The first of the steps that make a space: its name, and its kind as cards (icon, one line, a few of its types).
     Setup shows it as a step of its own (draft is app.spaceDraft); NewSpaceDialog shows it inside the dialog
     (inDialog), with its own Back and Next. Picking a kind resets the features and places the later steps offer. -->
<script>
  import { app, chooseKind, moveSetup, run } from '../lib/app.svelte.js';
  import { kindIcon } from '../lib/kinds.js';
  import SetupNav from '../components/SetupNav.svelte';
  import SpaceMade from '../components/SpaceMade.svelte';
  import HelpLink from '../components/HelpLink.svelte';

  /** draft: see newDraft() in lib/spaces.svelte.js. inDialog: shown inside NewSpaceDialog rather than as a setup step. */
  let { draft = $bindable(), inDialog = false } = $props();

  const id = `kind-${Math.random().toString(36).slice(2, 9)}`;
  const kinds = $derived([...app.starterPacks, { key: '', name: 'Start empty', description: 'Make every type yourself, on the Types tab.', types: [] }]);

  /** Moves on once the space has a name. */
  const next = () => run(async () => {
    if (!draft.name.trim()) throw new Error('Give the space a name, such as Home or Workshop.');
    await moveSetup(1);
  });
</script>

{#snippet body()}
  <label>Name <input bind:value={draft.name} required maxlength="80" autocomplete="off" /></label>
  <fieldset class="kinds" aria-describedby="{id}-hint">
    <legend>What is it for?</legend>
    <p id="{id}-hint" class="muted hint">This picks the types it starts with, and what it shows. You can add types from the others later.</p>
    <div class="grid">
      {#each kinds as kind (kind.key)}
        {@const Icon = kindIcon(kind.icon)}
        <label class="kind" class:chosen={draft.preset === kind.key}>
          <input type="radio" name="{id}-preset" value={kind.key} checked={draft.preset === kind.key}
                 onchange={() => chooseKind(draft, kind.key)} />
          <span class="kind-icon" aria-hidden="true"><Icon size={22} /></span>
          <span class="text">
            <strong>{kind.name}</strong>
            <span class="muted">{kind.description}</span>
            {#if kind.types.length}<span class="muted types">{kind.types.slice(0, 4).join(', ')}{kind.types.length > 4 ? ' and more' : ''}</span>{/if}
          </span>
        </label>
      {/each}
    </div>
  </fieldset>
{/snippet}

{#if inDialog}
  <h3>Your space</h3>
  {@render body()}
{:else}
  <section class="gate wide">
    <h2>Your space</h2>
    {#if draft.made}
      <SpaceMade space={draft.made} />
      <SetupNav>Your space is ready.</SetupNav>
    {:else}
      <p class="muted">A space holds your things, their types and the people who share them. Start with one; you can make
        more later. <HelpLink page="using/spaces">How spaces work</HelpLink></p>
      <div class="stack">{@render body()}</div>
      <SetupNav onnext={next}>Next, what you'll use.</SetupNav>
    {/if}
  </section>
{/if}

<style>
  .kinds { border: 0; margin: .5rem 0 0; padding: 0; min-width: 0; }
  legend { font-size: .85rem; color: var(--muted); padding: 0; }
  .hint { margin: .1rem 0 .5rem; font-size: .85rem; }
  .kind { display: flex; gap: .75rem; align-items: flex-start; padding: .8rem; border: 1px solid var(--line);
    border-radius: .5rem; background: var(--panel); color: var(--ink); font-size: .95rem; cursor: pointer; }
  .kind:hover { border-color: var(--accent); }
  .kind.chosen { border-color: var(--accent); box-shadow: 0 0 0 1px var(--accent); background: var(--accent-bg); }
  .kind input { flex: none; margin: .7rem 0 0; accent-color: var(--accent); }
  .kind-icon { flex: none; display: inline-grid; place-items: center; width: 2.4rem; height: 2.4rem; border-radius: .6rem;
    color: var(--accent-2); background: color-mix(in srgb, var(--accent-2) 14%, var(--panel)); }
  .text { display: grid; gap: .15rem; min-width: 0; }
  .types { font-size: .8rem; }
</style>
