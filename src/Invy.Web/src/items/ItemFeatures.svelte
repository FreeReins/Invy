<!-- The space features on an item's page, each shown only while its switch is on: its low-stock level, who it's lent
     to, upkeep dates to mark done, where it's going in a move (with its manifest), and the projects that set some
     aside. Editors change them; everyone sees them. -->
<script>
  import { untrack } from 'svelte';
  import { run, toast, canEdit } from '../lib/app.svelte.js';
  import { api } from '../lib/api.js';
  import { pendingClick } from '../lib/pending.js';
  import { amount, display } from '../lib/items.js';
  import { has } from '../lib/features.js';
  import { todayIso } from '../lib/format.js';
  import PlacePicker from './PlacePicker.svelte';
  import LendDialog from './LendDialog.svelte';
  import ReserveDialog from './ReserveDialog.svelte';
  import Handshake from '@lucide/svelte/icons/handshake';
  import Truck from '@lucide/svelte/icons/truck';
  import Hammer from '@lucide/svelte/icons/hammer';
  import Wrench from '@lucide/svelte/icons/wrench';
  import FileText from '@lucide/svelte/icons/file-text';

  /** spaceId; item: ItemResponse; type: its ItemTypeResponse or null; gone: disposed or archived; onchanged(). */
  let { spaceId, item, type, gone, onchanged } = $props();

  /** 'lend', 'reserve', 'destination' or null. */
  let dialog = $state(null);
  let projects = $state([]);
  let destination = $state(null);

  const editing = $derived(canEdit() && !gone);
  const base = $derived(`/api/spaces/${spaceId}/items/${item.id}`);
  const upkeep = $derived(has('reminders')
    ? (type?.fields ?? []).filter((field) => field.kind === 'Date' && field.repeatMonths && item.fields[field.id])
    : []);
  const overdue = (iso) => iso < todayIso();
  const showStock = $derived(has('stock') && item.quantity != null && (item.lowStockAt != null || item.reserved > 0));
  const showLending = $derived(has('lending') && (item.lentTo || editing));
  const showMoving = $derived(has('moving') && item.holdsItems && (item.destinationId || editing));
  const showProjects = $derived(has('projects') && (projects.length > 0 || editing));
  const any = $derived(showStock || showLending || upkeep.length > 0 || showMoving || showProjects);

  // What projects set aside, and the destination's name, load with the item.
  $effect(() => {
    const id = item.id;
    const going = item.destinationId;
    const wanted = has('projects');
    untrack(() => run(async () => {
      projects = wanted ? await api('GET', `/api/spaces/${spaceId}/items/${id}/projects`) : [];
      destination = going ? (await api('GET', `/api/spaces/${spaceId}/items/${going}`)).item : null;
    }));
  });

  const giveBack = () => run(async () => {
    await api('POST', `${base}/return`);
    toast(`${item.name} is back.`);
    onchanged();
  });

  const markDone = (field) => run(async () => {
    await api('POST', `${base}/upkeep-done`, { fieldId: field.id });
    toast(`${field.label} moved on ${field.repeatMonths} months from today.`);
    onchanged();
  });

  const arrived = () => run(async () => {
    await api('POST', `${base}/arrived`);
    toast(`${item.name} is in ${destination?.name ?? 'its destination'} now.`);
    onchanged();
  });

  async function setDestination(here) {
    await api('PUT', `${base}/destination`, { destinationId: here?.id ?? null });
    toast(here ? `${item.name} is going to ${here.name}.` : `${item.name} has no destination now.`);
    dialog = null;
    onchanged();
  }

  const useUp = (project, entry) => run(async () => {
    await api('POST', `/api/spaces/${spaceId}/projects/${project.id}/use-up`, { itemId: item.id, amount: null });
    toast(`${project.name} used ${entry.amount} of ${item.name}.`);
    onchanged();
  });

  const release = (project) => run(async () => {
    await api('POST', `/api/spaces/${spaceId}/projects/${project.id}/reserve`, { itemId: item.id, amount: 0 });
    toast(`${item.name} is no longer set aside for ${project.name}.`);
    onchanged();
  });

  function closed() {
    dialog = null;
    onchanged();
  }
</script>

{#if any}
  <section class="card features" aria-label="Stock, loans, upkeep, moving and projects">
    {#if showStock}
      <div class="line">
        <span>
          {#if item.isLow}<span class="chip warn">Running low</span>{/if}
          {#if item.lowStockAt != null}Low stock at {amount({ quantity: item.lowStockAt, unit: item.unit })}.{/if}
          {#if item.reserved > 0}{amount({ quantity: item.reserved, unit: item.unit })} set aside for projects.{/if}
        </span>
      </div>
    {/if}

    {#if showLending}
      <div class="line">
        <span class="with-icon"><Handshake size={16} aria-hidden="true" />
          {#if item.lentTo}
            <span>Lent to <strong>{item.lentTo}</strong>{#if item.dueOn}, due back {display({ kind: 'Date' }, item.dueOn)}{/if}.
              {#if item.dueOn && overdue(item.dueOn)}<span class="chip warn">Overdue</span>{/if}</span>
          {:else}
            <span class="muted">Not lent out.</span>
          {/if}
        </span>
        {#if editing}
          {#if item.lentTo}
            <button type="button" class="small" onclick={pendingClick(giveBack)}>It's back</button>
          {:else if item.status === 'Active'}
            <button type="button" class="small" onclick={() => { dialog = 'lend'; }}>Lend</button>
          {/if}
        {/if}
      </div>
    {/if}

    {#each upkeep as field (field.id)}
      <div class="line">
        <span class="with-icon"><Wrench size={16} aria-hidden="true" />
          <span>{field.label}: {display(field, item.fields[field.id])}
            {#if overdue(item.fields[field.id])}<span class="chip warn">Overdue</span>{/if}</span>
        </span>
        {#if editing}
          <button type="button" class="small" onclick={pendingClick(() => markDone(field))}>Done today</button>
        {/if}
      </div>
    {/each}

    {#if showMoving}
      <div class="line">
        <span class="with-icon"><Truck size={16} aria-hidden="true" />
          {#if item.destinationId}<span>Going to <a href="/browse/{item.destinationId}">{destination?.name ?? '…'}</a>.</span>
          {:else}<span class="muted">No destination for a move.</span>{/if}
        </span>
        <span class="actions">
          {#if item.destinationId}
            <a class="button small with-icon" href="/views/manifest?box={item.id}"><FileText size={14} aria-hidden="true" />Manifest</a>
          {/if}
          {#if editing}
            <button type="button" class="small" onclick={() => { dialog = 'destination'; }}>{item.destinationId ? 'Change' : 'Say where it’s going'}</button>
            {#if item.destinationId}<button type="button" class="small primary" onclick={pendingClick(arrived)}>Arrived</button>{/if}
          {/if}
        </span>
      </div>
    {/if}

    {#if showProjects}
      {#each projects as project (project.id)}
        {@const entry = project.items.find((candidate) => candidate.item.id === item.id)}
        <div class="line">
          <span class="with-icon"><Hammer size={16} aria-hidden="true" />
            <span>{item.quantity != null ? `${amount({ quantity: entry?.amount, unit: item.unit })} set aside` : 'Set aside'} for <a href="/views/projects">{project.name}</a>.</span>
          </span>
          {#if editing && entry}
            <span class="actions">
              <button type="button" class="small" onclick={pendingClick(() => useUp(project, entry))}>Used up</button>
              <button type="button" class="small" onclick={pendingClick(() => release(project))}>Release</button>
            </span>
          {/if}
        </div>
      {/each}
      {#if editing && item.status === 'Active'}
        <div class="line"><span></span><button type="button" class="small with-icon" onclick={() => { dialog = 'reserve'; }}><Hammer size={14} aria-hidden="true" />Set aside for a project</button></div>
      {/if}
    {/if}
  </section>
{/if}

{#if dialog === 'lend'}
  <LendDialog {spaceId} {item} onlent={closed} onclose={() => { dialog = null; }} />
{:else if dialog === 'reserve'}
  <ReserveDialog {spaceId} {item} onreserved={closed} onclose={() => { dialog = null; }} />
{:else if dialog === 'destination'}
  <PlacePicker {spaceId} title="Where {item.name} is going" exclude={item.id}
    action={(here) => (here ? { text: `Going to ${here.name}`, disabled: here.id === item.destinationId } : { text: 'Clear the destination', disabled: !item.destinationId })}
    onpick={setDestination} onclose={() => { dialog = null; }} />
{/if}

<style>
  .features { display: grid; gap: .5rem; }
  .line { display: flex; flex-wrap: wrap; justify-content: space-between; align-items: center; gap: .4rem .75rem; }
  .line .with-icon { align-items: flex-start; }
  .line :global(svg) { flex: none; margin-top: .15rem; }
</style>
