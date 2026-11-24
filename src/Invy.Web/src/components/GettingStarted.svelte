<!-- The getting-started card on Find: the space's kind's steps, each ticked from what the space already has (the API
     works it out), Remove examples while any are left, and Dismiss. It loads again when the space's items change
     (the live "items" topic) and hides once dismissed or once every step is done. Each member dismisses their own. -->
<script>
  import { untrack } from 'svelte';
  import { app, run, toast, canEdit, onLive } from '../lib/app.svelte.js';
  import { api } from '../lib/api.js';
  import { pendingClick } from '../lib/pending.js';
  import { plural } from '../lib/format.js';
  import CircleCheck from '@lucide/svelte/icons/circle-check';
  import Circle from '@lucide/svelte/icons/circle';
  import Sprout from '@lucide/svelte/icons/sprout';

  /** onexamples(): after the examples were removed, so the page can load its list again. */
  let { onexamples = null } = $props();

  /** GettingStartedResponse, or null until loaded. */
  let card = $state(null);

  const done = $derived(card ? card.steps.filter((step) => step.done).length : 0);
  const shown = $derived(card && !card.dismissed && card.steps.length > 0 && done < card.steps.length);

  async function load(id = app.spaceId) {
    if (!id) return;
    const answer = await api('GET', `/api/spaces/${id}/getting-started`);
    if (id === app.spaceId) card = answer;
  }

  // Loads for each space picked. run() reads app.busy, which mustn't count.
  $effect(() => {
    const id = app.spaceId;
    untrack(() => { card = null; run(() => load(id)); });
  });
  $effect(() => onLive('items', () => load()));

  const dismiss = () => run(async () => {
    card = await api('POST', `/api/spaces/${app.spaceId}/getting-started/dismiss`);
    toast('Getting started is put away. Bring it back from Settings, Kind and features.');
  });

  const removeExamples = () => run(async () => {
    const { removed } = await api('DELETE', `/api/spaces/${app.spaceId}/examples`);
    toast(removed ? `Removed ${plural(removed, 'example item')}.` : 'There were no examples left to remove.');
    await load();
    onexamples?.();
  });
</script>

{#if shown}
  <section class="card started" aria-labelledby="started-title">
    <div class="row">
      <h2 id="started-title" class="with-icon"><Sprout size={20} aria-hidden="true" />Getting started</h2>
      <span class="muted">{done} of {card.steps.length} done</span>
    </div>
    <ol class="steps">
      {#each card.steps as step (step.key)}
        <li class:done={step.done}>
          <span class="mark" aria-hidden="true">{#if step.done}<CircleCheck size={18} />{:else}<Circle size={18} />{/if}</span>
          <span><strong>{#if step.done}<span class="sr-only">{'Done: '}</span>{/if}{step.title}</strong>
            <span class="muted line">{step.description}</span></span>
        </li>
      {/each}
    </ol>
    <div class="actions">
      {#if card.examplesLeft > 0 && canEdit()}
        <button type="button" onclick={pendingClick(removeExamples)}>Remove examples ({card.examplesLeft})</button>
      {/if}
      <button type="button" onclick={pendingClick(dismiss)}>Dismiss</button>
    </div>
  </section>
{/if}

<style>
  .started { margin: 0 0 1rem; border-color: color-mix(in srgb, var(--area-find) 40%, var(--line)); }
  h2 { margin: 0; font-size: 1.1rem; }
  h2 :global(svg) { color: var(--area-find); }
  .steps { list-style: none; margin: .6rem 0; padding: 0; display: grid; gap: .45rem; }
  .steps li { display: flex; gap: .55rem; align-items: flex-start; }
  .mark { display: inline-flex; flex: none; margin-top: .1rem; color: var(--muted); }
  li.done .mark { color: var(--good); }
  li.done strong { color: var(--muted); text-decoration: line-through; }
  .line { display: block; font-size: .9rem; }
</style>
