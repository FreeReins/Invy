import { mount } from 'svelte';
import App from './App.svelte';
import './styles.css';

// Straight into <body>: the layout in styles.css centres the body itself.
mount(App, { target: document.body });

// Keeps the page on the device for offline use (public/sw.js). Browsers allow it only over HTTPS with a certificate
// they trust, or on localhost; anywhere else this quietly does nothing and the page works online as usual. Not in
// `npm run dev`, whose files change with every edit.
if (import.meta.env.PROD && 'serviceWorker' in navigator && window.isSecureContext) {
  navigator.serviceWorker.register('/sw.js').catch(() => { /* untrusted certificate or private mode: online only */ });
} else if (import.meta.env.DEV && 'serviceWorker' in navigator) {
  // A worker left on this localhost port by an earlier build (or another app on the same port) would keep serving
  // its stored icon and page, so dev removes it and its cache.
  navigator.serviceWorker.getRegistrations().then((workers) => workers.forEach((worker) => worker.unregister()));
  globalThis.caches?.keys().then((names) => names.forEach((name) => caches.delete(name)));
}
