using Invy.Core.Managers.Contracts;
using Invy.Core.Managers.Contracts.Messages;
using Invy.Core.Managers.Contracts.Services;
using Wolverine.Attributes;

namespace Invy.Api.Messaging;

/// <summary>
/// Runs a started import in the background. Safe to run twice: each batch is saved with how far it got, so a second
/// run carries on from there, and a finished import is left alone.
/// </summary>
[StickyHandler(MessageQueues.Imports)]
public static class RunImportHandler
{
    public static Task Handle(RunImport command, IImportManager imports, CancellationToken cancellationToken) =>
        imports.RunImportAsync(command, cancellationToken);
}
