using Invy.Core.ResourceAccess.Contracts.Messages;

namespace Invy.Core.Engines.Contracts.Messages;

/// <summary>An item with the space it is in, for a notification that covers several spaces.</summary>
internal sealed record SpaceItem(Space Space, Item Item);
