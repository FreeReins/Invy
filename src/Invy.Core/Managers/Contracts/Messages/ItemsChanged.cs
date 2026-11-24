using Wolverine.Attributes;

namespace Invy.Core.Managers.Contracts.Messages;

/// <summary>
/// Event: items were added, changed, moved, seen or disposed of inside these containers, so their counts and value
/// totals need recounting (the rollups queue), and open pages load again (the items live topic). Recounting twice gives
/// the same result; an empty list recounts nothing.
/// </summary>
/// <param name="SpaceId">The space the containers are in.</param>
/// <param name="ContainerIds">Every container whose contents changed, at any depth: the item's old and new containers and theirs.</param>
[MessageIdentity("invy.items-changed.v1")]
public sealed record ItemsChanged(Guid SpaceId, IReadOnlyList<Guid> ContainerIds);
