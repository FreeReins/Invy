using Invy.Core.ResourceAccess.Contracts.Messages;

namespace Invy.Core.ResourceAccess.Contracts.Services;

/// <summary>
/// Online product databases, for filling in an item from its UPC or EAN barcode, and downloading the photo they point
/// to. Only public internet addresses are reached (PublicWeb). Callers check that an admin turned lookups on.
/// </summary>
internal interface IProductAccess
{
    /// <summary>
    /// The product with this barcode: Open Food Facts first (open data, no key), then UPCitemdb's free trial (about 100
    /// a day). Null when neither knows it.
    /// </summary>
    /// <exception cref="Managers.Contracts.WebSourceException">Neither could be reached.</exception>
    Task<ProductInfo?> LookUpAsync(string barcode, CancellationToken cancellationToken);

    /// <summary>Downloads a file of at most <paramref name="maxBytes"/>.</summary>
    /// <exception cref="Managers.Contracts.WebSourceException">It couldn't be downloaded, or is bigger than that.</exception>
    Task<byte[]> DownloadAsync(Uri address, int maxBytes, CancellationToken cancellationToken);
}
