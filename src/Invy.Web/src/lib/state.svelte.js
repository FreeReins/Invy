// Everything the page shows (`app`), plus what every area's actions use: run(), toasts (and the inbox they feed) and the confirmation
// dialog. Components read `app` and import from app.svelte.js; Svelte redraws whatever reads a property of `app`
// when it changes, so nothing asks for a redraw.

const query = new URLSearchParams(location.search);

export const app = $state({
  screen: 'loading',   // loading | setup-account | setup-https | sign-in | join | reset | app
  session: null,       // SessionResponse: { setupRequired, user }
  linkToken: query.get('invite') ?? query.get('reset'),
  link: null,          // LinkDetails for linkToken: { purpose, username, expiresAt }
  https: null,         // HttpsStatusResponse
  security: null,      // SecurityResponse (admins only)
  notifications: null, // NotificationSettingsResponse (admins only)
  backups: null,       // BackupsResponse (admins only)
  tab: 'find',         // the open tab; the address bar says which (see addresses.js)
  settingsSection: 'login', // which Settings sidebar item is open
  aiFeatures: { ready: [] }, // AiFeaturesResponse: the AI tasks that have a model set up
  toasts: [],          // { id, text, kind: 'note' | 'error', action?: { label, run } } shown in the corner by Toasts.svelte: errors, and notes with a button
  inbox: { entries: [], unread: 0 }, // InboxResponse: the signed-in person's messages behind the bell (inbox.svelte.js)
  inboxLocal: [],      // notes not saved to the inbox yet: { id, text, createdAt, read, local: true }
  confirmation: null,  // { title, message, confirmLabel, danger } while ConfirmDialog.svelte is asking
  busy: 0,             // actions run() has started and not finished; App.svelte shows a progress bar while above 0
  setupDirection: 1,   // which way the last setup move went: 1 forward, -1 back, so the steps slide the right way
  offline: false,      // true while Invy can't be reached
  spaces: [],          // SpaceResponse[]: the spaces the signed-in person is in (spaces.svelte.js)
  spaceId: null,       // the space the page shows
  starterPacks: [],    // StarterPackResponse[]: offered when making a space
  newSpaceOpen: false, // true while the New space dialog is open
  itemId: null,        // the item page open on Browse (/browse/{id}), or null for the top of the space
  places: {},          // the sticky place per space id: an ItemRef new things go into ("Adding to Box F"), kept in this browser (labels.svelte.js)
  scanLabel: null,     // a label code /l/<code> opened that isn't on anything yet, offered on Scan to claim
  auditItem: null,     // a container Scan's Audit mode opened, whose page starts the audit walk
});

export const isAdmin = () => app.session?.user?.isAdmin === true;

/** Runs an action, then shows either its result or a readable error. */
export async function run(action) {
  clearErrors();
  app.busy++;
  try {
    await action();
    app.offline = false;
  } catch (error) {
    if (error.offline) {
      wentOffline();
    } else if (error.status === 401 && app.screen === 'app') {
      app.screen = 'sign-in';
      toast('Your session has ended. Sign in again.', 'error');
    } else {
      toast(error.message, 'error');
    }
  } finally {
    app.busy--;
  }
}

/** Notes that Invy can't be reached, once per spell offline. The live stream closes until the next action works. */
export function wentOffline() {
  if (app.offline && app.screen === 'app') return;
  if (app.screen === 'app') app.offline = true;
  toast("Invy can't be reached. Check your connection, then try again.", 'error');
}

// ---------- toasts ----------

let nextToastId = 0;
let noteSink = null;

/** Where notes go instead of the corner while someone is signed in: the inbox (inbox.svelte.js registers it). */
export function setNoteSink(sink) {
  noteSink = sink;
}

/**
 * Tells the person what happened. While someone is signed in, a note goes quietly into the inbox behind the bell,
 * so nothing covers the page. An error, and any message before sign-in, shows in the corner instead: a note goes
 * away by itself after long enough to read it, and an error stays until it is closed or the next action starts.
 * `action` ({ label, run }) adds a button, such as Undo. A note with one shows in the corner as well, for a little
 * longer, so there is time to press it. Returns the corner message's id, or null when it went only to the inbox.
 */
export function toast(text, kind = 'note', action = null) {
  if (kind === 'note' && noteSink?.(text) && !action) return null;
  const id = nextToastId++;
  app.toasts.push({ id, text, kind, action });
  if (kind !== 'error') setTimeout(() => dismissToast(id), Math.max(action ? 8000 : 5000, text.length * 60));
  return id;
}

export function dismissToast(id) {
  const index = app.toasts.findIndex((item) => item.id === id);
  if (index >= 0) app.toasts.splice(index, 1);
}

/** Closes the errors left by an earlier action. Notes stay until they time out. */
export function clearErrors() {
  app.toasts = app.toasts.filter((item) => item.kind !== 'error');
}

// ---------- confirmation ----------

let answerConfirmation = null;

/**
 * Asks before doing something hard to undo, in a dialog on the page. Resolves to true when confirmed, false when
 * cancelled. `danger` colours the confirm button for changes that lose data.
 */
export function ask({ title, message, confirmLabel = 'Confirm', danger = false }) {
  answerConfirmation?.(false);
  app.confirmation = { title, message, confirmLabel, danger };
  return new Promise((resolve) => { answerConfirmation = resolve; });
}

/** Closes the confirmation dialog with the person's answer. */
export function answer(confirmed) {
  const resolve = answerConfirmation;
  answerConfirmation = null;
  app.confirmation = null;
  resolve?.(confirmed);
}
