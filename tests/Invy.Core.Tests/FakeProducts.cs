using Invy.Core.Managers.Contracts;
using Invy.Core.ResourceAccess.Contracts.Messages;
using Invy.Core.ResourceAccess.Contracts.Services;

namespace Invy.Core.Tests;

/// <summary>Product databases and web files kept in memory: a barcode or address not listed isn't known.</summary>
internal sealed class FakeProducts : IProductAccess
{
    public Dictionary<string, ProductInfo> Products { get; } = [];

    public Dictionary<Uri, byte[]> Web { get; } = [];

    /// <summary>Barcodes looked up, in order.</summary>
    public List<string> Asked { get; } = [];

    public Task<ProductInfo?> LookUpAsync(string barcode, CancellationToken cancellationToken)
    {
        Asked.Add(barcode);
        return Task.FromResult(Products.GetValueOrDefault(barcode));
    }

    public Task<byte[]> DownloadAsync(Uri address, int maxBytes, CancellationToken cancellationToken) =>
        Web.TryGetValue(address, out var data)
            ? Task.FromResult(data)
            : throw new WebSourceException($"The photo at {address.Host} couldn't be downloaded. Add a photo of your own instead.");
}
