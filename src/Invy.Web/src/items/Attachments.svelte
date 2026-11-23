<!-- An item's photos and PDFs: thumbnails that open the full file, and for editors a button to add more (on a phone
     it offers the camera) and one to remove each. Photos are made smaller in the browser before they are sent. -->
<script>
  import { onMount } from 'svelte';
  import { run, ask, toast, canEdit } from '../lib/app.svelte.js';
  import { api } from '../lib/api.js';
  import { pendingClick } from '../lib/pending.js';
  import { base64, shrinkPhoto } from '../lib/photos.js';
  import ImagePlus from '@lucide/svelte/icons/image-plus';
  import FileText from '@lucide/svelte/icons/file-text';
  import Trash from '@lucide/svelte/icons/trash-2';

  /** The largest file the server keeps, in MB (ValidationEngine.MaxAttachmentBytes). */
  const MAX_MB = 10;

  /** spaceId; item: ItemResponse; onchanged(): after adding or removing, so the timeline reloads. */
  let { spaceId, item, onchanged } = $props();

  let attachments = $state(null);
  let picker = $state();
  let adding = $state(false);

  const base = $derived(`/api/spaces/${spaceId}`);
  const editor = $derived(canEdit() && item.status !== 'Disposed' && item.status !== 'Archived');
  const fileUrl = (attachment) => `${base}/attachments/${attachment.id}/file`;
  const thumbUrl = (attachment) => `${base}/attachments/${attachment.id}/${attachment.hasThumbnail ? 'thumbnail' : 'file'}`;

  const load = async () => { attachments = await api('GET', `${base}/items/${item.id}/attachments`); };
  onMount(() => { run(load); });

  /** A photo shrunk to at most 2560 pixels across; a PDF as it is. */
  const data = (file) => (file.type.startsWith('image/') && file.type !== 'image/gif' ? shrinkPhoto(file, 2560) : base64(file));

  const chosen = (event) => run(async () => {
    const files = [...event.currentTarget.files];
    event.currentTarget.value = '';
    if (!files.length) return;
    adding = true;
    try {
      for (const file of files) {
        if (file.type === 'application/pdf' && file.size > MAX_MB * 1024 * 1024) {
          throw new Error(`${file.name} is over ${MAX_MB} MB. Attach a smaller PDF.`);
        }
        await api('POST', `${base}/items/${item.id}/attachments`, { name: file.name, data: await data(file) });
      }
      toast(files.length === 1 ? `${files[0].name} attached.` : `${files.length} files attached.`);
      await load();
      onchanged();
    } finally {
      adding = false;
    }
  });

  const remove = (attachment) => run(async () => {
    const confirmed = await ask({
      title: `Remove ${attachment.name}?`,
      message: `It is taken off ${item.name}. Backups made before now still have it.`,
      confirmLabel: 'Remove', danger: true,
    });
    if (!confirmed) return;
    await api('DELETE', `${base}/attachments/${attachment.id}`);
    toast(`${attachment.name} removed.`);
    await load();
  });
</script>

<section class="card">
  <div class="row">
    <h3>Photos and files</h3>
    {#if editor}
      <button type="button" class="small with-icon" disabled={adding} onclick={() => picker.click()}>
        <ImagePlus size={14} aria-hidden="true" />{adding ? 'Attaching…' : 'Add a photo or PDF'}</button>
      <input bind:this={picker} type="file" accept="image/jpeg,image/png,image/webp,image/gif,application/pdf" multiple hidden onchange={chosen} />
    {/if}
  </div>
  {#if attachments === null}
    <span class="skeleton line"></span>
  {:else if attachments.length === 0}
    <p class="muted">No photos or files yet.{editor ? ' Add a photo of it, a receipt or its manual.' : ''}</p>
  {:else}
    <ul class="plain grid">
      {#each attachments as attachment (attachment.id)}
        <li>
          <a href={fileUrl(attachment)} target="_blank" rel="noopener" class="tile" aria-label="Open {attachment.name}">
            {#if attachment.kind === 'Photo'}
              <img src={thumbUrl(attachment)} alt="" loading="lazy" />
            {:else}
              <span class="doc" aria-hidden="true"><FileText size={32} /></span>
            {/if}
          </a>
          <div class="under">
            <span class="name">{attachment.name}</span>
            {#if editor}
              <button type="button" class="icon" aria-label="Remove {attachment.name}" onclick={pendingClick(() => remove(attachment))}><Trash size={16} /></button>
            {/if}
          </div>
        </li>
      {/each}
    </ul>
  {/if}
</section>

<style>
  .row { align-items: center; }
  h3 { margin: .25rem 0 .5rem; font-size: 1.05rem; }
  .grid { display: grid; gap: .75rem; grid-template-columns: repeat(auto-fill, minmax(8rem, 1fr)); margin: 0; }
  .grid li { min-width: 0; }
  .tile { display: block; }
  .tile img, .doc { width: 100%; aspect-ratio: 1; object-fit: cover; border-radius: .5rem; border: 1px solid var(--line); background: var(--chip); }
  .doc { display: grid; place-items: center; color: var(--muted); }
  .under { display: flex; align-items: center; justify-content: space-between; gap: .25rem; margin-top: .2rem; }
  .name { font-size: .8rem; min-width: 0; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
</style>
