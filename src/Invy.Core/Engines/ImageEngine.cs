using SkiaSharp;

namespace Invy.Core.Engines;

/// <summary>Makes small copies of photos for lists and item pages. Reads nothing: bytes in, bytes out.</summary>
internal sealed class ImageEngine
{
    /// <summary>The longer side of a thumbnail, in pixels.</summary>
    public const int ThumbnailSize = 480;

    /// <summary>Pictures with more pixels than this aren't decoded, so a crafted file can't take all the memory.</summary>
    private const long MaxPixels = 100_000_000;

    /// <summary>
    /// A JPEG at most <see cref="ThumbnailSize"/> pixels on its longer side, turned the way the camera says it was held,
    /// on white where the picture is see-through. Null when the bytes can't be read as a picture.
    /// </summary>
    public byte[]? Thumbnail(byte[] image)
    {
        using var codec = SKCodec.Create(new SKMemoryStream(image));
        if (codec is null || (long)codec.Info.Width * codec.Info.Height > MaxPixels)
        {
            return null;
        }

        using var full = SKBitmap.Decode(codec);
        if (full is null)
        {
            return null;
        }

        var scale = Math.Min(1.0, (double)ThumbnailSize / Math.Max(full.Width, full.Height));
        var width = Math.Max(1, (int)Math.Round(full.Width * scale));
        var height = Math.Max(1, (int)Math.Round(full.Height * scale));
        using var small = full.Resize(new SKImageInfo(width, height), new SKSamplingOptions(SKCubicResampler.Mitchell));
        if (small is null)
        {
            return null;
        }

        var origin = codec.EncodedOrigin;
        var sideways = origin is SKEncodedOrigin.RightTop or SKEncodedOrigin.LeftBottom or SKEncodedOrigin.LeftTop or SKEncodedOrigin.RightBottom;
        using var turned = new SKBitmap(sideways ? height : width, sideways ? width : height);
        using (var canvas = new SKCanvas(turned))
        {
            canvas.Clear(SKColors.White);
            switch (origin)
            {
                case SKEncodedOrigin.BottomRight:
                    canvas.Translate(turned.Width, turned.Height);
                    canvas.RotateDegrees(180);
                    break;
                case SKEncodedOrigin.RightTop:
                    canvas.Translate(turned.Width, 0);
                    canvas.RotateDegrees(90);
                    break;
                case SKEncodedOrigin.LeftBottom:
                    canvas.Translate(0, turned.Height);
                    canvas.RotateDegrees(270);
                    break;
            }

            canvas.DrawBitmap(small, 0, 0);
        }

        using var picture = SKImage.FromBitmap(turned);
        using var encoded = picture.Encode(SKEncodedImageFormat.Jpeg, 80);
        return encoded?.ToArray();
    }
}
