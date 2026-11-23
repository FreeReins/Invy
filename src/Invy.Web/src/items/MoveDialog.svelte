<!-- Moves an item: browse containers one level at a time, then "Move here". Everything inside the item goes with it.
     The item itself is left out, so it can't be moved into itself or anything inside it. -->
<script>
  import { toast } from '../lib/app.svelte.js';
  import { api } from '../lib/api.js';
  import PlacePicker from './PlacePicker.svelte';

  /** spaceId; item: ItemResponse being moved; chain: its containers now (ItemRef[]); onmoved(item); onclose(). */
  let { spaceId, item, chain, onmoved, onclose } = $props();

  function action(here) {
    const where = here?.name ?? 'the top';
    return (here?.id ?? null) === (item.parentId ?? null)
      ? { text: `Already in ${where}`, disabled: true }
      : { text: `Move to ${where}`, disabled: false };
  }

  async function move(here) {
    const moved = await api('POST', `/api/spaces/${spaceId}/items/${item.id}/move`, { parentId: here?.id ?? null });
    toast(`${item.name} moved to ${here?.name ?? 'the top'}.`);
    onmoved(moved);
  }
</script>

<PlacePicker {spaceId} title="Move {item.name}" {chain} exclude={item.id} {action} onpick={move} {onclose} />
