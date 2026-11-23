<script>
  import { app, enterApp } from '../lib/app.svelte.js';
  import { api } from '../lib/api.js';
  import AccountForm from './AccountForm.svelte';

  async function submit(account) {
    app.session.user = await api('POST', '/api/account/accept-invite', { token: app.linkToken, ...account });
    await enterApp();
  }
</script>

<AccountForm
  title="Join Invy"
  intro="You have been invited to Invy. Create an account to find and keep track of your things."
  button="Join"
  {submit} />
