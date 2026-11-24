import { app } from './state.svelte.js';

// ---------- live updates ----------
// Invy tells the open page which part of the data changed (GET /api/live, server-sent events), whoever changed
// it: a backup finishing, a message arriving in the inbox. Each lib registers what to load for its topic with onLive(). Only the topic comes down the stream,
// never data, so what's shown always comes from the normal API.
//
// A topic missed while the stream was down is not repeated, so after a reconnect every loader runs once. The stream
// is closed while signed out or offline; it opens again when the page is back in the app.

/** Whether the stream is open right now. Without it, actions refresh what they changed themselves. */
export const live = $state({ connected: false });

const loaders = new Map();
const waiting = new Map();
const DEBOUNCE = 150;

/** Runs `load` whenever `topic` changes ("backups", "inbox"; Invy.Api LiveTopics). */
export function onLive(topic, load) {
  if (!loaders.has(topic)) loaders.set(topic, new Set());
  loaders.get(topic).add(load);
  return () => loaders.get(topic).delete(load);
}

/** Loads a topic once, however many times it is announced in a burst. Failures stay quiet: the next change retries. */
function announce(topic) {
  clearTimeout(waiting.get(topic));
  waiting.set(topic, setTimeout(() => {
    waiting.delete(topic);
    for (const load of loaders.get(topic) ?? []) {
      Promise.resolve().then(load).catch(() => { /* offline or signed out meanwhile: the next change or reconnect loads it */ });
    }
  }, DEBOUNCE));
}

function connect() {
  const source = new EventSource('/api/live');
  let opened = false;
  source.addEventListener('error', () => { live.connected = false; });
  source.addEventListener('open', () => {
    live.connected = true;
    // The first connection comes right after everything was loaded; a reconnect may have missed changes.
    if (opened) for (const topic of loaders.keys()) announce(topic);
    opened = true;
  });
  for (const topic of ['backups', 'inbox', 'inbox-items', 'items']) {
    source.addEventListener(topic, () => announce(topic));
  }
  return () => {
    source.close();
    live.connected = false;
  };
}

$effect.root(() => {
  $effect(() => {
    if (app.screen !== 'app' || !app.session?.user || app.offline || typeof EventSource === 'undefined') return;
    return connect();
  });
});
