// Each view has its own address, so it can be bookmarked, reloaded or opened in a new tab:
//   /find  /settings/{section}
// The address is the source of truth: links and buttons call go(path), and showAddress() shows what it names.
import { app, run, clearErrors } from './state.svelte.js';

const TAB_KEYS = ['find', 'settings'];

/** Moves to an address inside the page and shows it. */
export async function go(path, { replace = false } = {}) {
  const here = location.pathname + location.search;
  if (path !== here) {
    // Each new entry remembers the address it was opened from, so closeDialog() can step back to it.
    if (replace) history.replaceState(history.state, '', path);
    else history.pushState({ from: here }, '', path);
  }
  clearErrors();
  await run(showAddress);
}

/**
 * Closes a dialog that has its own address by moving to `base`, the address behind it. When the dialog was opened from
 * there, this goes back in history instead, so Back afterwards doesn't reopen it.
 */
export async function closeDialog(base) {
  if (history.state?.from === base) history.back();
  else await go(base, { replace: true });
}

/** Shows the view the address bar names, once signed in. Unknown addresses go to Find. */
export async function showAddress() {
  const [first = null, second = null] = location.pathname.split('/').filter(Boolean).map(decodeURIComponent);
  if (!TAB_KEYS.includes(first)) {
    history.replaceState(null, '', '/find');
    return showAddress();
  }
  app.tab = first;
  if (first === 'settings' && second) app.settingsSection = second;
  app.screen = 'app';
}

/**
 * Follows a click on a link to another view inside the page without reloading it. Returns true when it did;
 * ctrl-, shift- and middle-clicks, downloads, API links and the /docs page are left to the browser so they open a new tab.
 */
export function followLink(event) {
  const link = event.target.closest?.('a[href]');
  if (!link || event.defaultPrevented || event.button !== 0 || event.metaKey || event.ctrlKey || event.shiftKey || event.altKey) return false;
  const url = new URL(link.href, location.href);
  if (url.origin !== location.origin || link.target || link.hasAttribute('download')
      || /^\/(api|openapi|docs|health|help|mcp)(\/|$)/.test(url.pathname)) return false;
  event.preventDefault();
  go(url.pathname + url.search);
  return true;
}

/** Follows in-page links and the back and forward buttons, once someone is signed in. Called once at start. */
export function listenForAddresses() {
  addEventListener('popstate', () => { if (app.session?.user) go(location.pathname + location.search, { replace: true }); });
  document.addEventListener('click', (event) => { if (app.session?.user) followLink(event); });
}
