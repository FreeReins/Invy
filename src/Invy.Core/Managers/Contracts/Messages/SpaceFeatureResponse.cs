namespace Invy.Core.Managers.Contracts.Messages;

/// <summary>A feature a space can switch on or off.</summary>
/// <param name="Key">What <see cref="SpaceResponse.Features"/> holds, such as audits.</param>
/// <param name="Name">What its switch is called.</param>
/// <param name="Description">One plain line on what it does.</param>
/// <param name="Available">False while the feature isn't built yet: its switch isn't offered, but spaces can keep it on.</param>
public sealed record SpaceFeatureResponse(string Key, string Name, string Description, bool Available);
