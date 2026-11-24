namespace Invy.Core.Managers.Contracts.Messages;

/// <summary>Starts an uploaded import with its columns mapped.</summary>
/// <param name="Mapping">Where each column goes. Columns left out are ignored. One column must be the name.</param>
/// <param name="ParentId">The container everything goes into (locations nest inside it), or null for the top.</param>
/// <param name="TypeId">The type for rows that don't name one, whose fields columns can fill, or null for none.</param>
public sealed record StartImportRequest(IReadOnlyList<ColumnMapping> Mapping, Guid? ParentId, Guid? TypeId);
