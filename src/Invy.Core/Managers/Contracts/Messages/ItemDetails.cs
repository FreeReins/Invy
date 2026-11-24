using System.Text.Json;

namespace Invy.Core.Managers.Contracts.Messages;

/// <summary>What a view shows of a found item beyond its summary: its field values, dates and photo.</summary>
/// <param name="Fields">Its field values, by field id.</param>
/// <param name="Tags">Its tags.</param>
/// <param name="LastSeenAt">When it was last seen (opened, scanned or audited), or null if never.</param>
/// <param name="CreatedAt">When it was added.</param>
/// <param name="PhotoId">The attachment id of its first photo with a thumbnail, or null without one.</param>
public sealed record ItemDetails(
    IReadOnlyDictionary<Guid, JsonElement> Fields, IReadOnlyList<string> Tags, DateTimeOffset? LastSeenAt, DateTimeOffset CreatedAt, Guid? PhotoId);
