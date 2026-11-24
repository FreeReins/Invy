// What saved views do, beyond what axe sees: live updates on Find and in an open view, and what the form sends.
import { test, expect, open, reply } from './fixtures.js';
import * as data from './data.js';

/**
 * A live stream that stays quiet until `push()` is called, then says the items changed (as Invy does when something
 * is scanned) and asks not to reconnect during the test.
 */
function liveStream(api) {
  let push;
  const pushed = new Promise((resolve) => { push = resolve; });
  api.set({
    'GET /api/live': async () => {
      await pushed;
      return reply(200, Buffer.from('event: items\ndata: scanned\n\nretry: 600000\n\n'), 'text/event-stream');
    },
  });
  return push;
}

test('a pinned "not seen in a year" view on Find drops a thing as soon as it is scanned', async ({ page, api }) => {
  const push = liveStream(api);
  await open(page, '/find');
  const pinned = page.getByRole('article', { name: 'Not seen in a year' });
  await expect(pinned.getByRole('link', { name: 'Baseball' })).toBeVisible();
  await expect(pinned.getByText('2 items')).toBeVisible();

  // The baseball is scanned somewhere else: the view no longer finds it, and the stream says the items changed.
  api.set({ 'GET /api/spaces/*/views/*/results': data.viewResults({ withoutBall: true }) });
  push();

  await expect(pinned.getByRole('link', { name: 'Baseball' })).toHaveCount(0);
  await expect(pinned.getByText('1 item', { exact: true })).toBeVisible();
  await expect(pinned.getByRole('link', { name: 'Drill' })).toBeVisible();
});

test('an open view loads again when the items change', async ({ page, api }) => {
  const push = liveStream(api);
  await open(page, `/views/${data.viewIds.notSeen}`);
  await expect(page.getByRole('cell', { name: 'Baseball' })).toBeVisible();

  api.set({ 'GET /api/spaces/*/views/*/results': data.viewResults({ withoutBall: true }) });
  push();

  await expect(page.getByRole('cell', { name: 'Baseball' })).toHaveCount(0);
  await expect(page.getByRole('cell', { name: 'DeWalt' })).toBeVisible();
});

test("a table's columns sort the loaded rows", async ({ page }) => {
  await open(page, `/views/${data.viewIds.notSeen}`);
  const names = page.getByRole('row').locator('td:first-child');
  await expect(names).toHaveText(['Drill', 'Baseball']);
  await page.getByRole('button', { name: 'Name' }).click();
  await expect(names).toHaveText(['Baseball', 'Drill']);
  await expect(page.getByRole('columnheader', { name: 'Name' })).toHaveAttribute('aria-sort', 'ascending');
});

test('saving a search from Find makes a view of it', async ({ page }) => {
  await open(page, '/find?q=baseball');
  await page.getByRole('link', { name: 'Save as a view' }).click();
  await expect(page.getByLabel('Search', { exact: true })).toHaveValue('baseball');
  await page.getByRole('textbox', { name: 'Name' }).fill('Balls');
  await page.getByLabel('Show as').selectOption('Board');
  await page.getByLabel('Group by').selectOption('power');
  await page.getByText('Pin to Find').click();
  const made = page.waitForRequest((request) => request.method() === 'POST' && request.url().endsWith('/views'));
  await page.getByRole('button', { name: 'Make view' }).click();
  expect((await made).postDataJSON()).toEqual({
    name: 'Balls', query: 'baseball', display: 'Board', columns: [], sort: null, groupBy: 'power', shared: false, pinned: true,
  });
  await expect(page).toHaveURL(/\/views\/00000000-0000-4000-8000-000000002099$/);
});

test('reports print from the browser and download their CSV from Invy', async ({ page }) => {
  await open(page, '/views');
  await expect(page.getByRole('link', { name: 'Download CSV' }).first()).toHaveAttribute('href', /\/reports\/insurance\.csv$/);
  await page.getByRole('link', { name: 'Insurance inventory', exact: true }).click();
  await expect(page.getByRole('heading', { name: /^Garage/ })).toContainText('129.50');
  await expect(page.getByRole('cell', { name: /Brand: DeWalt/ })).toBeVisible();

  await page.evaluate(() => { window.printed = 0; window.print = () => { window.printed += 1; }; });
  await page.getByRole('button', { name: 'Print or save as PDF' }).click();
  expect(await page.evaluate(() => window.printed)).toBe(1);
});

test('reports are hidden in a space without values and audits', async ({ page, api }) => {
  api.set({ 'GET /api/spaces': data.spacesWith([]) });
  await open(page, '/views');
  await expect(page.getByText('Tools by power')).toBeVisible();
  await expect(page.getByRole('heading', { name: 'Reports' })).toHaveCount(0);
  await expect(page.getByRole('columnheader', { name: 'Value' })).toHaveCount(0);
});

test("a view's CSV export is a download link to Invy", async ({ page }) => {
  await open(page, `/views/${data.viewIds.cards}`);
  await expect(page.getByRole('link', { name: 'Export CSV' })).toHaveAttribute('href', new RegExp(`/views/${data.viewIds.cards}/export\\.csv$`));
});
