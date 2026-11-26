// Every screen of the page, checked with axe in each project of playwright.config.js (light, dark, phone).
// To cover a new screen or state, add a row to VIEWS: where to go, any answers to change, and what to click first.
import { test, expect, open, reply } from './fixtures.js';
import { expectAccessible } from './axe.js';
import * as data from './data.js';

const signedOut = { 'GET /api/session': { setupRequired: false, user: null } };
const asMember = { 'GET /api/session': data.session(data.member()) };

/** { name, path, answers?: routes for api.set(), act?: async (page, api) => {} run before checking } */
/** Once an AI model is set up, every AI task is offered. */
const ai = { 'GET /api/ai/features': data.aiReady() };

/** Chooses a small photo in the dialog's file box. */
const choosePhoto = (page) => page.getByLabel(/^Photos/).setInputFiles({ name: 'box.png', mimeType: 'image/png', buffer: Buffer.from(data.TINY_PNG, 'base64') });

const VIEWS = [
  // Before signing in
  { name: 'sign in', path: '/', answers: signedOut },
  { name: 'sign in, with the theme button', path: '/', answers: signedOut,
    act: (page) => page.getByRole('button', { name: /^Switch to (light|dark) mode$/ }).click() },
  { name: 'join from an invite', path: '/?invite=invite-token', answers: signedOut },
  { name: 'choose a new password', path: '/?reset=reset-token', answers: signedOut },
  { name: 'setup: create the account', path: '/', answers: { 'GET /api/session': { setupRequired: true, user: null } } },

  // Find
  { name: 'find: recently changed', path: '/find', act: (page) => page.getByText('Recently changed').waitFor() },
  { name: 'find: nothing in the space yet', path: '/find',
    answers: { 'GET /api/spaces/*/search': { results: [], total: 0, totalIsEstimate: false, next: null } } },
  { name: 'find: no space yet', path: '/find', answers: { 'GET /api/spaces': [] } },
  { name: 'find for a member', path: '/find', answers: asMember },
  { name: 'find: the getting-started card', path: '/find', act: (page) => page.getByText('2 of 5 done').waitFor() },
  { name: 'find: results from the address', path: '/find?q=baseball', act: (page) => page.getByText('2 items found').waitFor() },
  {
    name: 'find: typing a search', path: '/find',
    act: async (page) => {
      await page.getByRole('searchbox').fill('baseball');
      await page.getByText('2 items found').waitFor();
    },
  },
  {
    name: 'find: a search that cannot be read yet', path: '/find',
    act: async (page) => {
      await page.getByRole('searchbox').fill('type:car');
      await page.getByText('There is no type called car.').waitFor();
    },
  },
  {
    name: 'find: nothing found', path: '/find',
    act: async (page) => { await page.getByRole('searchbox').fill('nothing here'); await page.getByText('Nothing found').waitFor(); },
  },
  {
    name: 'find: scrolled down a long list', path: '/find?q=part',
    act: async (page) => {
      await page.getByText('120 items found').waitFor();
      // Each scroll to the end asks for the next page, as a person scrolling would.
      for (let page2 = 0; page2 < 6 && !await page.getByText('Part 120').isVisible(); page2++) {
        await page.evaluate(() => window.scrollTo(0, document.body.scrollHeight));
        await page.waitForTimeout(250);
      }
      await page.getByText('Part 120').waitFor();
      await expect(page.getByRole('listitem').filter({ hasText: 'Part 001' })).toHaveCount(0);
    },
  },
  {
    name: 'dark mode chosen with the sun and moon button', path: '/find',
    act: async (page) => {
      const before = await page.evaluate(() => matchMedia('(prefers-color-scheme: dark)').matches);
      // In the dark project this picks light, so both themes are also checked when chosen rather than followed.
      await page.getByRole('button', { name: before ? 'Switch to light mode' : 'Switch to dark mode' }).click();
    },
  },

  // Spaces
  { name: 'space picker: new space dialog', path: '/find', act: (page) => page.getByRole('button', { name: 'New space' }).click() },
  { name: "new space dialog: what you'll use", path: '/find',
    act: async (page) => {
      await page.getByRole('button', { name: 'New space' }).click();
      await page.getByRole('button', { name: 'Next' }).click();
      await page.getByRole('checkbox', { name: /^Values/ }).waitFor();
    } },
  { name: 'new space dialog: places', path: '/find',
    act: async (page) => {
      await page.getByRole('button', { name: 'New space' }).click();
      await page.getByRole('button', { name: 'Next' }).click();
      await page.getByRole('button', { name: 'Next' }).click();
      await page.getByLabel('Your own place').fill('Spare room');
      await page.getByRole('button', { name: 'Add', exact: true }).click();
      await page.getByRole('checkbox', { name: 'Garage' }).check();
    } },
  { name: 'types: no space yet', path: '/types', answers: { 'GET /api/spaces': [] } },
  { name: 'people: no space yet', path: '/people', answers: { 'GET /api/spaces': [] } },

  // Browse and items
  { name: 'browse: the top of a space', path: '/browse', act: (page) => page.getByText('Mystery cable').waitFor() },
  { name: 'browse: nothing yet', path: '/browse',
    answers: { 'GET /api/spaces/*/items': { items: [], next: null }, 'GET /api/spaces/*/inbox': { items: [], next: null } } },
  { name: 'browse: a type suggested for an Inbox item', path: '/browse',
    answers: { ...ai, 'GET /api/spaces/*/inbox': { items: [{ ...data.inboxItems().items[0], suggestedTypeId: '00000000-0000-4000-8000-000000000d03' }], next: null } },
    act: (page) => page.getByRole('button', { name: 'Accept' }).waitFor() },
  { name: 'browse: a new item', path: '/browse', act: (page) => page.getByRole('button', { name: 'New item' }).click() },
  { name: 'item: a box with things inside', path: `/browse/${data.itemIds.f}`, act: (page) => page.getByText('Where it has been').waitFor() },
  { name: 'item: a tool with fields', path: `/browse/${data.itemIds.drill}`, act: (page) => page.getByText('DeWalt').first().waitFor() },
  { name: 'item: photos and files', path: `/browse/${data.itemIds.drill}`, act: (page) => page.getByText('DeWalt manual.pdf').waitFor() },
  { name: 'item: a counted thing', path: `/browse/${data.itemIds.screws}`, act: (page) => page.getByRole('button', { name: 'One more' }).waitFor() },
  { name: 'item: editing', path: `/browse/${data.itemIds.drill}`, act: (page) => page.getByRole('button', { name: 'Edit' }).click() },
  { name: 'item: moving', path: `/browse/${data.itemIds.f}`,
    act: async (page) => { await page.getByRole('button', { name: 'Move' }).click(); await page.getByRole('button', { name: /^Move to|^Already in/ }).waitFor(); } },
  { name: 'item: as a viewer', path: `/browse/${data.itemIds.f}`,
    answers: { 'GET /api/spaces': data.spaces().map((space) => ({ ...space, role: 'Viewer' })) },
    act: (page) => page.getByText('Where it has been').waitFor() },

  { name: 'item: a box in a space with audits and values off', path: `/browse/${data.itemIds.f}`, answers: { 'GET /api/spaces': data.spacesWith([]) },
    act: (page) => page.getByText('Where it has been').waitFor() },
  { name: 'item: auditing a box', path: `/browse/${data.itemIds.f}`,
    act: async (page) => { await page.getByRole('button', { name: 'Audit' }).click(); await page.getByText('0 of 3 ticked').waitFor(); } },
  { name: "item: an audit's differences", path: `/browse/${data.itemIds.f}`,
    act: async (page) => {
      await page.getByRole('button', { name: 'Audit' }).click();
      await page.getByRole('button', { name: 'Finish the audit' }).click();
      await page.getByText('Found again').waitFor();
    } },
  { name: 'item: adding from a photo', path: `/browse/${data.itemIds.f}`, answers: ai,
    act: (page) => page.getByRole('button', { name: 'Add from a photo' }).click().then(() => page.getByRole('dialog').waitFor()) },
  { name: 'item: what the AI model saw in a photo', path: `/browse/${data.itemIds.f}`, answers: ai,
    act: async (page) => {
      await page.getByRole('button', { name: 'Add from a photo' }).click();
      await choosePhoto(page);
      await page.getByRole('button', { name: 'Look at the photo' }).click();
      await page.getByRole('button', { name: 'Add 3 items' }).waitFor();
    } },
  { name: "item: the AI model saw nothing", path: `/browse/${data.itemIds.f}`,
    answers: { ...ai, 'POST /api/spaces/*/items/from-photo': { model: 'Claude', items: [] } },
    act: async (page) => {
      await page.getByRole('button', { name: 'Add from a photo' }).click();
      await choosePhoto(page);
      await page.getByRole('button', { name: 'Look at the photo' }).click();
      await page.getByText("didn't see anything").waitFor();
    } },
  { name: 'item: reading a label', path: `/browse/${data.itemIds.drill}`, answers: ai,
    act: async (page) => {
      await page.getByRole('button', { name: 'Read a label' }).click();
      await choosePhoto(page);
      await page.getByRole('button', { name: 'Read it' }).click();
      await page.getByRole('button', { name: /^Fill in/ }).waitFor();
    } },
  { name: 'item: sticking on a label', path: `/browse/${data.itemIds.drill}`,
    act: (page) => page.getByRole('button', { name: 'Stick on a label' }).click().then(() => page.getByRole('dialog').waitFor()) },

  // Scan and labels
  { name: 'scan: open', path: '/scan', act: (page) => page.getByRole('button', { name: 'Start the camera' }).waitFor() },
  { name: 'scan: an unclaimed label from its address', path: `/l/${data.labelCodes.unclaimed}`,
    act: (page) => page.getByText(`Label ${data.labelCodes.unclaimed} isn't on anything yet`).waitFor() },
  {
    name: 'scan: claiming labels into the sticky place', path: '/scan',
    act: async (page) => {
      await page.getByRole('radio', { name: 'Claim' }).check();
      await page.getByLabel('Or type the label code').fill(data.labelCodes.unclaimed);
      await page.getByRole('button', { name: 'Go' }).click();
      await page.getByText(`Made Box ${data.labelCodes.unclaimed}`).waitFor();
    },
  },
  {
    name: 'scan: picking the sticky place', path: '/scan',
    act: async (page) => {
      await page.getByRole('radio', { name: 'Claim' }).check();
      await page.getByRole('button', { name: 'Change' }).click();
      await page.getByRole('button', { name: 'Add to the top' }).waitFor();
    },
  },
  {
    name: 'scan: move, waiting for where it goes', path: '/scan',
    act: async (page) => {
      await page.getByRole('radio', { name: 'Move' }).check();
      await page.getByLabel('Or type the label code').fill(data.labelCodes.onBoxF);
      await page.getByRole('button', { name: 'Go' }).click();
      await page.getByText('Now scan where it is going').waitFor();
    },
  },
  {
    name: 'scan: adding to a scanned box', path: '/scan',
    act: async (page) => {
      await page.getByRole('radio', { name: 'Add' }).check();
      await page.getByLabel('Or type a label code or barcode').fill(data.labelCodes.onBoxF);
      await page.getByRole('button', { name: 'Go' }).click();
      await page.getByLabel('Quick add to Box F').waitFor();
    },
  },
  {
    name: 'scan: a product found from its barcode', path: '/scan',
    act: async (page) => {
      await page.getByRole('radio', { name: 'Add' }).check();
      await page.getByLabel('Or type a label code or barcode').fill(data.product().barcode);
      await page.getByRole('button', { name: 'Go' }).click();
      await page.getByRole('button', { name: 'Add it at the top' }).click();
      await page.getByRole('dialog').waitFor();
    },
  },
  { name: 'scan: audits and barcodes off', path: '/scan', answers: { 'GET /api/spaces': data.spacesWith(['values']) },
    act: (page) => page.getByRole('radio', { name: 'Add' }).check() },
  { name: 'scan: as a viewer', path: '/scan', answers: { 'GET /api/spaces': data.spaces().map((space) => ({ ...space, role: 'Viewer' })) } },
  { name: 'scan: no space yet', path: '/scan', answers: { 'GET /api/spaces': [] } },
  { name: 'settings: labels and printing', path: '/settings/labels' },
  {
    name: 'settings: a sheet of labels ready to print', path: '/settings/labels',
    act: async (page) => {
      await page.getByRole('button', { name: /^Make 30 labels/ }).click();
      await page.getByRole('button', { name: 'Print', exact: true }).waitFor();
    },
  },
  {
    name: 'settings: unclaimed labels on a small sheet', path: '/settings/labels',
    act: async (page) => {
      await page.getByLabel('Sticker sheet').selectOption('avery-l7651');
      await page.getByRole('button', { name: 'Print the unclaimed labels again' }).click();
      await page.getByText('12 unclaimed labels').waitFor();
    },
  },

  // Activity
  { name: 'views: the list', path: '/views', act: (page) => page.getByText('Tools by power').waitFor() },
  { name: 'views: none yet', path: '/views', answers: { 'GET /api/spaces/*/views': [] } },
  { name: 'views: a table', path: `/views/${data.viewIds.notSeen}`, act: (page) => page.getByRole('cell', { name: 'DeWalt' }).waitFor() },
  { name: 'views: cards', path: `/views/${data.viewIds.cards}`, act: (page) => page.getByRole('list', { name: 'Everything in the garage' }).waitFor() },
  { name: 'views: a tree', path: `/views/${data.viewIds.tree}`, act: (page) => page.getByRole('link', { name: 'Box F' }).waitFor() },
  { name: 'views: a board', path: `/views/${data.viewIds.board}`, act: (page) => page.getByRole('region', { name: /^Cordless/ }).waitFor() },
  { name: 'views: a timeline', path: `/views/${data.viewIds.timeline}`, act: (page) => page.getByText('took 15 from').waitFor() },
  { name: "views: someone else's shared view", path: `/views/${data.viewIds.mine}`, act: (page) => page.getByRole('button', { name: 'Save a copy' }).waitFor() },
  { name: 'views: a search that no longer reads', path: `/views/${data.viewIds.notSeen}`,
    answers: { 'GET /api/spaces/*/views/*/results': reply(400, { detail: 'There is no field called brand. Change the search.' }) },
    act: (page) => page.getByText('There is no field called brand').waitFor() },
  { name: 'views: making one', path: '/views', act: (page) => page.getByRole('button', { name: 'New view' }).click() },
  { name: 'views: changing a board', path: `/views/${data.viewIds.board}`, act: (page) => page.getByRole('button', { name: 'Change' }).click() },
  { name: 'views: reports and value totals', path: '/views', act: (page) => page.getByRole('cell', { name: 'Not in a place' }).waitFor() },
  { name: 'views: no reports with values and audits off', path: '/views', answers: { 'GET /api/spaces': data.spacesWith([]) },
    act: (page) => page.getByText('Tools by power').waitFor() },
  // Space features (phase 6), every one switched on.
  { name: 'views: the feature lists', path: '/views', answers: { 'GET /api/spaces': data.spacesWith(data.allFeatures) },
    act: (page) => page.getByRole('link', { name: 'Running low' }).waitFor() },
  { name: 'lists: running low', path: '/views/low-stock', answers: { 'GET /api/spaces': data.spacesWith(data.allFeatures) },
    act: (page) => page.getByText('12 left').waitFor() },
  { name: 'lists: running low, nothing', path: '/views/low-stock',
    answers: { 'GET /api/spaces': data.spacesWith(data.allFeatures), 'GET /api/spaces/*/low-stock': { items: [], more: false } },
    act: (page) => page.getByText('Nothing is running low').waitFor() },
  { name: 'lists: lent out', path: '/views/loans', answers: { 'GET /api/spaces': data.spacesWith(data.allFeatures) },
    act: (page) => page.getByText('with Sam').waitFor() },
  { name: 'lists: coming up', path: '/views/reminders', answers: { 'GET /api/spaces': data.spacesWith(data.allFeatures) },
    act: (page) => page.getByText('Next service').waitFor() },
  { name: 'lists: moving', path: '/views/moving', answers: { 'GET /api/spaces': data.spacesWith(data.allFeatures) },
    act: (page) => page.getByRole('link', { name: 'Manifest' }).waitFor() },
  { name: 'lists: a box manifest', path: `/views/manifest?box=${data.itemIds.f}`, answers: { 'GET /api/spaces': data.spacesWith(data.allFeatures) },
    act: (page) => page.getByText('Goes to:').waitFor() },
  { name: 'lists: projects', path: '/views/projects', answers: { 'GET /api/spaces': data.spacesWith(data.allFeatures) },
    act: (page) => page.getByRole('heading', { name: 'Bookshelf' }).waitFor() },
  { name: 'lists: a feature switched off', path: '/views/loans', answers: { 'GET /api/spaces': data.spacesWith([]) },
    act: (page) => page.getByText('That list is switched off').waitFor() },
  { name: 'item: low stock and set aside', path: `/browse/${data.itemIds.screws}`,
    answers: { 'GET /api/spaces': data.spacesWith(data.allFeatures), 'GET /api/spaces/*/items/*': data.lowScrews(), 'GET /api/spaces/*/items/*/projects': data.projects() },
    act: (page) => page.getByText('Low stock at 20.').waitFor() },
  { name: 'item: lent out', path: `/browse/${data.itemIds.drill}`,
    answers: { 'GET /api/spaces': data.spacesWith(data.allFeatures), 'GET /api/spaces/*/items/*': data.lentDrill() },
    act: (page) => page.getByText('Lent to').waitFor() },
  { name: 'item: lending', path: `/browse/${data.itemIds.drill}`, answers: { 'GET /api/spaces': data.spacesWith(data.allFeatures) },
    act: (page) => page.getByRole('button', { name: 'Lend' }).click() },
  { name: 'item: setting aside for a project', path: `/browse/${data.itemIds.screws}`, answers: { 'GET /api/spaces': data.spacesWith(data.allFeatures) },
    act: async (page) => { await page.getByRole('button', { name: 'Set aside for a project' }).click(); await page.getByRole('dialog').getByLabel('Project').waitFor(); } },
  { name: 'item: a packed box', path: `/browse/${data.itemIds.f}`,
    answers: { 'GET /api/spaces': data.spacesWith(data.allFeatures), 'GET /api/spaces/*/items/*': (request) => (request.url().endsWith(data.itemIds.f) ? data.packedBox() : data.itemDetail(data.itemIds.f)),
      'POST /api/spaces/*/items/*/seen': data.packedBox().item },
    act: (page) => page.getByRole('button', { name: 'Arrived' }).waitFor() },
  { name: 'settings: import', path: '/settings/import', act: (page) => page.getByText('1 row was skipped').waitFor() },
  { name: 'settings: import, checking the columns', path: '/settings/import',
    act: async (page) => {
      await page.getByLabel('CSV file').setInputFiles({ name: 'homebox-export.csv', mimeType: 'text/csv', buffer: Buffer.from('HB.name\nSander\n') });
      await page.getByRole('button', { name: 'Upload and check' }).click();
      await page.getByRole('cell', { name: 'Sander' }).waitFor();
    } },
  { name: 'settings: the calendar feed on', path: '/settings/space', answers: { 'GET /api/spaces/*/calendar': data.calendarFeed() },
    act: (page) => page.getByLabel('Address to subscribe to').waitFor() },
  { name: 'types: a date field that reminds', path: '/types', answers: { 'GET /api/spaces': data.spacesWith(data.allFeatures) },
    act: async (page) => {
      await page.getByRole('button', { name: 'New type' }).click();
      await page.getByRole('button', { name: 'Add a field' }).click();
      await page.getByLabel('Kind').selectOption('Date');
      await page.getByLabel('Remind, days ahead').fill('30');
    } },
  { name: 'reports: insurance inventory', path: '/views/insurance', act: (page) => page.getByRole('heading', { name: /^Garage/ }).waitFor() },
  { name: 'reports: insurance inventory, nothing yet', path: '/views/insurance',
    answers: { 'GET /api/spaces/*/reports/insurance': { madeAt: '2026-11-24T10:00:00Z', total: 0, count: 0, truncated: false, rooms: [] } } },
  { name: 'reports: missing items', path: '/views/missing', act: (page) => page.getByRole('cell', { name: 'Never' }).waitFor() },
  { name: 'reports: nothing missing', path: '/views/missing',
    answers: { 'GET /api/spaces/*/reports/missing': { results: [], total: 0, totalIsEstimate: false, next: null } } },
  { name: 'find: a pinned view', path: '/find', act: (page) => page.getByRole('heading', { name: 'Not seen in a year' }).waitFor() },
  { name: 'activity: the feed', path: '/activity', act: (page) => page.getByText('took 15 from').waitFor() },
  { name: 'activity: nothing yet', path: '/activity', answers: { 'GET /api/spaces/*/activity': { events: [], next: null } } },

  // Types
  { name: 'types: the list', path: '/types', act: (page) => page.getByRole('heading', { name: 'Tool' }).waitFor() },
  { name: 'types: none yet', path: '/types', answers: { 'GET /api/spaces/*/types': [] } },
  { name: 'types: adding from another pack', path: '/types',
    act: (page) => page.getByRole('button', { name: 'Add from another pack' }).click().then(() => page.getByRole('dialog').waitFor()) },
  {
    name: 'types: editing a type', path: '/types',
    act: (page) => page.getByRole('button', { name: 'Edit Tool' }).click(),
  },
  {
    name: 'types: a new type with a choice field', path: '/types',
    act: async (page) => {
      await page.getByRole('button', { name: 'New type' }).click();
      await page.getByRole('button', { name: 'Add a field' }).click();
      await page.getByLabel('Kind').selectOption('Select');
    },
  },
  {
    name: 'types: suggested fields', path: '/types', answers: ai,
    act: async (page) => {
      await page.getByRole('button', { name: 'New type' }).click();
      await page.getByLabel('Name', { exact: true }).fill('Lego sets');
      await page.getByRole('button', { name: 'Suggest fields' }).click();
      await page.getByRole('button', { name: 'Add the ticked fields' }).waitFor();
    },
  },
  {
    name: 'types: as a viewer', path: '/types',
    answers: { 'GET /api/spaces': data.spaces().map((space) => ({ ...space, role: 'Viewer' })) },
    act: (page) => page.getByRole('button', { name: 'View Tool' }).click(),
  },

  // People
  { name: 'people: as an owner', path: '/people', act: (page) => page.getByText('Jo Park', { exact: true }).waitFor() },
  {
    name: 'people: as a viewer', path: '/people',
    answers: { 'GET /api/spaces': data.spaces().map((space) => ({ ...space, role: 'Viewer' })) },
    act: (page) => page.getByText('Jo Park', { exact: true }).waitFor(),
  },

  // Settings
  { name: 'settings: login', path: '/settings/login' },
  {
    name: 'settings: login, asking before signing out everywhere', path: '/settings/login',
    act: async (page) => {
      await page.getByRole('button', { name: 'Sign out everywhere' }).click();
      await page.getByRole('dialog').waitFor();
    },
  },
  { name: 'settings: this space, as an owner', path: '/settings/space', act: (page) => page.getByRole('button', { name: 'Remove examples (2)' }).waitFor() },
  { name: 'settings: this space, as a viewer', path: '/settings/space',
    answers: { 'GET /api/spaces': data.spaces().map((space) => ({ ...space, role: 'Viewer' })), 'GET /api/spaces/*/getting-started': data.gettingStarted({ dismissed: true }) },
    act: (page) => page.getByRole('button', { name: 'Show getting started again' }).waitFor() },
  { name: 'settings: appearance', path: '/settings/appearance' },
  {
    // Phones only: the sidebar opened from the menu button. Wide screens show it all the time.
    name: 'settings: the sections menu open', path: '/settings/login',
    act: async (page) => {
      const menu = page.getByRole('button', { name: /^Settings sections/ });
      if (await menu.isVisible()) await menu.click();
    },
  },
  {
    // Phones only: scrolled past the menu button, the button that brings it back shows in the corner.
    name: 'settings: scrolled past the sections menu', path: '/settings/assistants',
    act: async (page) => {
      const menu = page.getByRole('button', { name: /^Settings sections/ });
      if (!await menu.isVisible()) return;
      await page.evaluate(() => window.scrollTo(0, document.body.scrollHeight));
      await expect(page.getByRole('button', { name: 'Back to the settings menu' })).toBeVisible();
    },
  },
  { name: 'settings: https', path: '/settings/https' },
  { name: 'settings: notifications', path: '/settings/notifications' },
  {
    name: 'settings: notifications, saved by itself', path: '/settings/notifications',
    act: async (page) => {
      await page.getByLabel('Send daily reminders at').fill('08:15');
      await page.getByLabel('Send daily reminders at').blur();
      await page.getByRole('status').filter({ hasText: 'Saved' }).waitFor();
    },
  },
  {
    name: 'settings: notifications, a field that stops it saving', path: '/settings/notifications',
    act: async (page) => {
      await page.getByLabel('Send daily reminders at').fill('');
      await page.getByLabel('Send daily reminders at').blur();
      await page.getByText('Not saved yet').waitFor();
    },
  },
  {
    name: 'settings: notifications, a save that failed', path: '/settings/notifications',
    answers: { 'PUT /api/system/notifications': reply(500, { detail: 'The settings could not be saved. Try again in a minute.' }) },
    act: async (page) => {
      await page.getByLabel('Send daily reminders at').fill('08:15');
      await page.getByLabel('Send daily reminders at').blur();
      await page.getByRole('button', { name: 'Try again' }).waitFor();
    },
  },
  { name: 'settings: backups', path: '/settings/backups' },
  {
    name: 'settings: backups with one opened', path: '/settings/backups',
    act: (page) => page.locator('#settings-body').getByRole('button', { expanded: false }).first().click()
      .then(() => page.getByText('Accounts').waitFor()),
  },
  { name: 'settings: logs', path: '/settings/logs' },
  {
    name: 'settings: logs with a long entry opened', path: '/settings/logs',
    act: (page) => page.getByRole('button', { name: 'Show all' }).click(),
  },
  { name: 'settings: AI assistants with no tokens', path: '/settings/assistants' },
  { name: 'settings: AI assistants with models set up', path: '/settings/assistants', answers: { 'GET /api/ai/settings': data.aiSettings() } },
  {
    name: 'settings: editing a saved AI model', path: '/settings/assistants', answers: { 'GET /api/ai/settings': data.aiSettings() },
    act: async (page) => {
      await page.locator('summary').filter({ hasText: 'claude-opus-5-5' }).click();
      await page.getByLabel('API key', { exact: true }).waitFor();
    },
  },
  {
    name: 'settings: adding an Ollama model', path: '/settings/assistants',
    act: async (page) => {
      await page.getByLabel('Start from').selectOption('ollama');
      await page.getByRole('button', { name: 'Add a model' }).click();
      await page.getByText('not saved yet').waitFor();
    },
  },
  { name: 'settings: AI assistants with tokens', path: '/settings/assistants', answers: { 'GET /api/tokens': data.apiTokens() } },
  {
    name: 'settings: AI assistants with a new token shown', path: '/settings/assistants',
    act: async (page) => {
      await page.getByLabel(/Name for a new token/).fill('Workshop tablet');
      await page.getByRole('button', { name: 'Make a token' }).click();
      await page.getByLabel('Token', { exact: true }).waitFor();
    },
  },

  // Someone who isn't an admin sees fewer sections.
  { name: 'settings for a member', path: '/settings/https', answers: asMember },
  { name: 'AI assistants for a member', path: '/settings/assistants', answers: asMember },

  // Things laid over the page
  {
    name: 'the confirm dialog', path: '/settings/assistants', answers: { 'GET /api/tokens': data.apiTokens() },
    act: (page) => page.getByRole('button', { name: 'Revoke Claude Desktop' }).click().then(() => page.getByRole('dialog').waitFor()),
  },
  {
    name: 'a message in the bell', path: '/find', answers: { 'GET /api/inbox': data.inbox() },
    act: (page) => page.getByRole('button', { name: /^Notifications, 1 unread/ }).click()
      .then(() => page.getByRole('region', { name: 'Notifications' }).waitFor()),
  },
  {
    name: 'an error message', path: '/settings/assistants',
    answers: { 'POST /api/tokens': reply(500, { detail: 'The token could not be made. Try again in a minute.' }) },
    act: async (page) => {
      await page.getByLabel(/Name for a new token/).fill('Workshop tablet');
      await page.getByRole('button', { name: 'Make a token' }).click();
      await page.getByText('The token could not be made').waitFor();
    },
  },
];

for (const view of VIEWS) {
  test(view.name, async ({ page, api }) => {
    if (view.answers) api.set(view.answers);
    await open(page, view.path);
    await view.act?.(page, api);
    await expectAccessible(page);
  });
}

test('setup wizard, every step', async ({ page, api }) => {
  // A new install has no spaces until the last step makes one.
  let made = [];
  api.set({
    'GET /api/session': { setupRequired: true, user: null },
    'POST /api/setup/account': data.admin(),
    'GET /api/spaces': () => made,
    'POST /api/spaces': (request) => {
      made = [{ ...data.spaces()[0], ...request.postDataJSON(), id: '00000000-0000-4000-8000-000000000c09', role: 'Owner' }];
      return made[0];
    },
  });
  await open(page, '/');
  await page.getByLabel('Your name').fill('Alex Rivera');
  await page.getByLabel('Username').fill('alex');
  await page.getByLabel(/^Password/).fill('correct horse');
  await page.getByRole('button', { name: 'Create account' }).click();

  // A welcome before moving on, rather than a jump straight to the next step.
  await page.getByRole('heading', { name: 'Welcome, Alex Rivera.' }).waitFor();
  await expectAccessible(page);
  await page.getByRole('button', { name: 'Continue' }).click();

  for (const step of ['Secure connection', 'Your space', "What you'll use", 'Places']) {
    await page.getByRole('heading', { name: step }).waitFor();
    await expect(page.getByRole('heading', { name: step })).toBeFocused();
    await expectAccessible(page);
    if (step === 'Places') {
      await page.getByRole('checkbox', { name: 'Kitchen' }).check();
      await page.getByRole('checkbox', { name: /^Add a few example items/ }).check();
      await expectAccessible(page);
    }
    await page.getByRole('button', { name: step === 'Places' ? 'Finish' : 'Next' }).click();
  }

  // The last step makes the space and opens Find in it.
  await expect(page).toHaveURL(/\/find$/);
  await expect(page.getByRole('navigation', { name: 'Views' })).toBeVisible();
  await expect(page.getByRole('heading', { name: 'Make your first space' })).toHaveCount(0);
  await expectAccessible(page);
});
