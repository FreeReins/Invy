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
    // Spaces: the shop, where Alex is a viewer, answers the same as Home.
    'GET /api/spaces': data.spaces(),
    'GET /api/starter-packs': data.starterPacks(),
    'POST /api/spaces': (request) => ({ ...data.spaces()[0], id: '00000000-0000-4000-8000-000000000c09', ...request.postDataJSON(), role: 'Owner' }),
    'PUT /api/spaces/*': (request) => ({ ...data.spaces()[0], ...request.postDataJSON() }),
    'DELETE /api/spaces/*': null,
    'GET /api/spaces/*/types': data.itemTypes(),
    'POST /api/spaces/*/types': (request) => ({ ...request.postDataJSON(), id: '00000000-0000-4000-8000-000000000d09', version: 1 }),
    'PUT /api/spaces/*/types/*': (request) => ({ ...request.postDataJSON(), id: new URL(request.url()).pathname.split('/').pop(), version: request.postDataJSON().version + 1 }),
    'DELETE /api/spaces/*/types/*': null,
    'GET /api/spaces/*/members': data.spaceMembers(),
    'POST /api/spaces/*/members': (request) => [...data.spaceMembers(), { userId: '00000000-0000-4000-8000-000000000004', username: request.postDataJSON().username, displayName: request.postDataJSON().username, role: request.postDataJSON().role }],
    'PUT /api/spaces/*/members/*': data.spaceMembers(),
    'DELETE /api/spaces/*/members/*': data.spaceMembers().slice(0, 2),
    // Items: the shed and garage at the top, Box F in the garage with a baseball, screws and a drill in it.
    'GET /api/spaces/*/items': (request) => (new URL(request.url()).searchParams.get('parent')
      ? (new URL(request.url()).searchParams.get('after') ? { items: [], next: null } : data.boxContents())
      : data.topItems()),
    'GET /api/spaces/*/inbox': data.inboxItems(),
    'GET /api/spaces/*/items/*': (request) => data.itemDetail(new URL(request.url()).pathname.split('/').pop()) ?? reply(404, { detail: 'That item no longer exists.' }),
    'GET /api/spaces/*/items/*/history': data.itemHistory(),
    'GET /api/spaces/*/activity': (request) => (new URL(request.url()).searchParams.get('before') ? { events: [], next: null } : data.activity()),
    'POST /api/spaces/*/items': (request) => ({ ...data.itemDetail(data.itemIds.f).item, ...request.postDataJSON(), id: '00000000-0000-4000-8000-000000000f99' }),
    'PUT /api/spaces/*/items/*': (request) => ({ ...request.postDataJSON(), id: new URL(request.url()).pathname.split('/').pop() }),
    'POST /api/spaces/*/items/*/move': (request) => ({ ...data.itemDetail(data.itemIds.f).item, parentId: request.postDataJSON().parentId }),
    'POST /api/spaces/*/items/*/quantity': data.itemDetail(data.itemIds.screws).item,
    'POST /api/spaces/*/items/*/dispose': data.itemDetail(data.itemIds.f).item,
    'POST /api/spaces/*/items/*/restore': data.itemDetail(data.itemIds.f).item,
    'POST /api/spaces/*/quick-add': [],
    // Search: "baseball" finds two, "type:" can't be read, "nothing" finds none, "part" finds 120 (for scrolling),
    // and an empty box gives the recently changed items.
    'GET /api/spaces/*/search': (request) => {
      const params = new URL(request.url()).searchParams;
      const q = params.get('q') ?? '';
      if (q === 'type:' || q.startsWith('type:car')) return reply(400, { detail: 'There is no type called car. The types are Box, Thing, Tool.' });
      if (q.startsWith('nothing')) return { results: [], total: 0, totalIsEstimate: false, next: null };
      if (q.startsWith('baseball')) return data.baseballResults();
      if (q.startsWith('part')) return data.searchResults(120, Number(params.get('after') ?? 0));
      return data.searchResults(5);
    },
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
