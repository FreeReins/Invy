<!-- Invy's page: first-run setup, sign-in, then the space picker and the tabs (Find, Browse, Scan, Activity, Types, People and Settings for now; the rest come with their phases in
     docs/roadmap/). State and actions live in lib/app.svelte.js. -->
<script>
  import { app, run, start, signOut } from './lib/app.svelte.js';
  import SetupAccount from './screens/SetupAccount.svelte';
  import SetupHttps from './screens/SetupHttps.svelte';
  import SignIn from './screens/SignIn.svelte';
  import Join from './screens/Join.svelte';
  import Reset from './screens/Reset.svelte';
  import Find from './tabs/Find.svelte';
  import Settings from './tabs/Settings.svelte';
  import Types from './tabs/Types.svelte';
  import Browse from './tabs/Browse.svelte';
  import Activity from './tabs/Activity.svelte';
  import Scan from './tabs/Scan.svelte';
  import People from './tabs/People.svelte';
  import SpacePicker from './components/SpacePicker.svelte';
  import Toasts from './components/Toasts.svelte';
  import Inbox from './components/Inbox.svelte';
  import ConfirmDialog from './components/ConfirmDialog.svelte';
  import ThemeToggle from './components/ThemeToggle.svelte';
  import SetupProgress from './components/SetupProgress.svelte';
  import { stepIn, stepOut, rise } from './lib/motion.js';
  import Boxes from '@lucide/svelte/icons/boxes';
  import Search from '@lucide/svelte/icons/search';
  import SettingsIcon from '@lucide/svelte/icons/settings';
  import Shapes from '@lucide/svelte/icons/shapes';
  import FolderTree from '@lucide/svelte/icons/folder-tree';
  import ActivityIcon from '@lucide/svelte/icons/activity';
  import Users from '@lucide/svelte/icons/users';
  import ScanLine from '@lucide/svelte/icons/scan-line';
  import CircleHelp from '@lucide/svelte/icons/circle-help';
  import LogOut from '@lucide/svelte/icons/log-out';
  import Sprout from '@lucide/svelte/icons/sprout';

  const TABS = [
    { key: 'find', label: 'Find', icon: Search },
    { key: 'browse', label: 'Browse', icon: FolderTree },
    { key: 'scan', label: 'Scan', icon: ScanLine },
    { key: 'activity', label: 'Activity', icon: ActivityIcon },
    { key: 'types', label: 'Types', icon: Shapes },
    { key: 'people', label: 'People', icon: Users },
    { key: 'settings', label: 'Settings', icon: SettingsIcon },
  ].map((tab) => ({ ...tab, area: `var(--area-${tab.key})` }));
  const GATES = { 'setup-account': SetupAccount, 'setup-https': SetupHttps, 'sign-in': SignIn, join: Join, reset: Reset };
  const TAB_VIEWS = { find: Find, browse: Browse, scan: Scan, activity: Activity, types: Types, people: People, settings: Settings };

  const inApp = $derived(app.screen === 'app');
  const Gate = $derived(GATES[app.screen]);
  const View = $derived(TAB_VIEWS[app.tab]);
  // The setup steps share one progress bar, which stays put while the steps slide beneath it.
  const inSetup = $derived(app.screen.startsWith('setup-'));

  // When a new screen arrives after another (a setup step, or sign-in after a link), the page goes back to its top and
  // focus moves to the screen's heading, so keyboard and screen reader users start where everyone else is looking.
  let firstScreen = true;
  function arrive(node) {
    if (firstScreen) { firstScreen = false; return; }
    window.scrollTo({ top: 0 });
    const heading = node.querySelector('h2');
    if (!heading) return;
    heading.tabIndex = -1;
    heading.focus({ preventScroll: true });
  }

  // The browser tab's title names the view, so bookmarks and history read well.
  const title = $derived(inApp ? TABS.find((tab) => tab.key === app.tab)?.label : null);

  run(start);
</script>

<svelte:head><title>{title ? `${title} · Invy` : 'Invy'}</title></svelte:head>

<header class="top">
  <div class="brand">
    <span class="mark" aria-hidden="true"><Boxes size={22} /></span>
    <h1>Invy</h1>
    <p id="tagline" class="muted">Everything you own, and where it is</p>
  </div>
  <div class="actions account">
    {#if inApp}<Inbox />{/if}
    <!-- Help is on every screen, sign-in and setup included: the documentation is built in and needs no sign-in. -->
    <a class="icon help" href="/help/" target="_blank" rel="noopener" aria-label="Help: open the Invy guide"
       title="Help"><CircleHelp size={20} /></a>
    <ThemeToggle />
    {#if inApp}
      <span class="muted name">{app.session.user.displayName}</span>
      <button class="with-icon sign-out" onclick={signOut} title="Sign out"><LogOut size={16} aria-hidden="true" /><span class="sign-out-label">Sign out</span></button>
    {/if}
  </div>
  <!-- Shown while something takes a moment to save or load, so a slow click doesn't look ignored. Fixed to the top of
       the window; it sits last in the header (whose first div is the title) so it is inside a landmark. -->
  {#if app.busy}<div class="busy-bar" role="progressbar" aria-label="Working"></div>{/if}
</header>

<Toasts />
<ConfirmDialog />

{#if inApp}
  <SpacePicker />
  <nav class="tabs" aria-label="Views">
    {#each TABS as tab (tab.key)}
      <a href="/{tab.key}" style="--area: {tab.area}" aria-current={app.tab === tab.key ? 'page' : undefined}><tab.icon size={18} /><span class="tab-label">{tab.label}</span></a>
    {/each}
  </nav>
{/if}

<main id="view">
  {#if app.screen === 'loading'}
    <p class="loading muted" role="status" aria-label="Loading">
      <span class="loading-mark" aria-hidden="true"><Boxes size={30} /></span>
      <span class="warming" aria-hidden="true">Opening the boxes…</span>
    </p>
  {:else if !inApp}
    {#if inSetup}<div class="gate wide setup-head"><SetupProgress /></div>{/if}
    <!-- One cell, so the step leaving and the step arriving overlap while they slide instead of stacking. -->
    <div class="stage" class:in-setup={inSetup}>
      {#key app.screen}
        <div class="scene" use:arrive in:stepIn={{ direction: app.setupDirection }} out:stepOut={{ direction: app.setupDirection }}>
          <Gate />
        </div>
      {/key}
    </div>
  {:else}
    {#key app.tab}
      <div in:rise={{ duration: 160 }}>
        <View />
      </div>
    {/key}
  {/if}
</main>

<!-- Pinned to the bottom of the window (at the end of the page on phones). Add a link by adding an <li>; the | between
     items comes from the CSS. Each link has an icon and a short name, so none can be mistaken for another. The GitHub
     mark is inline SVG from GitHub's Octicons (MIT licence), since Lucide has no brand logos; the rest come from
     Lucide like the other icons on the page. The API reference for developers is linked from Settings, AI assistants. -->
<footer class="site-footer">
  <ul class="footer-links">
    <li>
      <a href="/help/" target="_blank" rel="noopener"><CircleHelp size={16} /><span>Help</span></a>
    </li>
    <li>
      <a href="https://github.com/FreeReins/invy" target="_blank" rel="noopener">
        <svg viewBox="0 0 16 16" width="16" height="16" aria-hidden="true" focusable="false"><path fill="currentColor" d="M8 0c4.42 0 8 3.58 8 8a8.013 8.013 0 0 1-5.45 7.59c-.4.08-.55-.17-.55-.38 0-.27.01-1.13.01-2.2 0-.75-.25-1.23-.54-1.48 1.78-.2 3.65-.88 3.65-3.95 0-.88-.31-1.59-.82-2.15.08-.2.36-1.02-.08-2.12 0 0-.67-.22-2.2.82-.64-.18-1.32-.27-2-.27-.68 0-1.36.09-2 .27-1.53-1.03-2.2-.82-2.2-.82-.44 1.1-.16 1.92-.08 2.12-.51.56-.82 1.28-.82 2.15 0 3.06 1.86 3.75 3.64 3.95-.23.2-.44.55-.51 1.07-.46.21-1.61.55-2.33-.66-.15-.24-.6-.83-1.23-.82-.67.01-.27.38.01.53.34.19.73.9.82 1.13.16.45.68 1.31 2.69.94 0 .67.01 1.3.01 1.49 0 .21-.15.45-.55.38A7.995 7.995 0 0 1 0 8c0-4.42 3.58-8 8-8Z"/></svg><span>GitHub</span>
      </a>
    </li>
    <li>
      <a href="https://buymeacoffee.com/strifer.btw" target="_blank" rel="noopener"><Sprout size={16} /><span>Support Invy</span></a>
    </li>
  </ul>
</footer>

<style>
  .top { display: flex; flex-wrap: wrap; gap: 1rem; justify-content: space-between; align-items: flex-start; }
  .top > div:first-child { margin-right: auto; }
  /* The logo mark and the name share the first line; the tagline is under the name. */
  .brand { display: grid; grid-template-columns: auto minmax(0, 1fr); column-gap: .6rem; align-items: center; }
  .brand #tagline { grid-column: 2; }
  .mark { grid-row: 1 / span 2; display: inline-grid; place-items: center; width: 2.6rem; height: 2.6rem; border-radius: .7rem;
    color: var(--accent-2); background: color-mix(in srgb, var(--accent-2) 14%, var(--panel)); }
  .account { align-items: center; }
  /* Sits beside the theme toggle and looks like it. */
  a.help { display: inline-grid; place-items: center; width: 2.4rem; height: 2.4rem; padding: 0; border-radius: 50%;
    color: var(--muted); }
  a.help:hover { color: var(--ink); background: var(--chip); }
  .name { align-self: center; }
  .busy-bar { position: fixed; inset: 0 0 auto 0; z-index: 30; height: 3px; overflow: hidden; pointer-events: none;
    background: color-mix(in srgb, var(--accent) 25%, transparent); animation: appear .3s step-end; }
  .busy-bar::after { content: ""; position: absolute; inset: 0 auto 0 0; width: 35%; background: linear-gradient(90deg, var(--accent), var(--accent-2));
    animation: slide 1.1s ease-in-out infinite; }
  /* Hidden for its first moments, so quick actions don't flicker it. */
  @keyframes appear { from { visibility: hidden; } }
  @keyframes slide { from { left: -35%; } to { left: 100%; } }
  @media (prefers-reduced-motion: reduce) { .busy-bar::after { animation: none; width: 100%; } }
  .tabs { display: flex; gap: .25rem; margin: 1.25rem 0 0; border-bottom: 1px solid var(--line); overflow-x: auto; }
  .tabs a { display: inline-flex; align-items: center; gap: .45rem; border-bottom: 3px solid transparent; color: var(--muted);
    padding: .6rem .9rem; text-decoration: none; white-space: nowrap; }
  .tabs a :global(svg) { flex: none; color: var(--area); }
  .tabs a:hover { color: var(--ink); background: color-mix(in srgb, var(--area) 8%, transparent); }
  .tabs a:focus-visible { outline: 2px solid var(--accent); outline-offset: -2px; }
  .tabs a[aria-current="page"] { color: var(--ink); border-bottom-color: var(--area); font-weight: 600; }
  /* Pinned to the bottom of the window; the body's bottom padding in styles.css keeps content clear of it. */
  .site-footer { position: fixed; inset: auto 0 0 0; z-index: 10; background: var(--bg); border-top: 1px solid var(--line);
    padding: .45rem 1rem calc(.45rem + env(safe-area-inset-bottom)); }
  .footer-links { list-style: none; margin: 0 auto; padding: 0; max-width: 62rem; display: flex; justify-content: center;
    align-items: center; flex-wrap: wrap; }
  .footer-links li { display: flex; align-items: center; }
  .footer-links li + li:not([hidden])::before { content: "|"; color: var(--line); padding: 0 .6rem; }
  .footer-links a { display: inline-flex; align-items: center; gap: .35rem; padding: .25rem .4rem; border-radius: .4rem;
    color: var(--muted); font-size: .875rem; text-decoration: none; }
  .footer-links a:hover { color: var(--ink); background: var(--chip); }
  .footer-links a:focus-visible { outline: 2px solid var(--accent); outline-offset: 2px; }
  /* The first load: the logo mark bobs like a lid being lifted, and a line appears if it takes a moment. */
  .loading { display: grid; justify-items: center; gap: .75rem; padding: 3rem 0; }
  .loading-mark { display: inline-grid; place-items: center; width: 3.5rem; height: 3.5rem; border-radius: 1rem;
    color: var(--accent-2); background: color-mix(in srgb, var(--accent-2) 14%, var(--panel)); }
  .warming { animation: appear .6s step-end; }
  @media (prefers-reduced-motion: no-preference) {
    .loading-mark :global(svg) { animation: simmer 1.1s ease-in-out infinite; transform-origin: 50% 90%; }
  }
  @keyframes simmer { 0%, 100% { transform: translateY(0) rotate(0); } 30% { transform: translateY(-4px) rotate(-6deg); } 60% { transform: translateY(-2px) rotate(4deg); } }
  .stage { display: grid; }
  .scene { grid-area: 1 / 1; min-width: 0; }
  /* The progress bar above already makes the gap at the top of the step. */
  .setup-head { margin-bottom: 0; }
  .stage.in-setup :global(.gate) { margin-top: 0; }
  .scene :global(h2[tabindex="-1"]:focus:not(:focus-visible)) { outline: none; }

  /* Tablets and narrow windows: the tabs share the width evenly, label under icon, so the row never scrolls. */
  @media (max-width: 56rem) {
    .tabs { gap: 0; overflow: visible; }
    .tabs a { flex: 1 1 0; flex-direction: column; justify-content: center; gap: .15rem; min-width: 0;
      padding: .5rem .2rem; font-size: .8rem; }
    .tab-label { max-width: 100%; overflow: hidden; text-overflow: ellipsis; }
  }

  /* Phones: the logo, the name and the account buttons share the top line and the tagline gets the whole line under
     them, so nothing overlaps. Sign out shows as an icon (its name is still read out). The tabs
     become a bar pinned to the bottom of the window, and the footer links sit at the end of the page instead. */
  @media (max-width: 40rem) {
    .top { display: grid; grid-template-columns: auto minmax(0, 1fr) auto; grid-template-areas: "mark name account" "tagline tagline tagline";
      column-gap: .6rem; row-gap: .45rem; align-items: center; }
    .brand { display: contents; }
    .mark { grid-area: mark; grid-row: auto; width: 2.2rem; height: 2.2rem; }
    .brand h1 { grid-area: name; white-space: nowrap; overflow: hidden; text-overflow: ellipsis; }
    .brand #tagline { grid-area: tagline; }
    .account { grid-area: account; flex-wrap: nowrap; gap: .2rem; }
    .account .name { display: none; }
    a.help { width: 2.1rem; height: 2.1rem; }
    .sign-out { padding: .4rem .55rem; }
    .sign-out-label { position: absolute; width: 1px; height: 1px; overflow: hidden; clip-path: inset(50%); white-space: nowrap; }
    .tabs { position: fixed; inset: auto 0 0 0; z-index: 10; display: grid; grid-auto-flow: column; grid-auto-columns: minmax(0, 1fr);
      gap: 0; margin: 0; overflow: visible; background: var(--bg); border-bottom: 0; border-top: 1px solid var(--line);
      padding-bottom: env(safe-area-inset-bottom); }
    .tabs a { flex-direction: column; gap: .15rem; min-width: 0; padding: .45rem .1rem .4rem; font-size: .7rem;
      border-bottom: 0; border-top: 3px solid transparent; }
    .tabs a[aria-current="page"] { border-top-color: var(--area); background: color-mix(in srgb, var(--area) 10%, var(--bg)); }
    .tab-label { max-width: 100%; overflow: hidden; text-overflow: ellipsis; }
    .site-footer { position: static; margin-top: 1.5rem; padding-bottom: .45rem; background: none; }
  }
  /* The narrowest phones: Help is left to the footer at the end of the page, so the account buttons fit beside the name. */
  @media (max-width: 24rem) {
    .top { column-gap: .45rem; }
    a.help { display: none; }
  }
</style>
