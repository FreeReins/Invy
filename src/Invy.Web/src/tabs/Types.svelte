<!-- The Types tab: the space's item types and their custom fields (docs/developers/design.md, Field definitions). Any
     member can look; editors and owners add, change and remove types in TypeEditor.svelte. -->
<script>
  import { untrack } from 'svelte';
  import { app, run, currentSpace, canEdit } from '../lib/app.svelte.js';
  import { api } from '../lib/api.js';
  import NoSpace from '../components/NoSpace.svelte';
  import HelpLink from '../components/HelpLink.svelte';
  import TypeEditor from '../types/TypeEditor.svelte';
  import Shapes from '@lucide/svelte/icons/shapes';
  import Plus from '@lucide/svelte/icons/plus';

  let types = $state(null);
  /** The type being edited: an ItemTypeResponse, 'new', or null while the editor is closed. */
  let editing = $state(null);

  const space = $derived(currentSpace());

  // Loads again whenever another space is picked, and only then: run() reads app.busy, which mustn't count.
  $effect(() => {
    const id = app.spaceId;
    untrack(() => {
      types = null;
      if (id) run(async () => { types = await api('GET', `/api/spaces/${id}/types`); });
    });
  });

  function saved(type) {
    const others = (types ?? []).filter((item) => item.id !== type.id);
    types = [...others, type].sort((a, b) => a.name.localeCompare(b.name));
    editing = null;
  }

  function removed(id) {
    types = types.filter((item) => item.id !== id);
    editing = null;
  }
</script>

{#if !space}
  <NoSpace area="var(--area-types)" />
{:else}
  <section class="types">
    <div class="row head">
      <div>
        <h2>Types</h2>
        <p class="muted">The kinds of things in {space.name}, and the fields each one has.</p>
      </div>
      {#if canEdit()}
        <button type="button" class="primary with-icon" onclick={() => { editing = 'new'; }}><Plus size={16} aria-hidden="true" />New type</button>
      {/if}
    </div>

    {#if types === null}
      <div class="grid" aria-hidden="true">
        {#each [1, 2, 3] as n (n)}<div class="card"><span class="skeleton line"></span><span class="skeleton line"></span></div>{/each}
      </div>
    {:else if types.length === 0}
      <div class="card empty">
        <span class="empty-icon" style="--area: var(--area-types)" aria-hidden="true"><Shapes size={26} /></span>
        <h3>No types yet</h3>
        <p class="muted">A type says what fields a kind of thing has, such as a Tool with a brand and a price.
          {canEdit() ? 'Add one to get started.' : 'An editor of this space can add them.'}</p>
      </div>
    {:else}
      <ul class="grid plain-grid" aria-label="Types in {space.name}">
        {#each types as type (type.id)}
          <li class="card type" style="--swatch: {type.colour ?? 'var(--area-types)'}">
            <div class="row">
              <h3><span class="swatch" aria-hidden="true"></span>{type.name}</h3>
              <button type="button" class="small" onclick={() => { editing = type; }}
                aria-label="{canEdit() ? 'Edit' : 'View'} {type.name}">{canEdit() ? 'Edit' : 'View'}</button>
            </div>
            <div class="chips">
              {#if type.holdsItems}<span class="chip info">Holds items</span>{/if}
              <span class="chip">{type.fields.length === 1 ? '1 field' : `${type.fields.length} fields`}</span>
            </div>
            {#if type.fields.length}
              <p class="muted fields">{type.fields.map((field) => field.label).join(', ')}</p>
            {/if}
          </li>
        {/each}
      </ul>
    {/if}
    <p class="muted"><HelpLink page="using/types-and-fields">Types and fields</HelpLink></p>
  </section>
{/if}

{#if editing}
  <TypeEditor spaceId={app.spaceId} type={editing === 'new' ? null : editing} readonly={!canEdit()}
    onsaved={saved} onremoved={removed} onclose={() => { editing = null; }} />
{/if}

<style>
  .types { margin-top: 1.25rem; }
  .head { align-items: center; margin-bottom: .5rem; }
  h2 { margin: 0 0 .2rem; }
  .plain-grid { list-style: none; margin: 0 0 1rem; padding: 0; }
  .type { margin: 0; }
  .type .row { flex-wrap: nowrap; align-items: center; }
  .type h3 { display: flex; align-items: center; gap: .5rem; margin: 0; font-size: 1.05rem; min-width: 0; overflow-wrap: anywhere; }
  .swatch { flex: none; width: .8rem; height: .8rem; border-radius: 50%; background: var(--swatch); }
  .fields { margin-top: .4rem; }
</style>
