<!-- Makes a space in the same three steps as setup (the same components, from screens/): its name and kind, the
     features it uses, and its places and examples. The answers stay in this dialog's draft while moving between
     steps; the space is made once, by the last step's button. The maker becomes its owner. -->
<script>
  import { onMount, tick } from 'svelte';
  import { app, run, createSpace, newDraft, loadSpaceChoices } from '../lib/app.svelte.js';
  import { pending } from '../lib/pending.js';
  import Modal from './Modal.svelte';
  import SetupSpace from '../screens/SetupSpace.svelte';
  import SetupFeatures from '../screens/SetupFeatures.svelte';
  import SetupPlaces from '../screens/SetupPlaces.svelte';

  const STEPS = [SetupSpace, SetupFeatures, SetupPlaces];

  let draft = $state(newDraft());
  let step = $state(0);
  let body;

  const Step = $derived(STEPS[step]);
  const last = $derived(step === STEPS.length - 1);

  onMount(() => { run(loadSpaceChoices); });

  const close = () => { app.newSpaceOpen = false; };

  /** Shows another step and puts focus on its heading, so a screen reader starts there. */
  async function show(index) {
    step = index;
    await tick();
    const heading = body?.querySelector('h3');
    if (!heading) return;
    heading.tabIndex = -1;
    heading.focus();
  }

  function submit(event) {
    event.preventDefault();
    return pending(event, () => run(async () => {
      if (step === 0 && !draft.name.trim()) throw new Error('Give the space a name, such as Home or Workshop.');
      if (!last) return show(step + 1);
      await createSpace(draft);
      close();
    }));
  }
</script>

<Modal title="New space" wide onclose={close}>
  <form class="stack" onsubmit={submit} bind:this={body}>
    <p class="muted steps">Step {step + 1} of {STEPS.length}</p>
    <Step bind:draft inDialog />
    <div class="actions">
      <button type="submit" class="primary">{last ? 'Make the space' : 'Next'}</button>
      {#if step > 0}<button type="button" onclick={() => show(step - 1)}>Back</button>{/if}
      <button type="button" onclick={close}>Cancel</button>
    </div>
  </form>
</Modal>

<style>
  .steps { margin: 0; font-size: .8rem; letter-spacing: .04em; text-transform: uppercase; }
  form :global(h3) { margin: 0; }
  form :global(h3:focus:not(:focus-visible)) { outline: none; }
</style>
