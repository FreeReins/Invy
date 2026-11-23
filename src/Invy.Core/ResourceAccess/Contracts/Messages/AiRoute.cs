using Invy.Core.Managers.Contracts.Messages;

namespace Invy.Core.ResourceAccess.Contracts.Messages;

/// <summary>A task that uses a model other than the default.</summary>
internal sealed class AiRoute
{
    public AiTask Task { get; set; }

    public Guid ModelId { get; set; }
}
