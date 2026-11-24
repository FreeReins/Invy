using Wolverine.Attributes;

namespace Invy.Core.Managers.Contracts.Messages;

/// <summary>
/// Command: add the next batch of a started import's items, on the imports queue. Each batch is saved with how far it
/// got and queues the next, so running it again carries on where it stopped.
/// </summary>
/// <param name="SpaceId">The import's space.</param>
/// <param name="ImportId">The import.</param>
[MessageIdentity("invy.run-import.v1")]
public sealed record RunImport(Guid SpaceId, Guid ImportId);
