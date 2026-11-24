using System.Text.RegularExpressions;
using Invy.Core.Managers.Contracts;
using Invy.Core.ResourceAccess.Contracts.Messages;
using Invy.Core.ResourceAccess.Contracts.Services;

namespace Invy.Core.Engines;

/// <summary>
/// Checks requests against Invy's rules before anything changes, and says in a plain sentence what is wrong.
/// Rules that depend on what is stored (does the account exist, is the username taken, is setup done) read it through
/// the resource accessors; this is the only engine that does.
/// </summary>
internal sealed partial class ValidationEngine(IAccountAccess accounts, ISpaceAccess spaces, ITypeAccess types, IItemAccess items, ILabelAccess labels, IAttachmentAccess attachments, IViewAccess views, IStarterPackAccess starterPacks, IBackupAccess backups)
{
    public const int MinimumPasswordLength = 8;

    /// <summary>The longest name an API token can have.</summary>
    public const int ApiTokenNameLength = 80;

    public const int InboxNoteLength = 500;

    private static readonly Regex ValidUsername = new("^[a-z0-9._-]{3,32}$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex ValidBackupName = new(@"^invy-backup-\d{8}-\d{6}\.json\.gz$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public async Task<UserAccount> UserAsync(Guid id, CancellationToken cancellationToken) =>
        await accounts.FindUserAsync(id, cancellationToken) ?? throw new NotFoundException("That account no longer exists.");

    public async Task<AccessLink> LinkAsync(Guid id, CancellationToken cancellationToken) =>
        await accounts.FindLinkAsync(id, cancellationToken) ?? throw new NotFoundException("That link no longer exists.");

    /// <summary>A backup's name, once it is known to be a real backup file and not an attempt to reach another file.</summary>
    public string ExistingBackup(string name) =>
        ValidBackupName.IsMatch(name ?? string.Empty) && backups.List().Any(file => file.Name == name)
            ? name!
            : throw new NotFoundException("There is no backup with that name.");

}
