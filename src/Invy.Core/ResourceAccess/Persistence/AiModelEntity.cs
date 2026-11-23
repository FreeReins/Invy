using Invy.Core.Managers.Contracts.Messages;

namespace Invy.Core.ResourceAccess.Persistence;

internal sealed class AiModelEntity
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public AiProvider Provider { get; set; }

    public string Model { get; set; } = string.Empty;

    public string Endpoint { get; set; } = string.Empty;

    public string ApiKey { get; set; } = string.Empty;

    public bool IsDefault { get; set; }

    public int Position { get; set; }
}
