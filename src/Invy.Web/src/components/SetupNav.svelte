<!-- The bar at the bottom of each setup step: Back, a short status, and the way forward. It stays in view above the
     page footer, so moving on never needs a scroll. While anything is being saved, both buttons wait and
     the status says what is happening, so a step can't be left half done or skipped by a double click. -->
<script>
  import { app, setupSteps, moveSetup } from '../lib/app.svelte.js';
  import { pendingClick } from '../lib/pending.js';
  import LoaderCircle from '@lucide/svelte/icons/loader-circle';

  /**
   * label: the forward button's text, Next or (on the last step) Finish by default. primary: whether going forward is
   * the suggested action. onnext: runs instead of moving to the next step, and returns a promise.
   * working: what is under way on the step, such as "Checking the certificate…", shown in place of the status.
   * children: the status shown between the buttons.
   */
  let { label = null, primary = true, onnext = null, working = '', children } = $props();

  const steps = $derived(setupSteps());
  const index = $derived(steps.findIndex((step) => step.screen === app.screen));
  const isFirst = $derived(index <= 0);
  const isLast = $derived(index === steps.length - 1);
  const locked = $derived(app.busy > 0);

  const back = pendingClick(() => moveSetup(-1));
  const forward = pendingClick(() => (onnext ? onnext() : moveSetup(1)));
</script>

<div class="setup-nav">
  {#if !isFirst}<button type="button" disabled={locked} onclick={back}>Back</button>{/if}
  <span class="muted status" role="status">
    {#if working}<span class="working"><LoaderCircle size={15} aria-hidden="true" />{working}</span>
    {:else}{@render children?.()}{/if}
  </span>
  <button type="button" class:primary disabled={locked} onclick={forward}>{label ?? (isLast ? 'Finish' : 'Next')}</button>
</div>

<style>
  /* Above the step's content, which scrolls underneath it. */
  .setup-nav { position: sticky; z-index: 5; bottom: calc(3rem + env(safe-area-inset-bottom)); display: flex; gap: 1rem; align-items: center;
    margin-top: 1rem; padding: .6rem 1rem; background: var(--panel); border: 1px solid var(--line);
    border-radius: .5rem; box-shadow: 0 2px 10px rgb(0 0 0 / .08); }
  .status { flex: 1; min-width: 0; }
  .working { display: inline-flex; align-items: center; gap: .4rem; color: var(--ink); }
  .working :global(svg) { flex: none; color: var(--accent); }
  @media (prefers-reduced-motion: no-preference) { .working :global(svg) { animation: spin .8s linear infinite; } }
  /* Phones: the footer isn't pinned to the bottom there (App.svelte), so the bar sits right at the bottom. */
  @media (max-width: 40rem) {
    .setup-nav { bottom: calc(.5rem + env(safe-area-inset-bottom)); gap: .5rem; padding: .5rem .75rem; }
  }
</style>
