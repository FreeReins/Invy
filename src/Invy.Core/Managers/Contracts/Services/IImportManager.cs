using Invy.Core.Managers.Contracts.Messages;

namespace Invy.Core.Managers.Contracts.Services;

/// <summary>
/// Imports from CSV files and Homebox's export: upload, check the columns, start, and watch it run in the background.
/// </summary>
public interface IImportManager
{
    /// <summary>Keeps an uploaded file and suggests where each column goes. Nothing is added until it is started.</summary>
    Task<ImportResponse> UploadAsync(Guid userId, Guid spaceId, UploadImportRequest request, CancellationToken cancellationToken);

    /// <summary>The space's imports, newest first.</summary>
    Task<IReadOnlyList<ImportResponse>> ListImportsAsync(Guid userId, Guid spaceId, CancellationToken cancellationToken);

    /// <summary>One import, with its file's first rows while it has one.</summary>
    Task<ImportResponse> GetImportAsync(Guid userId, Guid spaceId, Guid id, CancellationToken cancellationToken);

    /// <summary>Starts an uploaded import with its columns mapped. It runs in the background.</summary>
    Task<ImportResponse> StartImportAsync(Guid userId, Guid spaceId, Guid id, StartImportRequest request, CancellationToken cancellationToken);

    /// <summary>Removes an import that isn't running (a queued one stops before it starts), and its file. What it added stays.</summary>
    Task DeleteImportAsync(Guid userId, Guid spaceId, Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Runs one batch of a started import, carrying on from where it stopped, and queues the next batch in the same
    /// commit until the file is done.
    /// </summary>
    Task RunImportAsync(RunImport command, CancellationToken cancellationToken);
}
