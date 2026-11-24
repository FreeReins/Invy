<!-- Pinned views on Find (docs/developers/design.md, Views): each pinned view's count and first few items (or latest
     events, for a timeline), loaded again whenever the items live topic says something changed, so a "not seen in a
     year" view drops a thing as soon as it is scanned. A pinned shared view shows on everyone's Find. -->
<script>
  import { untrack } from 'svelte';
  import { app, onLive } from '../lib/app.svelte.js';
  import { api } from '../lib/api.js';
  import { label } from '../lib/items.js';
  import { plural } from '../lib/format.js';
  import EventLine from '../items/EventLine.svelte';
  import Pin from '@lucide/svelte/icons/pin';

  const SHOWN = 5;

  // { view, page, problem } per pinned view, or null until loaded.
  let pinned = $state(null);
  let asked = 0;

  /** Loads the pinned views and their first pages. Failures stay quiet: Find's search still works without them. */
  async function load() {
    const id = app.spaceId;
    if (!id) return;
    const mine = ++asked;
    try {
      const views = (await api('GET', `/api/spaces/${id}/views`)).filter((view) => view.pinned);
      const loaded = await Promise.all(views.map(async (view) => {
        try {
          const part = view.display === 'Timeline' ? 'timeline' : 'results';
          return { view, page: await api('GET', `/api/spaces/${id}/views/${view.id}/${part}`), problem: '' };
        } catch (error) {
          return { view, page: null, problem: error.message };
        }
      }));
      if (mine === asked) pinned = loaded;
    } catch {
      if (mine === asked) pinned = [];
    }
  }

  $effect(() => {
    void app.spaceId;
    untrack(() => { pinned = null; load(); });
  });
  $effect(() => onLive('items', () => load()));

  const count = (page) => `${page.totalIsEstimate ? 'About ' : ''}${plural(page.total ?? page.results.length, 'item')}`;
</script>

{#if pinned?.length}
  <section class="pinned" aria-label="Pinned views">
    {#each pinned as { view, page, problem } (view.id)}
      <article class="card pin" aria-labelledby="pin-{view.id}">
        <header class="row">
          <h2 id="pin-{view.id}" class="with-icon"><Pin size={15} aria-hidden="true" /><a href="/views/{view.id}">{view.name}</a></h2>
          {#if page?.results}<span class="muted" aria-live="polite">{count(page)}</span>{/if}
        </header>
        {#if problem}
          <p class="bad-text">{problem}</p>
        {:else if page.events}
          {#if page.events.length === 0}<p class="muted">Nothing has happened yet.</p>{/if}
          <ul class="plain">
            {#each page.events.slice(0, SHOWN) as event (event.id)}<li><EventLine {event} /></li>{/each}
          </ul>
        {:else if page.results.length === 0}
          <p class="muted">Nothing right now.</p>
        {:else}
          <ul class="plain">
            {#each page.results.slice(0, SHOWN) as result (result.item.id)}
              <li><a href="/browse/{result.item.id}" class="clip">{label(result.item)}</a></li>
            {/each}
          </ul>
          {#if (page.total ?? page.results.length) > SHOWN}<a href="/views/{view.id}" class="all">See all {view.name}</a>{/if}
        {/if}
      </article>
    {/each}
  </section>
{/if}

<style>
  .pinned { display: grid; gap: .75rem; grid-template-columns: repeat(auto-fit, minmax(min(16rem, 100%), 1fr)); margin-bottom: 1rem; }
  .pin { margin: 0; }
  header { align-items: center; }
  h2 { margin: 0; font-size: 1rem; gap: .35rem; min-width: 0; }
  h2 :global(svg) { color: var(--area-views); }
  h2 a { color: var(--ink); overflow-wrap: anywhere; }
  ul { margin-top: .3rem; }
  li { padding: .25rem 0; }
  .all { display: inline-block; margin-top: .3rem; font-size: .9rem; }
</style>
