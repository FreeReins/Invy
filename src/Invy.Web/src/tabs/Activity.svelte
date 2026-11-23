<!-- The Activity tab: everything that happened in the space, newest first, 50 at a time. -->
<script>
  import { untrack } from 'svelte';
  import { app, run, currentSpace } from '../lib/app.svelte.js';
  import { api } from '../lib/api.js';
  import NoSpace from '../components/NoSpace.svelte';
  import EventLine from '../items/EventLine.svelte';
  import ActivityIcon from '@lucide/svelte/icons/activity';

  let feed = $state(null);
  const space = $derived(currentSpace());

  // Loads again whenever another space is picked, and only then: run() reads app.busy, which mustn't count.
  $effect(() => {
    const id = app.spaceId;
    untrack(() => {
      feed = null;
      if (id) run(async () => { feed = await api('GET', `/api/spaces/${id}/activity`); });
    });
  });

  const more = () => run(async () => {
    const page = await api('GET', `/api/spaces/${app.spaceId}/activity?before=${encodeURIComponent(feed.next)}`);
    feed = { events: [...feed.events, ...page.events], next: page.next };
  });
</script>

{#if !space}
  <NoSpace area="var(--area-activity)" />
{:else}
  <section class="activity">
    <h2>Activity</h2>
    <p class="muted">What happened in {space.name}, newest first.</p>
    {#if feed === null}
      <div class="card"><span class="skeleton line"></span><span class="skeleton line"></span></div>
    {:else if feed.events.length === 0}
      <div class="card empty">
        <span class="empty-icon" style="--area: var(--area-activity)" aria-hidden="true"><ActivityIcon size={26} /></span>
        <h3>Nothing yet</h3>
        <p class="muted">Adding, moving and changing items shows up here.</p>
      </div>
    {:else}
      <div class="card">
        <ul class="plain" aria-label="Activity in {space.name}">
          {#each feed.events as event (event.id)}<li><EventLine {event} /></li>{/each}
        </ul>
      </div>
      {#if feed.next}<button type="button" onclick={more}>Show older</button>{/if}
    {/if}
  </section>
{/if}

<style>
  .activity { margin-top: 1.25rem; }
  h2 { margin: 0 0 .2rem; }
  .empty h3 { margin: .25rem 0; }
  li { padding: .45rem 0; }
</style>
