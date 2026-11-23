<!-- The People tab: who is in the space the page shows, and their roles (docs/developers/design.md, Spaces and roles).
     Owners add people who have an account on this install, change roles and remove people; anyone can leave. Owners
     also rename or delete the space here. Install admins invite accounts in Settings; that gives no access to a space. -->
<script>
  import { untrack } from 'svelte';
  import { app, run, ask, toast, currentSpace, isOwner, replaceSpace, dropSpace } from '../lib/app.svelte.js';
  import { api } from '../lib/api.js';
  import { pending, pendingClick } from '../lib/pending.js';
  import NoSpace from '../components/NoSpace.svelte';
  import HelpLink from '../components/HelpLink.svelte';
  import UserPlus from '@lucide/svelte/icons/user-plus';

  const ROLES = [
    { role: 'Owner', about: 'everything, including people and deleting the space' },
    { role: 'Editor', about: 'add, change, move and remove items and types' },
    { role: 'Viewer', about: 'search and look' },
  ];

  let members = $state(null);
  let username = $state('');
  let role = $state('Editor');
  let newName = $state('');

  const space = $derived(currentSpace());
  const me = $derived(app.session?.user?.id);
  const path = $derived(`/api/spaces/${app.spaceId}`);

  // Loads again whenever another space is picked, and only then: run() reads app.busy, which mustn't count.
  $effect(() => {
    const id = app.spaceId;
    untrack(() => {
      members = null;
      newName = currentSpace()?.name ?? '';
      if (id) run(async () => { members = await api('GET', `/api/spaces/${id}/members`); });
    });
  });

  function add(event) {
    event.preventDefault();
    return pending(event, () => run(async () => {
      members = await api('POST', `${path}/members`, { username: username.trim(), role });
      toast(`${username.trim()} can now open ${space.name}.`);
      username = '';
    }));
  }

  const setRole = (member, value) => run(async () => {
    members = await api('PUT', `${path}/members/${member.userId}`, { role: value });
    toast(`${member.displayName} is now ${value === 'Owner' ? 'an owner' : `a ${value.toLowerCase()}`} of ${space.name}.`);
    if (member.userId === me) replaceSpace({ ...space, role: value });
  }).finally(() => {
    // A refused change puts the menu back to the role the member still has.
    members = members?.map((item) => ({ ...item })) ?? null;
  });

  const remove = (member) => run(async () => {
    const leaving = member.userId === me;
    const confirmed = await ask({
      title: leaving ? `Leave ${space.name}?` : `Remove ${member.displayName}?`,
      message: leaving
        ? `You won't see ${space.name} until an owner adds you again.`
        : `${member.displayName} won't see ${space.name} any more. Nothing in it changes.`,
      confirmLabel: leaving ? 'Leave' : 'Remove', danger: true,
    });
    if (!confirmed) return;
    const name = space.name;
    const left = await api('DELETE', `${path}/members/${member.userId}`);
    if (leaving) {
      dropSpace(space.id);
      toast(`You left ${name}.`);
    } else {
      members = left;
      toast(`${member.displayName} removed from ${name}.`);
    }
  });

  function rename(event) {
    event.preventDefault();
    return pending(event, () => run(async () => {
      const renamed = await api('PUT', path, { name: newName.trim() });
      replaceSpace(renamed);
      toast(`Renamed to ${renamed.name}.`);
    }));
  }

  const deleteSpace = () => run(async () => {
    const confirmed = await ask({
      title: `Delete ${space.name}?`,
      message: `Everything in ${space.name} is deleted for everyone in it: its items, types and history. This can't be undone, except by restoring a backup.`,
      confirmLabel: 'Delete space', danger: true,
    });
    if (!confirmed) return;
    const name = space.name;
    await api('DELETE', path);
    dropSpace(space.id);
    toast(`${name} deleted.`);
  });
</script>

{#if !space}
  <NoSpace area="var(--area-people)" />
{:else}
  <section class="people">
    <h2>People</h2>
    <p class="muted">Who can open {space.name}. Owners can do {ROLES[0].about}; editors {ROLES[1].about}; viewers {ROLES[2].about}.</p>

    <div class="card">
      {#if members === null}
        <span class="skeleton line"></span><span class="skeleton line"></span>
      {:else}
        <ul class="plain" aria-label="People in {space.name}">
          {#each members as member (member.userId)}
            <li class="row member">
              <span class="who">
                <span><strong>{member.displayName}</strong>{#if member.userId === me}<span class="muted">{' (you)'}</span>{/if}</span>
                <span class="muted">{member.username}</span>
              </span>
              <span class="row-actions">
                {#if isOwner()}
                  <label class="sr-only" for="role-{member.userId}">Role of {member.displayName}</label>
                  <select id="role-{member.userId}" value={member.role} onchange={(event) => setRole(member, event.currentTarget.value)}>
                    {#each ROLES as option (option.role)}<option value={option.role}>{option.role}</option>{/each}
                  </select>
                {:else}
                  <span class="chip">{member.role}</span>
                {/if}
                {#if isOwner() || member.userId === me}
                  <button type="button" class="small" onclick={pendingClick(() => remove(member))}
                    aria-label={member.userId === me ? `Leave ${space.name}` : `Remove ${member.displayName}`}>
                    {member.userId === me ? 'Leave' : 'Remove'}
                  </button>
                {/if}
              </span>
            </li>
          {/each}
        </ul>
      {/if}
    </div>

    {#if isOwner()}
      <h3>Add someone</h3>
      <form class="inline" onsubmit={add}>
        <label>Username <input bind:value={username} required autocomplete="off" autocapitalize="none" spellcheck="false" /></label>
        <label>Role
          <select bind:value={role}>
            {#each ROLES as option (option.role)}<option value={option.role}>{option.role}</option>{/each}
          </select>
        </label>
        <button type="submit" class="primary with-icon"><UserPlus size={16} aria-hidden="true" />Add</button>
      </form>
      <p class="muted hint">They need an account on this Invy first. An admin can make an invite link in Settings.</p>

      <h3>This space</h3>
      <form class="inline" onsubmit={rename}>
        <label>Name <input bind:value={newName} required maxlength="80" autocomplete="off" /></label>
        <button type="submit">Rename</button>
      </form>
      <div class="danger-zone">
        <button type="button" class="danger" onclick={pendingClick(deleteSpace)}>Delete {space.name}</button>
      </div>
    {/if}
    <p class="muted"><HelpLink page="using/spaces">Spaces, people and roles</HelpLink></p>
  </section>
{/if}

<style>
  .people { margin-top: 1.25rem; }
  h2 { margin: 0 0 .2rem; }
  h3 { margin: 1.25rem 0 .5rem; font-size: 1.05rem; }
  .member { align-items: center; }
  .who { display: grid; min-width: 0; overflow-wrap: anywhere; }
  .row-actions { align-items: center; }
  .hint { margin-top: .4rem; }
  .danger-zone { margin: .75rem 0 1rem; }
</style>
