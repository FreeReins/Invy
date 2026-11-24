import { api } from './api.js';
import { app, isAdmin } from './state.svelte.js';

// ---------- settings ----------

export const httpsUrl = () => `https://${location.hostname}${app.https.port === 443 ? '' : `:${app.https.port}`}/`;

/** Saves the sign-in rules. Turning "always use HTTPS" on from plain HTTP moves the browser to HTTPS. */
export async function saveSecurity(changes) {
  app.security = await api('PUT', '/api/system/security', { ...app.security, ...changes });
  app.https.requireHttps = app.security.requireHttps;
  if (app.security.requireHttps && location.protocol === 'http:') {
    location.href = httpsUrl();
  }
}

export async function loadSystem() {
  const admin = (path) => (isAdmin() ? api('GET', path) : null);
  const [https, security, notifications, backups] = await Promise.all([
    api('GET', '/api/system/https'),
    admin('/api/system/security'),
    admin('/api/system/notifications'),
    admin('/api/system/backups'),
  ]);
  Object.assign(app, { https, security, notifications, backups });
}

// ---------- AI models ----------

/** Which AI tasks have a model set up (AiFeaturesResponse.ready), so the page offers only those. */
export async function loadAiFeatures() {
  app.aiFeatures = await api('GET', '/api/ai/features');
}

/** True when an AI task (AiTask name, such as 'ItemsFromPhoto') has a model, so the page can offer it. */
export const aiReady = (task) => app.aiFeatures.ready.includes(task);
