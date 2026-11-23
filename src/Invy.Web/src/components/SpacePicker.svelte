<!-- The space the page shows, picked in a bar under the header, and the button that makes a new one. Every space-owned
     tab follows app.spaceId. Hidden until the person is in a space; NoSpace.svelte offers the first one instead. -->
<script>
  import { app, pickSpace } from '../lib/app.svelte.js';
  import NewSpaceDialog from './NewSpaceDialog.svelte';
  import FolderPlus from '@lucide/svelte/icons/folder-plus';
</script>

{#if app.spaces.length}
  <section class="space-bar" aria-label="Space">
    <label class="picker">
      <span>Space</span>
      <select value={app.spaceId} onchange={(event) => pickSpace(event.currentTarget.value)}>
        {#each app.spaces as space (space.id)}
          <option value={space.id}>{space.name}{space.role === 'Owner' ? '' : ` (${space.role.toLowerCase()})`}</option>
        {/each}
      </select>
    </label>
    <button type="button" class="with-icon small" onclick={() => { app.newSpaceOpen = true; }}>
      <FolderPlus size={16} aria-hidden="true" />New space
    </button>
  </section>
{/if}

{#if app.newSpaceOpen}<NewSpaceDialog />{/if}

<style>
  .space-bar { display: flex; flex-wrap: wrap; align-items: center; gap: .5rem; margin-top: 1rem; }
  .picker { grid-auto-flow: column; align-items: center; gap: .5rem; font-size: .9rem; }
  .picker select { min-width: 12rem; max-width: 100%; }
  @media (max-width: 40rem) {
    .picker { flex: 1 1 auto; }
    .picker select { min-width: 0; width: 100%; }
  }
</style>
