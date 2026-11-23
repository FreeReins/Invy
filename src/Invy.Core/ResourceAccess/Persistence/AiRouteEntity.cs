using Invy.Core.Managers.Contracts.Messages;

namespace Invy.Core.ResourceAccess.Persistence;

/// <summary>A task sent to a model other than the default; one row per task.</summary>
internal sealed class AiRouteEntity
{
    public AiTask Task { get; set; }

    public Guid ModelId { get; set; }
}
