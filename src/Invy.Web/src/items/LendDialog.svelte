<!-- Lends an item out, whole, to a person or a place, with an optional date it's due back. -->
<script>
  import { run, toast } from '../lib/app.svelte.js';
  import { api } from '../lib/api.js';
  import { pending } from '../lib/pending.js';
  import { todayIso } from '../lib/format.js';
  import Modal from '../components/Modal.svelte';

  /** spaceId; item: ItemResponse; onlent(item); onclose(). */
  let { spaceId, item, onlent, onclose } = $props();

  let to = $state('');
  let dueOn = $state('');

  function submit(event) {
    event.preventDefault();
    return pending(event, () => run(async () => {
      const lent = await api('POST', `/api/spaces/${spaceId}/items/${item.id}/lend`, { to: to.trim(), dueOn: dueOn || null });
      toast(`${item.name} lent to ${lent.lentTo}.`);
      onlent(lent);
    }));
  }
</script>

<Modal title="Lend {item.name}" {onclose}>
  <form class="stack" onsubmit={submit}>
    <label>Lent to <input bind:value={to} required maxlength="100" autocomplete="off" placeholder="Sam, or the church hall" /></label>
    <label>Due back <span class="muted">Leave it empty for no date.</span> <input type="date" bind:value={dueOn} min={todayIso()} /></label>
    <div class="actions">
      <button type="submit" class="primary">Lend</button>
      <button type="button" onclick={onclose}>Cancel</button>
    </div>
  </form>
</Modal>
