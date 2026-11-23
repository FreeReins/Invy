// What scanning does, beyond what axe sees: which calls each mode makes, and where a label's address leads.
import { test, expect, open } from './fixtures.js';
import * as data from './data.js';

/** Types a code into the Scan tab's box, as if it was scanned. */
async function scan(page, code) {
  await page.getByLabel(/^Or type /).fill(code);
  await page.getByRole('button', { name: 'Go' }).click();
}

test("a claimed label's address opens what it is on", async ({ page }) => {
  await open(page, `/l/${data.labelCodes.onBoxF}`);
  await expect(page).toHaveURL(new RegExp(`/browse/${data.itemIds.f}$`));
  await expect(page.getByRole('heading', { name: 'Box F', exact: true })).toBeVisible();
  await expect(page.getByText(`Label ${data.labelCodes.onBoxF}`)).toBeVisible();
});

test("an unclaimed label's address offers to make a box, in one tap", async ({ page }) => {
  await open(page, `/l/${data.labelCodes.unclaimed}`);
  await expect(page).toHaveURL(/\/scan$/);
  const claimed = page.waitForRequest((request) => request.method() === 'POST' && request.url().endsWith(`/labels/${data.labelCodes.unclaimed}/claim`));
  await page.getByRole('button', { name: `Make Box ${data.labelCodes.unclaimed} at the top` }).click();
  expect((await claimed).postDataJSON()).toEqual({ parentId: null, name: null, typeId: null });
  await expect(page.getByRole('link', { name: `Made Box ${data.labelCodes.unclaimed} at the top.` })).toBeVisible();
});

test('a label Invy does not have says so on Scan', async ({ page }) => {
  await open(page, '/l/XXXX');
  await expect(page).toHaveURL(/\/scan$/);
  await expect(page.getByText('There is no label XXXX')).toBeVisible();
});

test('scanning a product barcode in Open mode searches for it', async ({ page }) => {
  await open(page, '/scan');
  await scan(page, '0012345678905');
  await expect(page).toHaveURL(/\/find\?q=0012345678905$/);
});

test('claiming puts each new box in the sticky place, which stays', async ({ page }) => {
  await open(page, `/browse/${data.itemIds.f}`);
  await page.getByRole('button', { name: 'Add things here' }).click();
  await expect(page.getByRole('button', { name: 'Adding here' })).toBeDisabled();

  await page.getByRole('navigation', { name: 'Views' }).getByRole('link', { name: 'Scan' }).click();
  await page.getByRole('radio', { name: 'Claim' }).check();
  await expect(page.getByRole('group', { name: 'Where new things go' })).toContainText('Adding to Box F');
  const claimed = page.waitForRequest((request) => request.url().endsWith('/claim'));
  await scan(page, `https://invy.local/l/${data.labelCodes.unclaimed.toLowerCase()}`);
  expect((await claimed).postDataJSON().parentId).toBe(data.itemIds.f);
  await expect(page.getByText(`Made Box ${data.labelCodes.unclaimed} in Box F.`)).toBeVisible();

  await page.reload();
  await page.getByRole('radio', { name: 'Claim' }).check();
  await expect(page.getByRole('group', { name: 'Where new things go' })).toContainText('Adding to Box F');
});

test('Move takes the thing, then where it goes', async ({ page }) => {
  await open(page, '/scan');
  await page.getByRole('radio', { name: 'Move' }).check();
  await scan(page, data.labelCodes.onBoxF);
  await expect(page.getByText('Moving Box F. Now scan where it is going.')).toBeVisible();
  // Box F again isn't somewhere to go.
  await scan(page, data.labelCodes.onBoxF);
  await expect(page.getByText("That's Box F again.")).toBeVisible();

  // Any other code is answered as unclaimed, so the shed's label is answered here.
  await page.route('**/api/labels/RHMT', (route) => route.fulfill({ json: data.label('RHMT', { id: data.itemIds.shed, name: 'Shed', marking: null }) }));
  const moved = page.waitForRequest((request) => request.url().endsWith(`/items/${data.itemIds.f}/move`));
  await scan(page, 'RHMT');
  expect((await moved).postDataJSON()).toEqual({ parentId: data.itemIds.shed });
  await expect(page.getByText('Moved Box F into Shed.')).toBeVisible();
});

test('a new label sheet asks for whole sheets and shows them to print', async ({ page }) => {
  await open(page, '/settings/labels');
  await page.getByLabel('Sheets').fill('2');
  const made = page.waitForRequest((request) => request.method() === 'POST' && request.url().endsWith('/labels'));
  await page.getByRole('button', { name: 'Make 60 labels' }).click();
  expect((await made).postDataJSON()).toEqual({ count: 60 });
  await expect(page.getByRole('img', { name: /^Sheet 2 of 2: 30 labels/ })).toBeVisible();
});

test('opening a box marks it seen, and an audit sends what was ticked or scanned', async ({ page }) => {
  const seen = page.waitForRequest((request) => request.method() === 'POST' && request.url().endsWith(`/items/${data.itemIds.f}/seen`));
  await open(page, '/scan');
  await page.getByRole('radio', { name: 'Audit' }).check();
  await scan(page, data.labelCodes.onBoxF);
  await seen;
  await expect(page).toHaveURL(new RegExp(`/browse/${data.itemIds.f}$`));
  await expect(page.getByText('Last seen: Just now')).toBeVisible();

  const dialog = page.getByRole('dialog', { name: 'Audit Box F' });
  await dialog.getByRole('checkbox', { name: 'Baseball' }).check();
  await expect(dialog.getByText('1 of 3 ticked')).toBeVisible();
  const audited = page.waitForRequest((request) => request.url().endsWith(`/items/${data.itemIds.f}/audit`));
  await dialog.getByRole('button', { name: 'Finish the audit' }).click();
  expect((await audited).postDataJSON()).toEqual({ present: [data.itemIds.ball] });
  await expect(dialog.getByText('Missing (1)')).toBeVisible();
  await dialog.getByRole('button', { name: 'Move into Box F' }).click();
  await expect(dialog.getByText('Moved here')).toBeVisible();
});

test('Add mode looks a product barcode up and adds it with its photo', async ({ page }) => {
  await open(page, '/scan');
  await page.getByRole('radio', { name: 'Add' }).check();
  await scan(page, data.product().barcode);
  await expect(page.getByRole('heading', { name: 'DeWalt: 20V cordless drill' })).toBeVisible();
  await page.getByRole('button', { name: 'Add it at the top' }).click();
  const dialog = page.getByRole('dialog', { name: 'New item' });
  await expect(dialog.getByLabel('Name')).toHaveValue('DeWalt 20V cordless drill');
  const photo = page.waitForRequest((request) => request.url().endsWith('/attachments/from-web'));
  await dialog.getByRole('button', { name: /^Add|^Save/ }).click();
  expect((await photo).postDataJSON()).toEqual({ url: data.product().imageUrl, name: null });
});
