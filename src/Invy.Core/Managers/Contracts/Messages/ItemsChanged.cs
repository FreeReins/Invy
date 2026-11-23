using Wolverine.Attributes;

namespace Invy.Core.Managers.Contracts.Messages;

/// <summary>
/// Event: items were added, changed, moved or disposed of inside these containers, so their counts and value totals
/// need recounting (the rollups queue). Recounting twice gives the same result.
/// </summary>
/// <param name="SpaceId">The space the containers are in.</param>
/// <param name="ContainerIds">Every container whose contents changed, at any depth: the item's old and new containers and theirs.</param>
[MessageIdentity("invy.items-changed.v1")]
public sealed record ItemsChanged(Guid SpaceId, IReadOnlyList<Guid> ContainerIds);
