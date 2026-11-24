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

/** The text an empty tab shows ('find', 'browse', 'scan' or 'types'): the space's kind's own hint, or `fallback`. */
export const kindHint = (tab, fallback = null) => app.starterPacks.find((pack) => pack.key === currentSpace()?.preset)?.hints?.[tab] ?? fallback;

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

/** Shows another space. An item page open on Browse, or a view open on Views, belongs to the old space, so the tab goes back to its start. */
export function pickSpace(id) {
  const leavingItem = app.spaceId !== id && app.tab === 'browse' && app.itemId;
  const leavingView = app.spaceId !== id && app.tab === 'views' && app.viewId;
  app.spaceId = id;
  try { localStorage.setItem(KEY, id); } catch { /* kept for this visit only */ }
  if (leavingItem) go('/browse');
  if (leavingView) go('/views');
}

/** Loads what the steps for a new space offer, unless it's here already: the starter packs and the feature catalogue. */
export async function loadSpaceChoices() {
  const [packs, features] = await Promise.all([
    app.starterPacks.length ? app.starterPacks : api('GET', '/api/starter-packs'),
    app.spaceFeatures.length ? app.spaceFeatures : api('GET', '/api/space-features'),
  ]);
  app.starterPacks = packs;
  app.spaceFeatures = features;
}

/** The starter pack (StarterPackResponse) with this key, or null for none. */
export const packOf = (key) => app.starterPacks.find((pack) => pack.key === key) ?? null;

/**
 * The answers for a new space while its steps are filled in: its name, its kind (a pack key, or '' for none), the
 * features switched on, the places ticked, the places typed in, and whether to add the kind's examples. It starts
 * as the first pack, named after it.
 */
export function newDraft(name = null) {
  const draft = { name: '', preset: '', features: [], places: [], ownPlaces: [], examples: false, made: null };
  chooseKind(draft, app.starterPacks[0]?.key ?? '');
  if (name != null) draft.name = name;
  return draft;
}

/**
 * Picks a draft's kind, which resets what follows from it: the features to the kind's own, and the places and
 * examples ticked. A name still the old kind's (or empty) becomes the new kind's.
 */
export function chooseKind(draft, key) {
  const before = packOf(draft.preset);
  const pack = packOf(key);
  if (!draft.name.trim() || draft.name === before?.name) draft.name = pack?.name ?? '';
  draft.preset = pack?.key ?? '';
  draft.features = [...(pack?.features ?? [])];
  draft.places = [];
  draft.examples = false;
}

/** Switches a feature on or off in a draft, keeping any that aren't offered yet as they are. */
export function toggleFeature(draft, key, on) {
  draft.features = on ? [...new Set([...draft.features, key])] : draft.features.filter((feature) => feature !== key);
}

/** The places a draft makes: the ticked ones, then the ones typed in. */
export const draftPlaces = (draft) => [...draft.places, ...draft.ownPlaces];

/** Makes the space a draft describes, with the person as its owner, and shows it. Returns the SpaceResponse. */
export async function createSpace(draft) {
  const name = draft.name.trim();
  if (!name) throw new Error('Give the space a name, such as Home or Workshop.');
  const space = await api('POST', '/api/spaces', {
    name,
    preset: draft.preset || null,
    features: draft.features,
    places: draftPlaces(draft),
    examples: Boolean(draft.preset) && draft.examples,
  });
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
