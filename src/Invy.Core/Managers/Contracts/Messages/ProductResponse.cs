namespace Invy.Core.Managers.Contracts.Messages;

/// <summary>A product found online from its barcode, to fill in a new item.</summary>
/// <param name="Barcode">The barcode looked up, digits only.</param>
/// <param name="Name">The product's name.</param>
/// <param name="Brand">Its brand, or null.</param>
/// <param name="ImageUrl">A photo of it on the web, or null. Attach it with POST .../attachments/from-web.</param>
/// <param name="Source">The database that answered: Open Food Facts or UPCitemdb.</param>
public sealed record ProductResponse(string Barcode, string Name, string? Brand, string? ImageUrl, string Source);
