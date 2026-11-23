<!-- Sticks a new QR label on an item that already exists: scan the label, or type its code. A label the item had
     before is freed, to use on something else. -->
<script>
  import { run, toast, labelCode } from '../lib/app.svelte.js';
  import { api } from '../lib/api.js';
  import Modal from '../components/Modal.svelte';
  import Scanner from '../components/Scanner.svelte';

  /** spaceId; item: ItemResponse; onattached(item); onclose(). */
  let { spaceId, item, onattached, onclose } = $props();

  const scanned = (text) => run(async () => {
    const code = labelCode(text);
    if (!code) throw new Error(`"${text}" isn't an Invy label. Scan a QR label printed from Invy.`);
    const changed = await api('POST', `/api/spaces/${spaceId}/labels/${code}/attach`, { itemId: item.id });
    toast(`Label ${code} is on ${item.name} now.`);
    onattached(changed);
  });
</script>

<Modal title="Stick a label on {item.name}" {onclose}>
  <p class="muted">Use a label that isn't on anything yet.{item.labelCode ? ` Its label now, ${item.labelCode}, is freed for something else.` : ''}</p>
  <Scanner onscan={scanned} prompt="Point the camera at the new label" typeLabel="Or type the label's code" />
</Modal>
