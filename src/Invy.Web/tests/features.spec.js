// The space features of phase 6 beyond what axe sees: each shows only while its switch is on, and its buttons send
// what Invy expects. Low stock, lending, upkeep, moving and projects on an item; the Views tab's lists; imports and
// the calendar feed in Settings.
import { test, expect, open } from './fixtures.js';
import * as data from './data.js';

const allOn = data.spacesWith(data.allFeatures);
const allOff = data.spacesWith([]);
const sent = (api, method, path) => api.requests.filter((request) => request.startsWith(`${method} `) && request.includes(path));

test('low stock, a loan, upkeep and a destination show on an item only while their features are on', async ({ page, api }) => {
  const toolWithService = data.itemTypes().map((type) => (type.name === 'Tool'
    ? { ...type, fields: [...type.fields, { id: '00000000-0000-4000-8000-000000000e05', key: 'service', label: 'Next service', kind: 'Date', unit: null, options: [], required: false, sortable: true, remind: 7, repeatMonths: 12 }] }
    : type));
  const drill = data.lentDrill();
  drill.item.fields['00000000-0000-4000-8000-000000000e05'] = '2026-10-01';
  api.set({
    'GET /api/spaces': allOn,
    'GET /api/spaces/*/types': toolWithService,
    'GET /api/spaces/*/items/*': (request) => {
      const id = new URL(request.url()).pathname.split('/').pop();
      return { [data.itemIds.screws]: data.lowScrews(), [data.itemIds.drill]: drill, [data.itemIds.f]: data.packedBox() }[id] ?? data.itemDetail(id);
    },
    // Opening a box marks it seen, and the page shows the item that call sends back.
    'POST /api/spaces/*/items/*/seen': data.packedBox().item,
  });

  await open(page, `/browse/${data.itemIds.screws}`);
  await expect(page.getByText('Running low').first()).toBeVisible();
  await expect(page.getByText('Low stock at 20.')).toBeVisible();

  await open(page, `/browse/${data.itemIds.drill}`);
  await expect(page.getByText('Lent to Sam')).toBeVisible();
  await page.getByRole('button', { name: 'Done today' }).click();
  await expect.poll(() => sent(api, 'POST', '/upkeep-done')).toHaveLength(1);
  await page.getByRole('button', { name: "It's back" }).click();
  await expect.poll(() => sent(api, 'POST', '/return')).toHaveLength(1);

  await open(page, `/browse/${data.itemIds.f}`);
  await expect(page.getByRole('link', { name: 'Manifest' })).toBeVisible();
  await page.getByRole('button', { name: 'Arrived' }).click();
  await expect.poll(() => sent(api, 'POST', '/arrived')).toHaveLength(1);

  api.set({ 'GET /api/spaces': allOff });
  const projectCallsBefore = sent(api, 'GET', '/projects').length;
  await open(page, `/browse/${data.itemIds.screws}`);
  await page.getByText('Where it has been').waitFor();
  await expect(page.getByText('Running low')).toHaveCount(0);
  await open(page, `/browse/${data.itemIds.drill}`);
  await page.getByText('Where it has been').waitFor();
  await expect(page.getByText('Lent to Sam')).toHaveCount(0);
  await expect(page.getByRole('button', { name: 'Done today' })).toHaveCount(0);
  await open(page, `/browse/${data.itemIds.f}`);
  await page.getByText('Where it has been').waitFor();
  await expect(page.getByRole('button', { name: 'Arrived' })).toHaveCount(0);
  expect(sent(api, 'GET', '/projects')).toHaveLength(projectCallsBefore);
});

test('lending something sends who has it and when it is due', async ({ page, api }) => {
  api.set({ 'GET /api/spaces': allOn });
  await open(page, `/browse/${data.itemIds.drill}`);
  await page.getByRole('button', { name: 'Lend' }).click();
  await page.getByLabel('Lent to').fill(' Sam ');
  const lend = page.waitForRequest((request) => request.method() === 'POST' && request.url().endsWith('/lend'));
  await page.getByRole('dialog').getByRole('button', { name: 'Lend' }).click();
  expect((await lend).postDataJSON()).toEqual({ to: 'Sam', dueOn: null });
});

test('setting something aside for a new project starts the project first', async ({ page, api }) => {
  api.set({ 'GET /api/spaces': allOn, 'GET /api/spaces/*/projects': [] });
  await open(page, `/browse/${data.itemIds.screws}`);
  await page.getByRole('button', { name: 'Set aside for a project' }).click();
  await page.getByLabel("New project's name").fill('Shelf');
  await page.getByLabel('How much').fill('5');
  const reserve = page.waitForRequest((request) => request.url().endsWith('/reserve'));
  await page.getByRole('button', { name: 'Set aside', exact: true }).click();
  expect((await reserve).postDataJSON()).toEqual({ itemId: data.itemIds.screws, amount: 5 });
  expect(sent(api, 'POST', `/spaces/${data.spaceIds.home}/projects`).length).toBeGreaterThan(0);
});

test("the Views tab's lists show only for the features that are on", async ({ page, api }) => {
  api.set({ 'GET /api/spaces': allOn });
  await open(page, '/views');
  for (const name of ['Running low', 'Lent out', 'Coming up', 'Moving', 'Projects']) {
    await expect(page.getByRole('link', { name })).toBeVisible();
  }

  api.set({ 'GET /api/spaces': data.spacesWith(['lending']) });
  await open(page, '/views');
  await expect(page.getByRole('link', { name: 'Lent out' })).toBeVisible();
  await expect(page.getByRole('link', { name: 'Running low' })).toHaveCount(0);
  await expect(page.getByRole('link', { name: 'Projects' })).toHaveCount(0);

  await open(page, '/views/low-stock');
  await expect(page.getByRole('heading', { name: 'That list is switched off' })).toBeVisible();
  expect(sent(api, 'GET', '/low-stock')).toEqual([]);
});

test('an overdue loan is marked, and It\'s back returns it', async ({ page, api }) => {
  api.set({ 'GET /api/spaces': allOn });
  await open(page, '/views/loans');
  const row = page.getByRole('listitem').filter({ hasText: 'Drill' });
  await expect(row.getByText('Overdue')).toBeVisible();
  await row.getByRole('button', { name: "It's back" }).click();
  await expect.poll(() => sent(api, 'POST', `/items/${data.itemIds.drill}/return`)).toHaveLength(1);
});

test('an import is uploaded, its columns checked, and started', async ({ page, api }) => {
  await open(page, '/settings/import');
  await expect(page.getByText('1 row was skipped')).toBeVisible();
  await page.getByLabel('CSV file').setInputFiles({ name: 'homebox-export.csv', mimeType: 'text/csv', buffer: Buffer.from('HB.name\nSander\n') });
  await page.getByRole('button', { name: 'Upload and check' }).click();
  await expect(page.getByRole('cell', { name: 'Sander' })).toBeVisible();
  await page.getByLabel('Where HB.serial_number goes').selectOption('ignore');
  const start = page.waitForRequest((request) => request.url().endsWith('/start'));
  await page.getByRole('button', { name: 'Import 2 rows' }).click();
  expect((await start).postDataJSON()).toEqual({
    mapping: [{ column: 0, target: 'location' }, { column: 1, target: 'tags' }, { column: 2, target: 'name' }, { column: 3, target: 'quantity' }, { column: 4, target: 'ignore' }],
    parentId: null, typeId: null,
  });
});

test('the calendar feed shows while reminders or lending are on, and owners turn it on', async ({ page, api }) => {
  await open(page, '/settings/space');
  await page.getByRole('button', { name: 'Turn on the calendar feed' }).click();
  await expect(page.getByLabel('Address to subscribe to')).toHaveValue(data.calendarFeed().url);

  api.set({ 'GET /api/spaces': data.spacesWith(['values']) });
  await open(page, '/settings/space');
  await page.getByRole('heading', { name: 'Features', exact: true }).waitFor();
  await expect(page.getByRole('heading', { name: 'Calendar feed' })).toHaveCount(0);
});
