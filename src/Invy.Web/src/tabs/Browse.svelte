<!-- The Browse tab: the places at the top of the space and the Inbox (/browse), or one item's page (/browse/{id}),
     whose contents lead one level further down. Nothing is loaded below the level shown. -->
<script>
  import { untrack } from 'svelte';
  import { app, run, currentSpace, canEdit } from '../lib/app.svelte.js';
  import { api } from '../lib/api.js';
  import NoSpace from '../components/NoSpace.svelte';
  import HelpLink from '../components/HelpLink.svelte';
  import ItemList from '../items/ItemList.svelte';
  import ItemPage from '../items/ItemPage.svelte';
  import ItemForm from '../items/ItemForm.svelte';
  import QuickAdd from '../items/QuickAdd.svelte';
  import FolderTree from '@lucide/svelte/icons/folder-tree';
  import Plus from '@lucide/svelte/icons/plus';

  let top = $state(null);
  let inbox = $state(null);
  let types = $state([]);
  let adding = $state(false);

  const space = $derived(currentSpace());

  async function load() {
    const base = `/api/spaces/${app.spaceId}`;
    [top, inbox, types] = await Promise.all([api('GET', `${base}/items`), api('GET', `${base}/inbox`), api('GET', `${base}/types`)]);
  }

  const reload = () => run(load);

  // The top level loads whenever it is shown, for whichever space is picked. run() reads app.busy, which mustn't count.
  $effect(() => {
    const shown = app.spaceId && !app.itemId;
    untrack(() => {
      top = null;
      inbox = null;
      if (shown) reload();
    });
  });

  const more = (list, path) => run(async () => {
    const page = await api('GET', `/api/spaces/${app.spaceId}/${path}${path.includes('?') ? '&' : '?'}after=${encodeURIComponent(list.next)}`);
    const merged = { items: [...list.items, ...page.items], next: page.next };
    if (list === top) top = merged; else inbox = merged;
  });
</script>

{#if !space}
  <NoSpace area="var(--area-browse)" />
{:else if app.itemId}
  {#key `${app.spaceId}/${app.itemId}`}<ItemPage spaceId={app.spaceId} id={app.itemId} />{/key}
{:else}
  <section class="browse">
    <div class="row head">
      <div>
        <h2>Browse</h2>
        <p class="muted">The places at the top of {space.name}. Open one to see what's inside.</p>
      </div>
      {#if canEdit()}
        <button type="button" class="primary with-icon" onclick={() => { adding = true; }}><Plus size={16} aria-hidden="true" />New item</button>
      {/if}
    </div>

    <div class="card">
      {#if top === null}
        <span class="skeleton line"></span><span class="skeleton line"></span>
      {:else if top.items.length === 0}
        <div class="empty">
          <span class="empty-icon" style="--area: var(--area-browse)" aria-hidden="true"><FolderTree size={26} /></span>
          <h3>No places yet</h3>
          <p class="muted">Start with the big places, such as a shed, a garage or a room. Then put boxes in them, and things in the boxes.</p>
        </div>
      {:else}
        <ItemList items={top.items} name="Places in {space.name}" onmore={top.next ? () => more(top, 'items') : null} />
      {/if}
    </div>

    <h3>Inbox</h3>
    <p class="muted">Things with no place and no type yet, to tidy later.</p>
    <div class="card">
      {#if canEdit()}<QuickAdd spaceId={app.spaceId} onadded={reload} />{/if}
      {#if inbox?.items.length}
        <ItemList items={inbox.items} name="Inbox" onmore={inbox.next ? () => more(inbox, 'inbox') : null} />
      {:else if inbox}
        <p class="muted">The Inbox is empty.</p>
      {/if}
    </div>
    <p class="muted"><HelpLink page="using/items">Items, containers and moves</HelpLink></p>
  </section>

  {#if adding}
    <ItemForm spaceId={app.spaceId} {types} onsaved={() => { adding = false; reload(); }} onclose={() => { adding = false; }} />
  {/if}
{/if}

<style>
  .browse { margin-top: 1.25rem; }
  .head { align-items: center; margin-bottom: .5rem; }
  h2 { margin: 0 0 .2rem; }
  h3 { margin: 1.25rem 0 .2rem; font-size: 1.05rem; }
  .empty h3 { margin: .25rem 0; }
</style>
