// The `test` every spec uses: a page whose /api requests are answered from data.js, plus `api` to change answers.
// A request with no answer gets a 500 and fails the test once it ends, so the stubs can't fall behind the page.
import { test as base, expect } from '@playwright/test';
import * as data from './data.js';

const REPLY = Symbol('reply');

/** An answer other than 200 with JSON, for example `reply(500, { detail: 'The server is down.' })`. */
export const reply = (status, body = null, contentType = 'application/json') => ({ [REPLY]: true, status, body, contentType });

/** What every test starts with: an admin signed in, and everything the Settings tab loads. */
function defaults() {
  return {
    'GET /api/session': data.session(),
    // The live stream: an empty one that asks not to reconnect for ten minutes, so tests see no pushes.
    'GET /api/live': reply(200, Buffer.from('retry: 600000\n\n'), 'text/event-stream'),
    'GET /api/tokens': [],
    'POST /api/tokens': (request) => data.issuedApiToken(request.postDataJSON().name),
    'GET /api/system/https': data.https(),
    'GET /api/system/security': data.security(),
    'PUT /api/system/security': (request) => request.postDataJSON(),
    'GET /api/system/notifications': data.notifications(),
    'PUT /api/system/notifications': (request) => ({ ...data.notifications(), ...request.postDataJSON() }),
    'GET /api/system/backups': data.backups(),
    'PUT /api/system/backups': (request) => ({ ...data.backups(), ...request.postDataJSON() }),
    'GET /api/system/backups/*/summary': data.backupSummary(),
    'GET /api/system/logs': data.logs(),
    'GET /api/users': data.users(),
    'GET /api/links': data.links(),
    'GET /api/account/link': (request) =>
      data.linkDetails(new URL(request.url()).searchParams.get('token') === 'reset-token' ? 'PasswordReset' : 'Invite'),
    // No AI model is set up unless a test says so.
    'GET /api/ai/features': data.aiFeatures(),
    'GET /api/ai/settings': data.aiSettings(false),
    ...inbox(),
  };
}

/** The signed-in person's inbox, empty at first; it keeps the notes the page sends, as Invy would. */
function inbox() {
  const entries = [];
  const answer = () => ({ entries: [...entries], unread: entries.filter((entry) => !entry.read).length });
  return {
    'GET /api/inbox': () => answer(),
    'POST /api/inbox': (request) => {
      const id = `00000000-0000-4000-8000-${String(900000 + entries.length).padStart(12, '0')}`;
      entries.unshift({ id, text: request.postDataJSON().text, source: 'Page', createdAt: new Date().toISOString(), read: false });
      return answer();
    },
    'POST /api/inbox/read': () => {
      for (const entry of entries) entry.read = true;
      return answer();
    },
    'DELETE /api/inbox': () => {
      entries.length = 0;
      return answer();
    },
  };
}

/**
 * Opens the bell in the header and returns its panel, to check what the page said. Notes go there rather than the
 * corner while someone is signed in.
 */
export async function openInbox(page) {
  await page.getByRole('button', { name: /^Notifications/ }).click();
  return page.getByRole('region', { name: 'Notifications' });
}

/** Checks the inbox has a message, then closes the bell again so the rest of the test can carry on. */
export async function expectInbox(page, text) {
  await expect((await openInbox(page)).getByText(text)).toBeVisible();
  await page.keyboard.press('Escape');
}

/** 'GET /api/system/backups/*' matches any one path segment where the * is. */
const pattern = (key) => new RegExp(`^${key.replace(/[.+?^${}()|[\]\\]/g, '\\$&').replaceAll('*', '[^/]+')}$`);

class ApiStub {
  /** Requests that had no answer, as 'METHOD /path?query'. */
  unknown = [];
  /** Every request made, as 'METHOD /path?query', in order. */
  requests = [];
  /** While true, every request fails as if Invy can't be reached (no signal, server off), and isn't recorded. */
  offline = false;
  #routes = new Map();

  constructor(page) {
    this.page = page;
    this.set(defaults());
  }

  /**
   * Answers requests matching each 'METHOD /path' key from now on. A value is sent as JSON; a function gets the
   * Playwright request and returns the value (or a promise of it); `reply()` sends another status or content type.
   */
  set(routes) {
    for (const [key, value] of Object.entries(routes)) this.#routes.set(key, { match: pattern(key), value });
    return this;
  }

  async install() {
    await this.page.route('**/api/**', async (route) => {
      if (this.offline) return route.abort('internetdisconnected');
      const request = route.request();
      const url = new URL(request.url());
      const key = `${request.method()} ${url.pathname}`;
      this.requests.push(key + url.search);
      // The newest matching route wins, so set() can narrow a default.
      const found = [...this.#routes.values()].reverse().find((entry) => entry.match.test(key));
      if (!found) {
        this.unknown.push(key + url.search);
        return route.fulfill({ status: 500, json: { detail: `The test has no answer for ${key}.` } });
      }
      const value = typeof found.value === 'function' ? await found.value(request) : found.value;
      if (value?.[REPLY]) {
        const body = Buffer.isBuffer(value.body) ? value.body : value.body == null ? '' : JSON.stringify(value.body);
        return route.fulfill({ status: value.status, contentType: value.contentType, body });
      }
      return route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(value ?? null) });
    });
  }
}

export const test = base.extend({
  // Automatic, so a test that never names `api` still gets the stubbed answers.
  api: [async ({ page }, use) => {
    const stub = new ApiStub(page);
    await stub.install();
    await use(stub);
    expect(stub.unknown, 'requests the test had no answer for; add them to tests/fixtures.js').toEqual([]);
  }, { auto: true }],
});

export { expect };

/** Opens an address and waits until the page has left its loading screen. */
export async function open(page, path) {
  await page.goto(path);
  await expect(page.getByRole('status', { name: 'Loading', exact: true })).toHaveCount(0);
}
