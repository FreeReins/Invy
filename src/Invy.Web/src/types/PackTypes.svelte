<!-- Types, Add from another pack: copies one type, with its fields, from any starter pack into the space. Types the
     space already has a type of the same name for are shown but can't be added again. -->
<script>
  import { app, run, toast } from '../lib/app.svelte.js';
  import { api } from '../lib/api.js';
  import { pendingClick } from '../lib/pending.js';
  import Modal from '../components/Modal.svelte';

  /** spaceId; have: the names of the space's types; onadded(ItemTypeResponse); onclose(). */
  let { spaceId, have, onadded, onclose } = $props();

  const other = app.starterPacks.find((pack) => pack.key !== app.spaces.find((space) => space.id === spaceId)?.preset);
  let packKey = $state(other?.key ?? app.starterPacks[0]?.key ?? '');
  const pack = $derived(app.starterPacks.find((candidate) => candidate.key === packKey));
  const taken = $derived(new Set(have.map((name) => name.toLowerCase())));

  const add = (name) => run(async () => {
    const type = await api('POST', `/api/spaces/${spaceId}/types/from-pack`, { pack: packKey, type: name });
    onadded(type);
    toast(`Added ${type.name}, with its fields, from the ${pack.name} pack.`);
  });
</script>

<Modal title="Add from another pack" {onclose}>
  <div class="stack">
    <p class="muted">Copy a type, with its fields, from any kind of space. You can change it afterwards.</p>
    <label>Pack
      <select bind:value={packKey}>
        {#each app.starterPacks as option (option.key)}<option value={option.key}>{option.name}</option>{/each}
      </select>
    </label>
    {#if pack}
      <ul class="plain" aria-label="Types in the {pack.name} pack">
        {#each pack.types as name (name)}
          <li class="row">
            <span>{name}</span>
            {#if taken.has(name.toLowerCase())}
              <span class="muted">Already here</span>
            {:else}
              <button type="button" class="small" aria-label="Add {name}" onclick={pendingClick(() => add(name))}>Add</button>
            {/if}
          </li>
        {/each}
      </ul>
    {/if}
  </div>
</Modal>

<style>
  .row { align-items: center; }
</style>
