import { listenForAddresses, showAddress } from './addresses.js';
import { api } from './api.js';
import { forgetInbox, loadInbox } from './inbox.svelte.js';
import { loadAiFeatures, loadSystem } from './settings.svelte.js';
import { app, run, toast } from './state.svelte.js';

// ---------- signing in and out ----------

/** Loads everything the signed-in views need, then shows the app. */
export async function enterApp() {
  if (app.linkToken) {
    history.replaceState(null, '', location.pathname);
    app.linkToken = null;
  }
  app.offline = false;
  await Promise.all([loadSystem(), loadAiFeatures(), loadInbox()]);
  await showAddress();
}

export function signOut() {
  return run(async () => {
    await api('POST', '/api/account/sign-out');
    app.session.user = null;
    app.toasts = [];
    forgetInbox();
    app.screen = 'sign-in';
  });
}

/** Works out which screen to show first: setup, the app, an invite or reset link, or sign-in. */
export async function start() {
  listenForAddresses();
  try {
    app.session = await api('GET', '/api/session');
  } catch (error) {
    if (!error.offline) throw error;
    app.session = { setupRequired: false, user: null };
    app.screen = 'sign-in';
    toast("Invy can't be reached. Check your connection, then reload the page.", 'error');
    return;
  }
  if (app.session.setupRequired) {
    app.screen = 'setup-account';
  } else if (app.session.user) {
    await enterApp();
  } else if (app.linkToken) {
    await api('GET', `/api/account/link?token=${encodeURIComponent(app.linkToken)}`)
      .then((link) => { app.link = link; app.screen = link.purpose === 'Invite' ? 'join' : 'reset'; })
      .catch((error) => { app.screen = 'sign-in'; toast(error.message, 'error'); });
  } else {
    app.screen = 'sign-in';
  }
}
