// Making a space, beyond what axe sees: the setup steps for each kind, the draft kept while moving between steps,
// the New space dialog, and the screens hiding what a space's features leave off.
import { test, expect, open, reply } from './fixtures.js';
import { expectAccessible } from './axe.js';
import * as data from './data.js';

const newSpaceId = '00000000-0000-4000-8000-000000000c09';

/**
 * A new install: no spaces until POST /api/spaces makes one, which later GETs then return. Returns the bodies sent.
 */
function freshInstall(api) {
  const sent = [];
  let made = [];
  api.set({
    'GET /api/session': { setupRequired: true, user: null },
    'POST /api/setup/account': data.admin(),
    'GET /api/spaces': () => made,
    'POST /api/spaces': (request) => {
      sent.push(request.postDataJSON());
      made = [{ ...data.spaces()[0], ...request.postDataJSON(), id: newSpaceId, role: 'Owner' }];
      return made[0];
    },
  });
  return sent;
}

/** Makes the admin account and moves past the secure connection step to Your space. */
async function toYourSpace(page) {
  await open(page, '/');
  await page.getByLabel('Your name').fill('Alex Rivera');
  await page.getByLabel('Username').fill('alex');
  await page.getByLabel(/^Password/).fill('correct horse');
  await page.getByRole('button', { name: 'Create account' }).click();
  await page.getByRole('button', { name: 'Continue' }).click();
  await page.getByRole('heading', { name: 'Secure connection' }).waitFor();
  await page.getByRole('button', { name: 'Next' }).click();
  await page.getByRole('heading', { name: 'Your space' }).waitFor();
}

const next = (page) => page.getByRole('button', { name: 'Next' }).click();
const offered = data.spaceFeatures().filter((feature) => feature.available);

const kinds = [...data.starterPacks(), { key: '', name: 'Start empty', features: [], places: null, examples: [] }];

for (const kind of kinds) {
  test(`setup makes a ${kind.name.toLowerCase()} space with its features and places`, async ({ page, api }) => {
    const sent = freshInstall(api);
    await toYourSpace(page);
    await page.getByRole('radio', { name: new RegExp(`^${kind.name}`) }).check();
    // An untouched name follows the kind; Start empty has none to give.
    await expect(page.getByLabel('Name')).toHaveValue(kind.key ? kind.name : '');
    await page.getByLabel('Name').fill(`My ${kind.name}`);
    await next(page);

    await page.getByRole('heading', { name: "What you'll use" }).waitFor();
    for (const feature of offered) {
      const box = page.getByRole('checkbox', { name: new RegExp(`^${feature.name}`) });
      if (kind.features.includes(feature.key)) await expect(box).toBeChecked();
      else await expect(box).not.toBeChecked();
    }
    // Every feature is built now, so every switch is offered.
    await expect(page.getByRole('checkbox', { name: /^Projects/ })).toHaveCount(1);
    await next(page);

    await page.getByRole('heading', { name: 'Places' }).waitFor();
    const suggested = kind.places?.suggest ?? [];
    if (suggested.length) await page.getByRole('checkbox', { name: suggested[0], exact: true }).check();
    await expect(page.getByRole('checkbox', { name: /^Add a few example items/ })).toHaveCount(kind.examples.length ? 1 : 0);
    if (kind.examples.length) await page.getByRole('checkbox', { name: /^Add a few example items/ }).check();
    await page.getByLabel('Your own place').fill('Spare room');
    await page.keyboard.press('Enter');
    await page.getByRole('button', { name: 'Finish' }).click();

    await expect(page).toHaveURL(/\/find$/);
    expect(sent).toEqual([{
      name: `My ${kind.name}`,
      preset: kind.key || null,
      features: kind.features,
      places: [...suggested.slice(0, 1), 'Spare room'],
      examples: kind.examples.length > 0,
    }]);
  });
}

test('going back and forward through setup keeps the answers', async ({ page, api }) => {
  const sent = freshInstall(api);
  await toYourSpace(page);
  await page.getByRole('radio', { name: /^Hobby/ }).check();
  await page.getByLabel('Name').fill('Workshop');
  await next(page);
  await page.getByRole('checkbox', { name: /^Barcodes/ }).uncheck();
  await page.getByRole('checkbox', { name: /^Values/ }).check();
  await next(page);
  await page.getByRole('checkbox', { name: 'Workbench' }).check();

  await page.getByRole('button', { name: 'Back' }).click();
  await expect(page.getByRole('checkbox', { name: /^Barcodes/ })).not.toBeChecked();
  await expect(page.getByRole('checkbox', { name: /^Values/ })).toBeChecked();
  // The progress bar jumps back too.
  await page.getByRole('navigation', { name: 'Setup progress' }).getByRole('button', { name: 'Your space' }).click();
  await expect(page.getByLabel('Name')).toHaveValue('Workshop');
  await expect(page.getByRole('radio', { name: /^Hobby/ })).toBeChecked();

  await page.getByRole('navigation', { name: 'Setup progress' }).getByRole('button', { name: 'Places' }).click();
  await expect(page.getByRole('checkbox', { name: 'Workbench' })).toBeChecked();
  await page.getByRole('button', { name: 'Finish' }).click();
  await expect(page).toHaveURL(/\/find$/);
  expect(sent).toEqual([{ name: 'Workshop', preset: 'hobby', features: ['stock', 'reminders', 'projects', 'values'], places: ['Workbench'], examples: false }]);
});

test('a space without a name is not made', async ({ page, api }) => {
  const sent = freshInstall(api);
  await toYourSpace(page);
  await page.getByLabel('Name').fill('');
  await next(page);
  await expect(page.getByText('Give the space a name')).toBeVisible();
  await expect(page.getByRole('heading', { name: 'Your space' })).toBeVisible();

  // Jumping to the last step and finishing comes back here, still without making anything.
  await page.getByRole('navigation', { name: 'Setup progress' }).getByRole('button', { name: 'Places' }).click();
  await page.getByRole('button', { name: 'Finish' }).click();
  await expect(page.getByRole('heading', { name: 'Your space' })).toBeVisible();
  // The first error may still be fading out as the second arrives.
  await expect(page.getByRole('alert')).toContainText('Give the space a name');
  expect(sent).toEqual([]);
});

test('coming back to a step after the space was made shows it and links to Settings', async ({ page, api }) => {
  freshInstall(api);
  // Making the space works, but opening Invy afterwards doesn't, so setup stays on its last step.
  let failing = true;
  api.set({ 'GET /api/ai/features': () => (failing ? reply(500, { detail: 'Invy could not load its AI settings. Try again.' }) : data.aiFeatures()) });
  await toYourSpace(page);
  await next(page);
  await next(page);
  await page.getByRole('button', { name: 'Finish' }).click();
  await expect(page.getByText('Invy could not load its AI settings')).toBeVisible();

  await page.getByRole('button', { name: 'Back' }).click();
  await page.getByRole('heading', { name: "What you'll use" }).waitFor();
  await expect(page.getByText('Change its name, kind and features')).toBeVisible();
  await expect(page.getByRole('checkbox', { name: /^Values/ })).toHaveCount(0);
  await expectAccessible(page);

  failing = false;
  await page.getByRole('link', { name: 'Settings' }).click();
  await expect(page).toHaveURL(/\/settings/);
  await expect(page.getByRole('navigation', { name: 'Views' })).toBeVisible();
  expect(api.requests.filter((request) => request === 'POST /api/spaces')).toHaveLength(1);
});

test('the New space dialog runs the same three steps', async ({ page, api }) => {
  await open(page, '/find');
  await page.getByRole('button', { name: 'New space' }).click();
  const dialog = page.getByRole('dialog', { name: 'New space' });
  await dialog.getByRole('radio', { name: /^Business/ }).check();
  await dialog.getByLabel('Name').fill('Corner shop 2');
  await dialog.getByRole('button', { name: 'Next' }).click();
  await expect(dialog.getByRole('heading', { name: "What you'll use" })).toBeFocused();
  await expect(dialog.getByRole('checkbox', { name: /^Audits/ })).toBeChecked();
  await dialog.getByRole('checkbox', { name: /^Audits/ }).uncheck();
  await dialog.getByRole('button', { name: 'Next' }).click();
  await dialog.getByRole('checkbox', { name: 'Stockroom' }).check();

  await dialog.getByRole('button', { name: 'Back' }).click();
  await expect(dialog.getByRole('checkbox', { name: /^Audits/ })).not.toBeChecked();
  await dialog.getByRole('button', { name: 'Next' }).click();
  await expect(dialog.getByRole('checkbox', { name: 'Stockroom' })).toBeChecked();

  const made = page.waitForRequest((request) => request.method() === 'POST' && new URL(request.url()).pathname === '/api/spaces');
  await dialog.getByRole('button', { name: 'Make the space' }).click();
  expect((await made).postDataJSON()).toEqual({
    name: 'Corner shop 2', preset: 'business', features: ['values', 'barcodes', 'stock', 'lending', 'reminders'], places: ['Stockroom'], examples: false,
  });
  await expect(dialog).toBeHidden();
});

test('audits and values show on a box only while the space has them on', async ({ page, api }) => {
  await open(page, `/browse/${data.itemIds.f}`);
  await expect(page.getByRole('button', { name: 'Audit' })).toBeVisible();
  await expect(page.getByText('Worth inside')).toBeVisible();

  api.set({ 'GET /api/spaces': data.spacesWith(['barcodes']) });
  await open(page, `/browse/${data.itemIds.f}`);
  await page.getByText('Where it has been').waitFor();
  await expect(page.getByRole('button', { name: 'Audit' })).toHaveCount(0);
  await expect(page.getByText('Worth inside')).toHaveCount(0);
});

test("Scan's Audit mode and barcode lookup show only while the space has them on", async ({ page, api }) => {
  await open(page, '/scan');
  await expect(page.getByRole('radio', { name: 'Audit' })).toBeVisible();
  await page.getByRole('radio', { name: 'Add' }).check();
  await expect(page.getByLabel('Or type a label code or barcode')).toBeVisible();

  api.set({ 'GET /api/spaces': data.spacesWith(['values']) });
  await open(page, '/scan');
  await expect(page.getByRole('radio', { name: 'Audit' })).toHaveCount(0);
  await page.getByRole('radio', { name: 'Add' }).check();
  await page.getByLabel('Or type the label code').fill(data.product().barcode);
  await page.getByRole('button', { name: 'Go' }).click();
  await expect(page.getByRole('button', { name: /^Add it/ })).toHaveCount(0);
  expect(api.requests.filter((request) => request.includes('/barcodes/'))).toEqual([]);
});

test('the getting-started card ticks steps, removes examples and can be dismissed', async ({ page, api }) => {
  await open(page, '/find');
  const card = page.getByRole('region', { name: 'Getting started' });
  await expect(card.getByText('2 of 5 done')).toBeVisible();
  await expect(card.getByRole('listitem').filter({ hasText: 'Add your places' })).toContainText('Done: Add your places');
  await expect(card.getByRole('listitem').filter({ hasText: 'Try a photo' })).not.toContainText('Done:');

  api.set({ 'GET /api/spaces/*/getting-started': data.gettingStarted({ examplesLeft: 0 }) });
  await card.getByRole('button', { name: 'Remove examples (2)' }).click();
  await expect(card.getByRole('button', { name: /^Remove examples/ })).toHaveCount(0);
  expect(api.requests).toContain(`DELETE /api/spaces/${data.spaceIds.home}/examples`);

  await card.getByRole('button', { name: 'Dismiss' }).click();
  await expect(card).toHaveCount(0);
  expect(api.requests).toContain(`POST /api/spaces/${data.spaceIds.home}/getting-started/dismiss`);
});

test('the getting-started card hides once every step is done, and while searching', async ({ page, api }) => {
  const all = data.gettingStarted().steps.map((step) => ({ ...step, done: true }));
  api.set({ 'GET /api/spaces/*/getting-started': data.gettingStarted({ steps: all }) });
  await open(page, '/find');
  await page.getByText('Recently changed').waitFor();
  await expect(page.getByRole('region', { name: 'Getting started' })).toHaveCount(0);

  api.set({ 'GET /api/spaces/*/getting-started': data.gettingStarted() });
  await open(page, '/find?q=baseball');
  await page.getByText('2 items found').waitFor();
  await expect(page.getByRole('region', { name: 'Getting started' })).toHaveCount(0);
});

test('a viewer sees the card without Remove examples', async ({ page, api }) => {
  api.set({ 'GET /api/spaces': data.spaces().map((space) => ({ ...space, role: 'Viewer' })) });
  await open(page, '/find');
  const card = page.getByRole('region', { name: 'Getting started' });
  await expect(card.getByRole('button', { name: 'Dismiss' })).toBeVisible();
  await expect(card.getByRole('button', { name: /^Remove examples/ })).toHaveCount(0);
});

test("empty tabs use the kind's hints, and general text without one", async ({ page, api }) => {
  api.set({ 'GET /api/spaces/*/items': { items: [], next: null }, 'GET /api/spaces/*/inbox': { items: [], next: null } });
  await open(page, '/browse');
  await expect(page.getByText('Start with your rooms, then add')).toBeVisible();

  api.set({ 'GET /api/spaces': data.spaces().map((space) => ({ ...space, preset: null })) });
  await open(page, '/browse');
  await expect(page.getByText('Start with the big places')).toBeVisible();
});

test('an owner changes the kind and features in Settings, Kind and features', async ({ page, api }) => {
  await open(page, '/settings/space');
  await expect(page.getByRole('heading', { name: 'Kind and features' })).toBeVisible();

  const features = page.waitForRequest((request) => request.method() === 'PUT' && request.url().endsWith('/features'));
  await page.getByRole('checkbox', { name: /^Audits/ }).uncheck();
  // Lending isn't offered yet, but stays on.
  expect((await features).postDataJSON()).toEqual({ features: ['values', 'barcodes', 'lending'] });
  await expect(page.getByRole('checkbox', { name: /^Audits/ })).not.toBeChecked();

  const kind = page.waitForRequest((request) => request.method() === 'PUT' && request.url().endsWith('/kind'));
  await page.getByLabel("What it's for").selectOption('collection');
  expect((await kind).postDataJSON()).toEqual({ kind: 'collection' });

  await page.getByRole('button', { name: 'Remove examples (2)' }).click();
  expect(api.requests).toContain(`DELETE /api/spaces/${data.spaceIds.home}/examples`);
});

test('Settings, Kind and features brings getting started back once dismissed', async ({ page, api }) => {
  api.set({ 'GET /api/spaces/*/getting-started': data.gettingStarted({ dismissed: true }) });
  await open(page, '/settings/space');
  await page.getByRole('button', { name: 'Show getting started again' }).click();
  await expect(page.getByRole('button', { name: 'Show getting started again' })).toHaveCount(0);
  expect(api.requests).toContain(`POST /api/spaces/${data.spaceIds.home}/getting-started/show`);
});

test('a viewer sees the kind and features in Settings but cannot change them', async ({ page, api }) => {
  api.set({ 'GET /api/spaces': data.spaces().map((space) => ({ ...space, role: 'Viewer' })) });
  await open(page, '/settings/space');
  await expect(page.getByText('Only an owner of Home can change these.')).toBeVisible();
  await expect(page.getByRole('checkbox', { name: /^Audits/ })).toBeDisabled();
  await expect(page.getByLabel("What it's for")).toBeDisabled();
  await expect(page.getByRole('button', { name: /^Remove examples/ })).toHaveCount(0);
});

test('Types adds a type from another pack', async ({ page }) => {
  await open(page, '/types');
  await page.getByRole('button', { name: 'Add from another pack' }).click();
  const dialog = page.getByRole('dialog', { name: 'Add from another pack' });
  // Starts on a pack other than the space's own.
  await expect(dialog.getByRole('combobox', { name: /^Pack/ })).toHaveValue('hobby');
  const added = page.waitForRequest((request) => request.method() === 'POST' && request.url().endsWith('/types/from-pack'));
  await dialog.getByRole('button', { name: 'Add Kit' }).click();
  expect((await added).postDataJSON()).toEqual({ pack: 'hobby', type: 'Kit' });
  await expect(page.getByRole('heading', { name: 'Kit' })).toBeVisible();

  await dialog.getByRole('combobox', { name: /^Pack/ }).selectOption('home');
  await expect(dialog.getByRole('listitem').filter({ hasText: 'Tool' })).toContainText('Already here');
});
