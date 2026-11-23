using Invy.Core.Managers.Contracts;
using Invy.Core.Managers.Contracts.Messages;
using Invy.Core.ResourceAccess.Contracts.Messages;
using Invy.Core.Utilities;

namespace Invy.Core.Engines;

/// <content>Files attached to items.</content>
internal sealed partial class ValidationEngine
{
    /// <summary>The largest file that can be attached.</summary>
    public const int MaxAttachmentBytes = 10 * 1024 * 1024;

    public const int AttachmentNameLength = 200;

    public async Task<Attachment> AttachmentAsync(Guid spaceId, Guid id, CancellationToken cancellationToken) =>
        await attachments.FindAsync(spaceId, id, cancellationToken)
        ?? throw new NotFoundException("That attachment no longer exists. It may have been removed; reload the item.");

    /// <summary>What a file to attach is, from its bytes: a picture a browser shows everywhere, or a PDF. Anything else is refused.</summary>
    public static (AttachmentKind Kind, string ContentType, string Extension) AttachmentFile(AddAttachmentRequest request)
    {
        if (request.Data is not { Length: > 0 } data)
        {
            throw new RuleViolationException("The file is empty. Choose the photo or PDF again.");
        }

        if (data.Length > MaxAttachmentBytes)
        {
            throw new RuleViolationException($"That file is {data.Length / (1024 * 1024.0):0.#} MB. Attach files up to {MaxAttachmentBytes / (1024 * 1024)} MB.");
        }

        if (ImageFormat.Detect(data) is { } image)
        {
            return (AttachmentKind.Photo, image, image["image/".Length..].Replace("jpeg", "jpg", StringComparison.Ordinal));
        }

        if (data.AsSpan().StartsWith("%PDF-"u8))
        {
            return (AttachmentKind.Document, "application/pdf", "pdf");
        }

        throw new RuleViolationException("Invy keeps photos (JPEG, PNG, WebP or GIF) and PDFs. Save the file as one of those, then attach it.");
    }

    /// <summary>An http or https address to download from.</summary>
    public static Uri DownloadAddress(string? text) =>
        Uri.TryCreate(text?.Trim(), UriKind.Absolute, out var address) && address.Scheme is "https" or "http"
            ? address
            : throw new RuleViolationException("That isn't a web address. Use one starting with https://.");

    /// <summary>A UPC or EAN barcode: 8, 12, 13 or 14 digits, spaces and dashes left out.</summary>
    public static string Barcode(string? text)
    {
        var digits = new string((text ?? string.Empty).Where(c => c is not (' ' or '-')).ToArray());
        return digits.Length is 8 or 12 or 13 or 14 && digits.All(char.IsAsciiDigit)
            ? digits
            : throw new RuleViolationException($"\"{text}\" isn't a product barcode. Scan the UPC or EAN barcode on the packaging.");
    }

    /// <summary>The name to show and download the file as: the last part of what was sent, or a plain one.</summary>
    public static string AttachmentName(string? name, string extension)
    {
        var plain = Path.GetFileName((name ?? string.Empty).Replace('\\', '/')).Trim();
        plain = new string(plain.Where(c => !char.IsControl(c) && c is not ('"' or '/')).ToArray());
        if (plain.Length == 0)
        {
            return $"{(extension == "pdf" ? "document" : "photo")}.{extension}";
        }

        return plain.Length <= AttachmentNameLength ? plain : plain[..AttachmentNameLength];
    }
}
