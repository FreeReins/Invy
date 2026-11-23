namespace Invy.Core.ResourceAccess.Contracts.Messages;

/// <summary>How search results are ordered.</summary>
internal enum SearchOrder
{
    /// <summary>Best match for the words first.</summary>
    Relevance,

    /// <summary>By name.</summary>
    Name,

    /// <summary>Most recently changed first.</summary>
    Recent,
}
