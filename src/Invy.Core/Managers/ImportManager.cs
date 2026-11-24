using Invy.Core.Engines;
using Invy.Core.Engines.Contracts.Messages;
using Invy.Core.Engines.Transformation;
using Invy.Core.Managers.Contracts;
using Invy.Core.Managers.Contracts.Messages;
using Invy.Core.Managers.Contracts.Services;
using Invy.Core.ResourceAccess.Contracts.Messages;
using Invy.Core.ResourceAccess.Contracts.Services;

namespace Invy.Core.Managers;

/// <inheritdoc cref="IImportManager"/>
internal sealed class ImportManager(
    IImportAccess imports,
    IItemAccess items,
    ITypeAccess types,
    IFileStoreAccess files,
    IMessageAccess messages,
    ValidationEngine validation,
    ImportEngine importEngine,
    ItemEngine itemEngine,
    TimeProvider clock) : IImportManager
{
    /// <summary>How many rows one run reads, saved with how far the import got before the next run is queued.</summary>
    public const int BatchSize = 1000;

    /// <summary>The most problems an import keeps.</summary>
    public const int MaxProblems = 100;

    /// <summary>The most levels of containers one row's location makes.</summary>
    public const int MaxLocationLevels = 10;

    private const int SampleRows = 5;

    public async Task<ImportResponse> UploadAsync(Guid userId, Guid spaceId, UploadImportRequest request, CancellationToken cancellationToken)
    {
        await validation.SpaceRoleAsync(spaceId, userId, SpaceRole.Editor, cancellationToken);
        var rows = importEngine.Parse(request.Data ?? []);
        var kind = ImportEngine.KindOf(rows[0]);
        var stored = await files.SaveAsync(spaceId, request.Data!, ".csv", cancellationToken);
        var name = Path.GetFileName((request.Name ?? string.Empty).Trim());
        var import = new Import
        {
            SpaceId = spaceId,
            Kind = kind,
            FileName = name.Length is > 0 and <= 200 ? name : "import.csv",
            FilePath = stored.Path,
            Mapping = [.. importEngine.Suggest(rows[0], kind, null)],
            Status = ImportStatus.Draft,
            Total = rows.Count - 1,
            CreatedBy = userId,
            CreatedAt = clock.GetUtcNow(),
            Version = 1,
        };
        await imports.SaveAsync(import, cancellationToken);
        await messages.CommitAsync(cancellationToken);
        return Response(import, rows);
    }

    public async Task<IReadOnlyList<ImportResponse>> ListImportsAsync(Guid userId, Guid spaceId, CancellationToken cancellationToken)
    {
        await validation.SpaceRoleAsync(spaceId, userId, SpaceRole.Viewer, cancellationToken);
        return (await imports.ListAsync(spaceId, 50, cancellationToken)).Select(import => Response(import, null)).ToList();
    }

    public async Task<ImportResponse> GetImportAsync(Guid userId, Guid spaceId, Guid id, CancellationToken cancellationToken)
    {
        await validation.SpaceRoleAsync(spaceId, userId, SpaceRole.Viewer, cancellationToken);
        var import = await ImportAsync(spaceId, id, cancellationToken);
        return Response(import, await RowsAsync(import, cancellationToken));
    }

    public async Task<ImportResponse> StartImportAsync(Guid userId, Guid spaceId, Guid id, StartImportRequest request, CancellationToken cancellationToken)
    {
        await validation.SpaceRoleAsync(spaceId, userId, SpaceRole.Editor, cancellationToken);
        var import = await ImportAsync(spaceId, id, cancellationToken);
        if (import.Status != ImportStatus.Draft)
        {
            throw new RuleViolationException("This import has already been started. Upload the file again to import it again.");
        }

        var rows = await RowsAsync(import, cancellationToken)
            ?? throw new RuleViolationException("This import's file has gone. Upload it again.");
        var parent = await validation.ContainerAsync(spaceId, request.ParentId, cancellationToken);
        var type = await validation.ItemTypeAsync(spaceId, request.TypeId, cancellationToken);
        import.Mapping = [.. ImportEngine.Check(request.Mapping ?? [], rows[0].Length, type)];
        import.ParentId = parent?.Id;
        import.TypeId = type?.Id;
        import.Status = ImportStatus.Queued;
        import.Version++;
        await imports.SaveAsync(import, cancellationToken);
        await messages.PublishAsync(new RunImport(spaceId, import.Id));
        await messages.CommitAsync(cancellationToken);
        return Response(import, rows);
    }

    public async Task DeleteImportAsync(Guid userId, Guid spaceId, Guid id, CancellationToken cancellationToken)
    {
        await validation.SpaceRoleAsync(spaceId, userId, SpaceRole.Editor, cancellationToken);
        var import = await ImportAsync(spaceId, id, cancellationToken);
        // A queued import can go: if its run starts afterwards, it finds nothing to do.
        if (import.Status == ImportStatus.Running)
        {
            throw new RuleViolationException("This import is running. Wait for it to finish, then remove it.");
        }

        await imports.RemoveAsync(spaceId, id, cancellationToken);
        await messages.CommitAsync(cancellationToken);
        await DeleteFileAsync(import.Id, import.FilePath, cancellationToken);
    }

    public async Task RunImportAsync(RunImport command, CancellationToken cancellationToken)
    {
        var import = await imports.FindAsync(command.SpaceId, command.ImportId, cancellationToken);
        if (import is null || import.Status is not (ImportStatus.Queued or ImportStatus.Running))
        {
            return; // removed, not started, or already finished: nothing to do, so running twice is safe
        }

        var rows = await RowsAsync(import, cancellationToken);
        var parent = import.ParentId is Guid parentId ? await items.FindAsync(import.SpaceId, parentId, cancellationToken) : null;
        if (rows is null || (import.ParentId is not null && parent is null))
        {
            await FinishAsync(import, ImportStatus.Failed, rows is null ? "The import's file has gone. Upload it again." : "The container it was going into has been removed.", cancellationToken);
            return;
        }

        // One batch per run, carrying on after the rows already saved, and committed once with how far it got; the
        // next batch is queued in the same commit.
        var spaceTypes = await types.ListAsync(import.SpaceId, cancellationToken);
        var importType = spaceTypes.FirstOrDefault(type => type.Id == import.TypeId);
        var places = new Dictionary<string, Item>(StringComparer.CurrentCultureIgnoreCase);
        var batch = rows.Skip(import.Done + 1).Take(BatchSize).ToList();
        var now = clock.GetUtcNow();
        var added = new List<Item>();
        var events = new List<ItemEvent>();
        var values = new List<ItemValue>();
        var changed = new HashSet<Guid>();
        for (var i = 0; i < batch.Count; i++)
        {
            try
            {
                var row = importEngine.Read(batch[i], rows[0], import.Mapping, import.Kind);
                var type = row.TypeName is null
                    ? importType
                    : spaceTypes.FirstOrDefault(type => string.Equals(type.Name, row.TypeName, StringComparison.CurrentCultureIgnoreCase))
                      ?? throw new RuleViolationException($"There is no type called {row.TypeName}.");
                var container = await PlaceAsync(import, parent, row.Location, places, now, cancellationToken);
                var item = NewItem(import, row, type, container, now);
                added.Add(item);
                events.Add(ItemEngine.Event(item, import.CreatedBy, ItemEventKind.Created, now, to: container?.Id));
                values.AddRange(itemEngine.SortableValues(item, type));
                changed.UnionWith(ItemPaths.AncestorIds(item.Path));
            }
            catch (RuleViolationException problem)
            {
                if (import.Problems.Count < MaxProblems)
                {
                    import.Problems.Add(new ImportProblem(import.Done + i + 2, problem.Message));
                }
            }
        }

        await items.InsertManyAsync(added, events, values, cancellationToken);
        import.Done += batch.Count;
        import.Added += added.Count;
        if (changed.Count > 0)
        {
            await messages.PublishAsync(new ItemsChanged(import.SpaceId, changed.ToList()));
        }

        if (import.Done < import.Total && batch.Count > 0)
        {
            import.Status = ImportStatus.Running;
            import.Version++;
            await imports.SaveAsync(import, cancellationToken);
            await messages.PublishAsync(command);
            await messages.CommitAsync(cancellationToken);
            return;
        }

        await FinishAsync(import, ImportStatus.Done, null, cancellationToken);
    }

    /// <summary>
    /// The container a row's location names, inside the import's container: each level found by name, or made (with
    /// its Created event, saved in the batch's commit). Cached for the rest of the run.
    /// </summary>
    private async Task<Item?> PlaceAsync(
        Import import, Item? parent, IReadOnlyList<string> location, Dictionary<string, Item> places, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var current = parent;
        var key = string.Empty;
        foreach (var name in location.Take(MaxLocationLevels))
        {
            key += "\u0000" + name;
            if (!places.TryGetValue(key, out var found))
            {
                found = await items.FindContainerAsync(import.SpaceId, current?.Id, name, cancellationToken);
                if (found is null)
                {
                    found = new Item
                    {
                        SpaceId = import.SpaceId, Name = name.Length <= ValidationEngine.ItemNameLength ? name : name[..ValidationEngine.ItemNameLength],
                        HoldsItems = true, Status = ItemStatus.Active, CreatedAt = now, UpdatedAt = now, Version = 1,
                    };
                    ItemEngine.Place(found, current);
                    await items.SaveAsync(found, [], cancellationToken);
                    await items.AddEventAsync(ItemEngine.Event(found, import.CreatedBy, ItemEventKind.Created, now, to: current?.Id), cancellationToken);
                    import.Added++;
                }

                places[key] = found;
            }

            current = found;
        }

        return current;
    }

    /// <summary>A new item from a row, checked as the item form checks one. Field values that don't fit are skipped with the row.</summary>
    private Item NewItem(Import import, ImportRow row, ItemType? type, Item? container, DateTimeOffset now)
    {
        var fields = new Dictionary<Guid, System.Text.Json.JsonElement>();
        foreach (var (fieldId, text) in row.Fields)
        {
            var field = type?.Fields.FirstOrDefault(field => field.Id == fieldId);
            if (field is null)
            {
                continue; // a field of the import's type, on a row that names another type
            }

            fields[fieldId] = TransformationEngine.FieldValue(field, text)
                ?? throw new RuleViolationException($"\"{text}\" isn't a value {field.Label} can hold.");
        }

        var item = new Item { SpaceId = import.SpaceId, Status = ItemStatus.Active, CreatedAt = now, UpdatedAt = now, Version = 1 };
        ValidationEngine.ItemDetails(item, new ItemRequest(
            row.Name, row.Marking, type?.Id, null, null, row.Quantity, row.Unit, null, row.Tags, row.Notes, 0), type);
        item.Fields = fields;
        ItemEngine.Place(item, container);
        item.Value = itemEngine.Value(item, type);
        return item;
    }

    private async Task FinishAsync(Import import, ImportStatus status, string? problem, CancellationToken cancellationToken)
    {
        import.Status = status;
        import.FinishedAt = clock.GetUtcNow();
        if (problem is not null)
        {
            import.Problems.Insert(0, new ImportProblem(0, problem));
        }

        var file = import.FilePath;
        import.FilePath = null;
        import.Version++;
        await imports.SaveAsync(import, cancellationToken);
        await messages.CommitAsync(cancellationToken);
        await DeleteFileAsync(import.Id, file, cancellationToken);
    }

    /// <summary>Deletes an import's file, unless another import uploaded the same file and still needs it.</summary>
    private async Task DeleteFileAsync(Guid importId, string? file, CancellationToken cancellationToken)
    {
        if (file is string path && !await imports.FileInUseAsync(path, importId, cancellationToken))
        {
            files.Delete(path);
        }
    }

    private async Task<Import> ImportAsync(Guid spaceId, Guid id, CancellationToken cancellationToken) =>
        await imports.FindAsync(spaceId, id, cancellationToken)
        ?? throw new NotFoundException("That import no longer exists. It may have been removed; go back and reload.");

    /// <summary>The import file's rows, heading first, or null once its file has gone.</summary>
    private async Task<List<string[]>?> RowsAsync(Import import, CancellationToken cancellationToken) =>
        import.FilePath is string path && await files.ReadAsync(path, cancellationToken) is { } data ? importEngine.Parse(data) : null;

    private static ImportResponse Response(Import import, List<string[]>? rows) => new(
        import.Id, import.Kind, import.FileName, import.Status,
        rows?[0] ?? [],
        rows?.Skip(1).Take(SampleRows).Select(row => (IReadOnlyList<string>)row).ToList() ?? [],
        import.Mapping, import.ParentId, import.TypeId, import.Total, import.Done, import.Added, import.Problems, import.CreatedAt, import.FinishedAt);
}
