using Invy.Core.ResourceAccess.Contracts.Messages;

namespace Invy.Core.ResourceAccess.Contracts.Services;

/// <summary>The starter packs of item types offered when making a space.</summary>
internal interface IStarterPackAccess
{
    /// <summary>Every pack, in the order the files give.</summary>
    IReadOnlyList<StarterPack> All();
}
