// QR labels and the sticky place. A label's QR holds https://<host>/l/<code>; the code alone is unique across the
// install, so looking it up tells which space it belongs to. The sticky place ("Adding to Box F") is where scanning
// and quick add put new things, per space, kept in this browser under KEY until changed.
import { go } from './addresses.js';
import { api } from './api.js';
import { pickSpace } from './spaces.svelte.js';
import { app, toast } from './state.svelte.js';

const KEY = 'invy-places';

/** The code's characters: no 0 O, 1 I L, 5 S, 8 B or 2 Z, which are easily misread. Matches LabelEngine.Alphabet. */
export const ALPHABET = '34679ACDEFGHJKMNPQRTUVWXY';

/**
 * The label code in what was scanned or typed (7k3q, or an address ending in /l/7K3Q), or null when it isn't one, such
 * as a product barcode.
 */
export function labelCode(text) {
  let code = (text ?? '').trim().replace(/[?#].*$/, '').replace(/\/+$/, '');
  code = code.slice(code.lastIndexOf('/') + 1).toUpperCase();
  return code.length >= 4 && code.length <= 6 && [...code].every((c) => ALPHABET.includes(c)) ? code : null;
}

/** What a label's QR holds: this Invy's address for it. */
export const labelAddress = (code) => `${location.origin}/l/${code}`;

/** LabelResponse for a code, in whichever space it is in. */
export const findLabel = (code) => api('GET', `/api/labels/${encodeURIComponent(code)}`);

/** Shows the space a label belongs to, when that isn't the one shown. */
export function showSpaceOf(label) {
  if (label.spaceId === app.spaceId) return;
  pickSpace(label.spaceId);
  toast(`Switched to ${app.spaces.find((space) => space.id === label.spaceId)?.name ?? 'its space'}.`);
}

/**
 * What /l/<code> opens: the item the label is on, or Scan offering to claim it. Called from showAddress, so the
 * address is replaced rather than added to history.
 */
export async function openLabel(code) {
  let label;
  try {
    label = await findLabel(code);
  } catch (error) {
    // Scan is where to try again; the error says what was wrong with the label.
    history.replaceState(null, '', '/scan');
    app.tab = 'scan';
    throw error;
  }
  showSpaceOf(label);
  if (label.item) {
    await go(`/browse/${label.item.id}`, { replace: true });
  } else {
    app.scanLabel = label.code;
    await go('/scan', { replace: true });
  }
}

// ---------- the sticky place ----------

function remembered() {
  try { return JSON.parse(localStorage.getItem(KEY) ?? '{}') ?? {}; } catch { return {}; }
}
app.places = remembered();

/** The sticky place of the space shown (ItemRef), or null for the top. */
export const stickyPlace = () => app.places[app.spaceId] ?? null;

/** Makes a container the place new things go, until changed. Null goes back to the top. */
export function setStickyPlace(place) {
  if (place) app.places[app.spaceId] = { id: place.id, name: place.name, marking: place.marking ?? null };
  else delete app.places[app.spaceId];
  try { localStorage.setItem(KEY, JSON.stringify(app.places)); } catch { /* kept for this visit only */ }
}
