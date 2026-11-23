// Getting text out of Invy: to the share sheet, the clipboard or a downloaded file. Printing is styles.css's @media print.

/** True when the browser can hand text to the phone's share sheet; otherwise sharing copies it instead. */
export const canShare = () => typeof navigator.share === 'function';

/**
 * Sends text to the share sheet, or copies it where there is none. Returns 'shared', 'copied' or 'cancelled' (the
 * share sheet was closed), so the caller can say what happened.
 */
export async function share(title, text) {
  if (canShare()) {
    try {
      await navigator.share({ title, text });
      return 'shared';
    } catch (error) {
      if (error.name === 'AbortError') return 'cancelled';
      // Some browsers have the share sheet but refuse it here, for example without a click; copy instead.
    }
  }
  await copyText(text);
  return 'copied';
}

/** Puts text on the clipboard. Browsers only allow it over a secure connection, so this says so when they refuse. */
export async function copyText(text) {
  if (!navigator.clipboard?.writeText) {
    throw new Error('This browser can only copy over a secure connection. Open Invy over HTTPS, or select the text and copy it yourself.');
  }
  await navigator.clipboard.writeText(text);
}

/** Saves text as a file, named `name`, through the browser's usual download. */
export function download(name, type, text) {
  const url = URL.createObjectURL(new Blob([text], { type }));
  const link = Object.assign(document.createElement('a'), { href: url, download: name });
  document.body.append(link);
  link.click();
  link.remove();
  setTimeout(() => URL.revokeObjectURL(url), 0);
}
