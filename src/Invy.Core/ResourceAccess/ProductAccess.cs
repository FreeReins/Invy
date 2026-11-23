using System.Net;
using System.Text.Json;
using Invy.Core.Managers.Contracts;
using Invy.Core.ResourceAccess.Contracts.Messages;
using Invy.Core.ResourceAccess.Contracts.Services;

namespace Invy.Core.ResourceAccess;

/// <inheritdoc cref="IProductAccess"/>
/// <param name="http">Made with <see cref="PublicWeb.Handler"/>, so only the public internet is reached.</param>
internal sealed class ProductAccess(HttpClient http) : IProductAccess
{
    /// <summary>Who is asking, as Open Food Facts asks apps to say. Nothing about the install or its owner.</summary>
    public const string UserAgent = "Invy/1.0 (self-hosted home inventory)";

    public async Task<ProductInfo?> LookUpAsync(string barcode, CancellationToken cancellationToken)
    {
        WebSourceException? failure = null;
        foreach (var source in new Func<string, CancellationToken, Task<ProductInfo?>>[] { OpenFoodFactsAsync, UpcItemDbAsync })
        {
            try
            {
                if (await source(barcode, cancellationToken) is { } found)
                {
                    return found;
                }
            }
            catch (Exception exception) when (exception is HttpRequestException or JsonException or TaskCanceledException && !cancellationToken.IsCancellationRequested)
            {
                failure = PublicWeb.Refusal(exception)
                    ?? new WebSourceException("The barcode lookup services couldn't be reached. Check Invy's internet connection, or type the name instead.", exception);
            }
        }

        return failure is null ? null : throw failure;
    }

    public async Task<byte[]> DownloadAsync(Uri address, int maxBytes, CancellationToken cancellationToken)
    {
        if (address.Scheme is not ("https" or "http"))
        {
            throw new WebSourceException("Invy only downloads photos from http and https addresses.");
        }

        try
        {
            using var response = await http.GetAsync(address, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            response.EnsureSuccessStatusCode();
            if (response.Content.Headers.ContentLength > maxBytes)
            {
                throw new WebSourceException($"The photo at {address.Host} is over {maxBytes / (1024 * 1024)} MB, so Invy didn't keep it.");
            }

            await using var body = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var copy = new MemoryStream();
            var buffer = new byte[81920];
            int read;
            while ((read = await body.ReadAsync(buffer, cancellationToken)) > 0)
            {
                if (copy.Length + read > maxBytes)
                {
                    throw new WebSourceException($"The photo at {address.Host} is over {maxBytes / (1024 * 1024)} MB, so Invy didn't keep it.");
                }

                copy.Write(buffer, 0, read);
            }

            return copy.ToArray();
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException && !cancellationToken.IsCancellationRequested)
        {
            throw PublicWeb.Refusal(exception)
                ?? new WebSourceException($"The photo at {address.Host} couldn't be downloaded. Add a photo of your own instead.", exception);
        }
    }

    private async Task<ProductInfo?> OpenFoodFactsAsync(string barcode, CancellationToken cancellationToken)
    {
        using var json = await GetJsonAsync(
            $"https://world.openfoodfacts.org/api/v2/product/{barcode}.json?fields=product_name,brands,image_front_url,image_url", cancellationToken);
        if (json is null || !json.RootElement.TryGetProperty("product", out var product) || Text(product, "product_name") is not { } name)
        {
            return null;
        }

        var brand = Text(product, "brands")?.Split(',')[0].Trim();
        return new ProductInfo(name, string.IsNullOrEmpty(brand) ? null : brand, Address(Text(product, "image_front_url") ?? Text(product, "image_url")), "Open Food Facts");
    }

    private async Task<ProductInfo?> UpcItemDbAsync(string barcode, CancellationToken cancellationToken)
    {
        using var json = await GetJsonAsync($"https://api.upcitemdb.com/prod/trial/lookup?upc={barcode}", cancellationToken);
        if (json is null || !json.RootElement.TryGetProperty("items", out var items) || items.ValueKind != JsonValueKind.Array
            || items.GetArrayLength() == 0 || Text(items[0], "title") is not { } name)
        {
            return null;
        }

        var item = items[0];
        var image = item.TryGetProperty("images", out var images) && images.ValueKind == JsonValueKind.Array
            ? images.EnumerateArray().Select(element => element.ValueKind == JsonValueKind.String ? Address(element.GetString()) : null).FirstOrDefault(uri => uri is not null)
            : null;
        return new ProductInfo(name, Text(item, "brand"), image, "UPCitemdb");
    }

    /// <summary>The answer as JSON, or null when the service says it doesn't know the code (404, or an empty answer).</summary>
    private async Task<JsonDocument?> GetJsonAsync(string address, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, address);
        request.Headers.UserAgent.ParseAdd(UserAgent);
        request.Headers.Accept.ParseAdd("application/json");
        using var response = await http.SendAsync(request, cancellationToken);
        if (response.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.BadRequest)
        {
            return null;
        }

        if (response.StatusCode == HttpStatusCode.TooManyRequests)
        {
            throw new HttpRequestException("The lookup service's daily limit is used up.", null, response.StatusCode);
        }

        response.EnsureSuccessStatusCode();
        await using var body = await response.Content.ReadAsStreamAsync(cancellationToken);
        return await JsonDocument.ParseAsync(body, cancellationToken: cancellationToken);
    }

    private static string? Text(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String && value.GetString()?.Trim() is { Length: > 0 } text
            ? text
            : null;

    private static Uri? Address(string? text) =>
        Uri.TryCreate(text, UriKind.Absolute, out var uri) && uri.Scheme is "https" or "http" ? uri : null;
}
