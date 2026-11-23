namespace Invy.Core.ResourceAccess.Contracts.Messages;

/// <summary>What an online product database says about a barcode.</summary>
/// <param name="Name">The product's name.</param>
/// <param name="Brand">Its brand, or null.</param>
/// <param name="ImageUrl">A photo of it on the web, or null.</param>
/// <param name="Source">The database that answered, such as Open Food Facts.</param>
internal sealed record ProductInfo(string Name, string? Brand, Uri? ImageUrl, string Source);
