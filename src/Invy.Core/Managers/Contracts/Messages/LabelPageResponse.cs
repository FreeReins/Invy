namespace Invy.Core.Managers.Contracts.Messages;

/// <summary>A page of a space's labels, by code.</summary>
/// <param name="Labels">The labels on this page.</param>
/// <param name="Next">Pass as <c>after</c> for the next page, or null on the last.</param>
public sealed record LabelPageResponse(IReadOnlyList<LabelResponse> Labels, string? Next);
