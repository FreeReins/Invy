/** Reads a yyyy-mm-dd date as a local date, so it doesn't shift a day in time zones west of UTC. */
export function parseDate(iso) {
  const [year, month, day] = iso.split('-').map(Number);
  return new Date(year, month - 1, day);
}

/** A local date as yyyy-mm-dd, the way the API writes dates. */
export const isoDate = (date) =>
  `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, '0')}-${String(date.getDate()).padStart(2, '0')}`;

export const todayIso = () => isoDate(new Date());

/** The yyyy-mm-dd date some days after (or before) another. */
export function addDays(iso, days) {
  const date = parseDate(iso);
  date.setDate(date.getDate() + days);
  return isoDate(date);
}

export const dayMonth = (date) => date.toLocaleDateString(undefined, { month: 'short', day: 'numeric' });

export const shortDate = (iso) => (iso ? dayMonth(parseDate(iso)) : '');

/** How long ago a moment (an ISO time) was, briefly: "Just now", "5 min ago", "3 h ago", "Yesterday", or its date. */
export function ago(iso, now = new Date()) {
  const then = new Date(iso);
  const minutes = Math.floor((now - then) / 60000);
  if (minutes < 1) return 'Just now';
  if (minutes < 60) return `${minutes} min ago`;
  if (minutes < 24 * 60 && then.getDate() === now.getDate()) return `${Math.floor(minutes / 60)} h ago`;
  const yesterday = new Date(now);
  yesterday.setDate(now.getDate() - 1);
  if (then.toDateString() === yesterday.toDateString()) return 'Yesterday';
  return dayMonth(then);
}

/**
 * A date in full, with the time when it is known: "Saturday, November 21, 2026 at 6:42 PM", or
 * "Saturday, November 21, 2026" when there is only a yyyy-mm-dd date.
 */
export function fullDate(iso, at) {
  if (at) return new Date(at).toLocaleString(undefined, { dateStyle: 'full', timeStyle: 'short' });
  return iso ? parseDate(iso).toLocaleDateString(undefined, { dateStyle: 'full' }) : '';
}

/** "3 items", "1 item". */
export const plural = (count, word) => `${count} ${word}${count === 1 ? '' : 's'}`;

/** Words joined the way a sentence lists them: "a, b and c". */
export const listWords = (words) => (words.length < 2 ? words.join('') : `${words.slice(0, -1).join(', ')} and ${words[words.length - 1]}`);
