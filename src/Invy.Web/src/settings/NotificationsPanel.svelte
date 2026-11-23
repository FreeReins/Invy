<script>
  import { pendingClick } from '../lib/pending.js';
  import { app, run, toast } from '../lib/app.svelte.js';
  import { api } from '../lib/api.js';
  import { autosave } from '../lib/autosave.svelte.js';
  import SaveStatus from '../components/SaveStatus.svelte';

  const settings = $derived(app.notifications);

  let form;

  // Saved shortly after each change; a field is changed once it is left or Enter is pressed.
  const saver = autosave(async () => {
    const data = new FormData(form);
    const changes = { webhookUrl: data.get('webhookUrl'), dailyTime: data.get('dailyTime') };
    app.notifications = await api('PUT', '/api/system/notifications', { ...settings, ...changes });
  }, { validate: () => form.checkValidity() });

  function submit(event) {
    event.preventDefault();
    saver.retry();
  }

  const send = (path, describe) => run(async () => {
    toast(describe(await api('POST', path)));
  });

  const describeTest = (result) => (result.delivered ? 'The test notification was delivered.' : `The test wasn't delivered. ${result.problem}`);
  const describeRun = (result) => [
    result.sent.length
      ? `Sending: ${result.sent.join(', ')}. A webhook that can't be reached is tried again over the next half hour.`
      : 'Nothing needed sending today.',
    result.problems.length ? `Not delivered: ${result.problems.join('; ')}` : '',
  ].join(' ').trim();
</script>

<h2>Notifications</h2>
<form class="stack card" bind:this={form} onchange={saver.schedule} onsubmit={submit}>
  <p class="muted">Invy posts notifications as JSON to a webhook, such as a Home Assistant webhook, which can turn them into phone notifications or announcements.</p>
  <label>Webhook address (leave empty to turn notifications off)
    <input id="notify-url" name="webhookUrl" type="url" value={settings.webhookUrl} placeholder="http://homeassistant.local:8123/api/webhook/invy">
  </label>
  <label>Send daily reminders at
    <input id="notify-time" name="dailyTime" type="time" required value={settings.dailyTime}>
  </label>
  <p class="muted">Reminders for warranties, upkeep, lent items and low stock come in a later version of Invy.</p>
  <SaveStatus {saver} />
  <div class="row-actions">
    <button type="button" onclick={pendingClick(() => send('/api/system/notifications/test', describeTest))}>Send a test</button>
    <button type="button" onclick={pendingClick(() => send('/api/system/notifications/send-today', describeRun))}>Send today's reminders now</button>
  </div>
</form>
