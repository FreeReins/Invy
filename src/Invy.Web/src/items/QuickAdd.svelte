<!-- Several items in one line: "baseball, glove, 2 bats". They go in the container given, or the Inbox without one. -->
<script>
  import { run, toast } from '../lib/app.svelte.js';
  import { api } from '../lib/api.js';
  import { pending } from '../lib/pending.js';
  import { plural } from '../lib/format.js';
  import Zap from '@lucide/svelte/icons/zap';

  /** spaceId; parent: the container (ItemResponse) or null for the Inbox; onadded(items). */
  let { spaceId, parent = null, onadded } = $props();

  let text = $state('');

  function submit(event) {
    event.preventDefault();
    return pending(event, () => run(async () => {
      const made = await api('POST', `/api/spaces/${spaceId}/quick-add`, { text, parentId: parent?.id ?? null });
      toast(`${plural(made.length, 'item')} added to ${parent?.name ?? 'the Inbox'}.`);
      text = '';
      onadded(made);
    }));
  }
</script>

<form class="quick" onsubmit={submit}>
  <label>
    <span>Quick add {parent ? `to ${parent.name}` : 'to the Inbox'}</span>
    <input bind:value={text} required placeholder="baseball, glove, 2 bats" autocomplete="off" />
  </label>
  <button type="submit" class="with-icon"><Zap size={16} aria-hidden="true" />Add</button>
</form>

<style>
  .quick { display: flex; gap: .5rem; align-items: end; margin: .5rem 0 .75rem; }
  .quick label { flex: 1 1 auto; min-width: 0; }
  .quick input { width: 100%; box-sizing: border-box; }
</style>
