// The documentation is built into Invy and served at /help (docs/ in the repository), so these links work on a home
// network with no internet. A page is its path under docs/ without ".md", with an optional #section:
// helpUrl('admin/settings#backups') is '/help/admin/settings/#backups'.
export function helpUrl(page = '') {
  const [path, section] = page.split('#');
  const trimmed = path.replace(/^\/+|\/+$/g, '');
  return `/help/${trimmed ? `${trimmed}/` : ''}${section ? `#${section}` : ''}`;
}
