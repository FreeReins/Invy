<!-- Makes a space: a name, and a starter pack of types to begin with (or none). The maker becomes its owner. -->
<script>
  import { app, run, createSpace } from '../lib/app.svelte.js';
  import { pending } from '../lib/pending.js';
  import Modal from './Modal.svelte';

  let name = $state(app.spaces.length ? '' : 'Home');
  let preset = $state(app.starterPacks[0]?.key ?? '');

  const close = () => { app.newSpaceOpen = false; };

  function submit(event) {
    event.preventDefault();
    return pending(event, () => run(async () => {
      await createSpace(name.trim(), preset);
      close();
    }));
  }
</script>

<Modal title="New space" wide onclose={close}>
  <form class="stack" onsubmit={submit}>
    <p class="muted">A space has its own items, types and people, such as Home, Workshop or Shop. You'll be its owner.</p>
    <label>Name <input bind:value={name} required maxlength="80" autocomplete="off" /></label>
    <fieldset>
      <legend>Start with these types</legend>
      {#each app.starterPacks as pack (pack.key)}
        <label class="check">
          <input type="radio" name="preset" value={pack.key} bind:group={preset} />
          <span>
            <strong>{pack.name}</strong>
            <span class="muted">{pack.description}</span>
            <span class="muted types">{pack.types.join(', ')}</span>
          </span>
        </label>
      {/each}
      <label class="check">
        <input type="radio" name="preset" value="" bind:group={preset} />
        <span><strong>None</strong> <span class="muted">Make every type yourself, on the Types tab.</span></span>
      </label>
    </fieldset>
    <div class="actions">
      <button type="submit" class="primary">Make the space</button>
      <button type="button" onclick={close}>Cancel</button>
    </div>
  </form>
</Modal>

<style>
  fieldset { border: 0; margin: 0; padding: 0; }
  legend { font-size: .85rem; color: var(--muted); margin-bottom: .25rem; }
  .check { color: var(--ink); font-size: .95rem; cursor: pointer; }
  .check > span { display: grid; gap: .1rem; }
  .types { font-size: .8rem; }
</style>
