<!-- Sets aside some of an item for an open project, or starts a new project for it. A single thing is set aside whole. -->
<script>
  import { untrack } from 'svelte';
  import { run, toast } from '../lib/app.svelte.js';
  import { api } from '../lib/api.js';
  import { pending } from '../lib/pending.js';
  import Modal from '../components/Modal.svelte';

  /** spaceId; item: ItemResponse; onreserved(); onclose(). */
  let { spaceId, item, onreserved, onclose } = $props();

  let projects = $state(null);
  let projectId = $state('');
  let newName = $state('');
  let amount = $state(1);

  const counted = $derived(item.quantity != null);
  const free = $derived(counted ? Math.max(0, item.quantity - item.reserved) : 1);

  $effect(() => {
    untrack(() => run(async () => {
      projects = await api('GET', `/api/spaces/${spaceId}/projects`);
      projectId = projects[0]?.id ?? 'new';
    }));
  });

  function submit(event) {
    event.preventDefault();
    return pending(event, () => run(async () => {
      let id = projectId;
      if (id === 'new') id = (await api('POST', `/api/spaces/${spaceId}/projects`, { name: newName.trim(), notes: null, version: 0 })).id;
      const project = projects.find((candidate) => candidate.id === id);
      const already = project?.items.find((entry) => entry.item.id === item.id)?.amount ?? 0;
      const saved = await api('POST', `/api/spaces/${spaceId}/projects/${id}/reserve`, {
        itemId: item.id, amount: counted ? already + Number(amount) : 1,
      });
      toast(`${item.name} set aside for ${saved.name}.`);
      onreserved();
    }));
  }
</script>

<Modal title="Set aside {item.name}" {onclose}>
  {#if projects === null}
    <span class="skeleton line"></span>
  {:else}
    <form class="stack" onsubmit={submit}>
      <label>Project
        <select bind:value={projectId}>
          {#each projects as project (project.id)}<option value={project.id}>{project.name}</option>{/each}
          <option value="new">A new project…</option>
        </select>
      </label>
      {#if projectId === 'new'}
        <label>New project's name <input bind:value={newName} required maxlength="100" autocomplete="off" placeholder="Build a bookshelf" /></label>
      {/if}
      {#if counted}
        <label>How much <span class="muted">{free} {item.unit ?? ''} not set aside yet</span>
          <input type="number" bind:value={amount} min="0" max={free} step="any" required /></label>
      {/if}
      <div class="actions">
        <button type="submit" class="primary">Set aside</button>
        <button type="button" onclick={onclose}>Cancel</button>
      </div>
    </form>
  {/if}
</Modal>
