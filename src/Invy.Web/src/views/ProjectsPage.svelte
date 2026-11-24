<!-- Projects (/views/projects): each open project with what it sets aside, to use up or release, and finished ones on
     request. Things are set aside from their own page. Editors start, finish and remove projects. -->
<script>
  import { untrack } from 'svelte';
  import { app, run, ask, toast, canEdit, onLive } from '../lib/app.svelte.js';
  import { api } from '../lib/api.js';
  import { pending, pendingClick } from '../lib/pending.js';
  import { amount, label } from '../lib/items.js';
  import { has } from '../lib/features.js';
  import ArrowLeft from '@lucide/svelte/icons/arrow-left';
  import Hammer from '@lucide/svelte/icons/hammer';

  let projects = $state(null);
  let showDone = $state(false);
  let name = $state('');

  const base = $derived(`/api/spaces/${app.spaceId}/projects`);
  const load = () => run(async () => { projects = await api('GET', `${base}${showDone ? '?done=true' : ''}`); });

  $effect(() => {
    void app.spaceId;
    void showDone;
    untrack(() => load());
  });
  $effect(() => onLive('items', () => load()));

  function start(event) {
    event.preventDefault();
    return pending(event, () => run(async () => {
      const made = await api('POST', base, { name: name.trim(), notes: null, version: 0 });
      name = '';
      toast(`${made.name} started. Set things aside for it from their pages.`);
      await load();
    }));
  }

  const useUp = (project, entry) => run(async () => {
    await api('POST', `${base}/${project.id}/use-up`, { itemId: entry.item.id, amount: null });
    toast(`${project.name} used ${entry.item.name}.`);
    await load();
  });

  const release = (project, entry) => run(async () => {
    await api('POST', `${base}/${project.id}/reserve`, { itemId: entry.item.id, amount: 0 });
    await load();
  });

  const finish = (project) => run(async () => {
    await api('POST', `${base}/${project.id}/finish`);
    toast(`${project.name} is done. What it still set aside is free again.`);
    await load();
  });

  async function remove(project) {
    const sure = await ask({
      title: `Remove ${project.name}?`, message: 'What it set aside is free again. Things it used up stay used up.',
      confirmLabel: 'Remove project', danger: true,
    });
    if (!sure) return undefined;
    return run(async () => {
      await api('DELETE', `${base}/${project.id}`);
      await load();
    });
  }
</script>

<section class="projects">
  <a class="button back" href="/views"><ArrowLeft size={16} aria-hidden="true" />All views</a>
  <h2>Projects</h2>
  {#if !has('projects')}
    <p class="muted">Projects are switched off for this space. An owner can switch them on in Settings, This space.</p>
  {:else}
    <p class="muted">Set things aside for a project from their pages. Setting aside doesn't change an amount until it's used up.</p>
    {#if canEdit()}
      <form class="card start" onsubmit={start}>
        <label>New project <input bind:value={name} required maxlength="100" autocomplete="off" placeholder="Build a bookshelf" /></label>
        <button type="submit" class="primary">Start</button>
      </form>
    {/if}
    <label class="check"><input type="checkbox" bind:checked={showDone} /> Show finished projects</label>

    {#if projects === null}
      <div class="card"><span class="skeleton line"></span></div>
    {:else if projects.length === 0}
      <div class="card empty">
        <span class="empty-icon" style="--area: var(--area-views)" aria-hidden="true"><Hammer size={26} /></span>
        <h3>No projects yet</h3>
        <p class="muted">Start one, then set aside what it needs.</p>
      </div>
    {:else}
      {#each projects as project (project.id)}
        <article class="card project">
          <div class="row">
            <h3>{project.name} {#if project.status === 'Done'}<span class="chip good">Done</span>{/if}</h3>
            {#if canEdit()}
              <span class="actions">
                {#if project.status === 'Open'}<button type="button" class="small" onclick={pendingClick(() => finish(project))}>Finish</button>{/if}
                <button type="button" class="small" onclick={pendingClick(() => remove(project))}>Remove</button>
              </span>
            {/if}
          </div>
          {#if project.items.length}
            <ul class="plain entries" aria-label="Set aside for {project.name}">
              {#each project.items as entry (entry.item.id)}
                <li>
                  <span><a href="/browse/{entry.item.id}">{label(entry.item)}</a>
                    <span class="muted">{entry.item.quantity != null ? amount({ quantity: entry.amount, unit: entry.item.unit }) : 'whole'}</span></span>
                  {#if canEdit() && project.status === 'Open'}
                    <span class="actions">
                      <button type="button" class="small" onclick={pendingClick(() => useUp(project, entry))}>Used up</button>
                      <button type="button" class="small" onclick={pendingClick(() => release(project, entry))}>Release</button>
                    </span>
                  {/if}
                </li>
              {/each}
            </ul>
          {:else}
            <p class="muted">Nothing set aside{project.status === 'Open' ? ' yet' : ''}.</p>
          {/if}
        </article>
      {/each}
    {/if}
  {/if}
</section>

<style>
  .projects { margin-top: 1.25rem; }
  .start { display: flex; flex-wrap: wrap; gap: .5rem; align-items: end; }
  .start label { flex: 1 1 14rem; }
  .check { display: flex; gap: .5rem; align-items: center; margin: .75rem 0; color: var(--ink); font-size: .9rem; }
  .project h3 { margin: 0; }
  .entries li { display: flex; flex-wrap: wrap; justify-content: space-between; gap: .3rem .75rem; padding: .4rem 0; }
  .entries li + li { border-top: 1px solid var(--line); }
  .empty h3 { margin: .25rem 0; }
</style>
