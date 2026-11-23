// The spaces the signed-in person is in, and which one the page shows. The choice is kept in this browser under KEY,
// so a reload opens the same space; every space-owned call puts app.spaceId in its address.
import { go } from './addresses.js';
import { api } from './api.js';
import { app, toast } from './state.svelte.js';

const KEY = 'invy-space';

function remembered() {
  try { return localStorage.getItem(KEY); } catch { return null; }
}

/** SpaceResponse of the space the page shows, or null when the person is in none. */
export const currentSpace = () => app.spaces.find((space) => space.id === app.spaceId) ?? null;

/** Editors and owners can change items and types. */
export const canEdit = () => ['Editor', 'Owner'].includes(currentSpace()?.role);

/** Owners manage members and the space itself. */
export const isOwner = () => currentSpace()?.role === 'Owner';

/** Loads the person's spaces and the starter packs, and picks the remembered space, or the first. */
export async function loadSpaces() {
  const [spaces, packs] = await Promise.all([api('GET', '/api/spaces'), api('GET', '/api/starter-packs')]);
  app.spaces = spaces;
  app.starterPacks = packs;
  const wanted = app.spaceId ?? remembered();
  app.spaceId = (spaces.find((space) => space.id === wanted) ?? spaces[0])?.id ?? null;
}

/** Shows another space. An item page open on Browse belongs to the old space, so Browse goes back to its top. */
export function pickSpace(id) {
  const leavingItem = app.spaceId !== id && app.tab === 'browse' && app.itemId;
  app.spaceId = id;
  try { localStorage.setItem(KEY, id); } catch { /* kept for this visit only */ }
  if (leavingItem) go('/browse');
}

/** Makes a space with the person as its owner, and shows it. */
export async function createSpace(name, preset) {
  const space = await api('POST', '/api/spaces', { name, preset: preset || null });
  app.spaces.push(space);
  pickSpace(space.id);
  toast(`${space.name} is ready.`);
  return space;
}

/** Puts a renamed space's new details in the list. */
export function replaceSpace(space) {
  const index = app.spaces.findIndex((item) => item.id === space.id);
  if (index >= 0) app.spaces[index] = space;
}

/** Takes a space the person deleted or left out of the list, and shows another. */
export function dropSpace(id) {
  app.spaces = app.spaces.filter((space) => space.id !== id);
  if (app.spaceId === id) {
    const next = app.spaces[0]?.id ?? null;
    if (next) pickSpace(next);
    else app.spaceId = null;
  }
}

export function forgetSpaces() {
  app.spaces = [];
  app.spaceId = null;
}
