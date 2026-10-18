using SkiaSharp;

namespace SaviaUp.Backend.Infrastructure.Storage;

internal static class ImageProcessor
{
    public static byte[] ConvertToWebp(
        byte[] source,
        int maximumWidth,
        int maximumHeight,
        int quality)
    {
        if (source.Length == 0) throw new InvalidDataException("The image is empty.");
        if (maximumWidth <= 0) throw new ArgumentOutOfRangeException(nameof(maximumWidth));
        if (maximumHeight <= 0) throw new ArgumentOutOfRangeException(nameof(maximumHeight));

        try
        {
            using var decoded = Decode(source, out var origin);
            using var oriented = ApplyOrientation(decoded, origin);
            using var resized = ResizeToFit(oriented, maximumWidth, maximumHeight);
            using var image = SKImage.FromBitmap(resized);
            using var encoded = image.Encode(SKEncodedImageFormat.Webp, Math.Clamp(quality, 1, 100));

            return encoded?.ToArray()
                ?? throw new InvalidDataException("The image could not be encoded as WebP.");
        }
        catch (InvalidDataException)
        {
            throw;
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        {
            throw new InvalidDataException("The supplied image is invalid or unsupported.", exception);
        }
    }

    private static SKBitmap Decode(byte[] source, out SKEncodedOrigin origin)
    {
        using var stream = new SKMemoryStream(source);
        using var codec = SKCodec.Create(stream)
            ?? throw new InvalidDataException("The supplied content is not a supported image.");

        origin = codec.EncodedOrigin;
        return SKBitmap.Decode(codec)
            ?? throw new InvalidDataException("The supplied image is invalid.");
    }

    private static SKBitmap ApplyOrientation(SKBitmap source, SKEncodedOrigin origin)
    {
        var swapsDimensions = origin is
            SKEncodedOrigin.LeftTop or
            SKEncodedOrigin.RightTop or
            SKEncodedOrigin.RightBottom or
            SKEncodedOrigin.LeftBottom;
        var width = swapsDimensions ? source.Height : source.Width;
        var height = swapsDimensions ? source.Width : source.Height;
        var target = CreateBitmap(width, height);

        using var canvas = new SKCanvas(target);
        canvas.Clear(SKColors.Transparent);
        canvas.Save();

        switch (origin)
        {
            case SKEncodedOrigin.TopRight:
                canvas.Translate(width, 0);
                canvas.Scale(-1, 1);
                break;
            case SKEncodedOrigin.BottomRight:
                canvas.Translate(width, height);
                canvas.RotateDegrees(180);
                break;
            case SKEncodedOrigin.BottomLeft:
                canvas.Translate(0, height);
                canvas.Scale(1, -1);
                break;
            case SKEncodedOrigin.LeftTop:
                canvas.RotateDegrees(90);
                canvas.Scale(1, -1);
                break;
            case SKEncodedOrigin.RightTop:
                canvas.Translate(width, 0);
                canvas.RotateDegrees(90);
                break;
            case SKEncodedOrigin.RightBottom:
                canvas.Translate(width, height);
                canvas.RotateDegrees(90);
                canvas.Scale(-1, 1);
                break;
            case SKEncodedOrigin.LeftBottom:
                canvas.Translate(0, height);
                canvas.RotateDegrees(-90);
                break;
        }

#pragma warning disable CS0618 // SkiaSharp 3.119.1 exposes filtering on SKPaint for DrawBitmap.
        using var paint = new SKPaint { FilterQuality = SKFilterQuality.High };
#pragma warning restore CS0618
        canvas.DrawBitmap(source, 0, 0, paint);
        canvas.Restore();
        return target;
    }

    private static SKBitmap ResizeToFit(SKBitmap source, int maximumWidth, int maximumHeight)
    {
        var scale = Math.Min(
            (double)maximumWidth / source.Width,
            (double)maximumHeight / source.Height);
        var width = scale >= 1 ? source.Width : Math.Max(1, (int)Math.Round(source.Width * scale));
        var height = scale >= 1 ? source.Height : Math.Max(1, (int)Math.Round(source.Height * scale));

        var target = CreateBitmap(width, height);
        using var canvas = new SKCanvas(target);
        canvas.Clear(SKColors.Transparent);
#pragma warning disable CS0618 // SkiaSharp 3.119.1 exposes filtering on SKPaint for DrawBitmap.
        using var paint = new SKPaint { FilterQuality = SKFilterQuality.High };
#pragma warning restore CS0618
        canvas.DrawBitmap(source, new SKRect(0, 0, width, height), paint);
        return target;
    }

    private static SKBitmap CreateBitmap(int width, int height)
        => new(new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Premul));
}
