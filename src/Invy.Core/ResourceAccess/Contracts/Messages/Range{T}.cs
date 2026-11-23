namespace Invy.Core.ResourceAccess.Contracts.Messages;

/// <summary>A range with either end open. An end is included when its Inclusive flag is set.</summary>
internal sealed record Range<T>(T? Low, bool LowInclusive, T? High, bool HighInclusive)
    where T : struct;
