using Microsoft.Extensions.Options;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SaviaUp.Backend.Domain.Options;
using SaviaUp.Backend.Infrastructure.Storage;

namespace SaviaUp.Backend.IntegrationTests;

public sealed class LocalFileStorageTests
{
    [Fact]
    public async Task SaveImage_ConvertsToWebpAndKeepsTenantScopedImmutableReference()
    {
        var root = Path.Combine(Path.GetTempPath(), $"saviaup-storage-{Guid.NewGuid():N}");
        try
        {
            var storage = new LocalFileStorage(Options.Create(new FileStorageOptions
            {
                RootPath = root,
                PublicBaseUrl = "https://saviaup.test/pvc",
                MaximumWidth = 640,
                MaximumHeight = 640,
                WebpQuality = 80
            }));
            var tenantId = Guid.NewGuid();
            var content = CreatePng();

            var stored = await storage.SaveImageAsync(
                tenantId,
                "Products",
                "Product 1",
                content,
                "image/png",
                "original.png",
                CancellationToken.None);

            Assert.StartsWith($"/pvc/{tenantId:D}/products/product-1/", stored.Reference);
            Assert.EndsWith(".webp", stored.Reference);
            Assert.Equal("image/webp", stored.ContentType);
            Assert.Equal($"https://saviaup.test{stored.Reference}", storage.GetPublicUrl(stored.Reference));

            var retrieved = await storage.GetAsync(tenantId, stored.Reference, CancellationToken.None);
            Assert.NotNull(retrieved);
            Assert.Equal("image/webp", retrieved.ContentType);
            using var decoded = Image.Load(retrieved.Content);
            Assert.Equal((24, 16), (decoded.Width, decoded.Height));

            await Assert.ThrowsAsync<InvalidOperationException>(() => storage.GetAsync(
                Guid.NewGuid(),
                stored.Reference,
                CancellationToken.None));

            await storage.DeleteAsync(tenantId, stored.Reference, CancellationToken.None);
            Assert.Null(await storage.GetAsync(tenantId, stored.Reference, CancellationToken.None));
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }

    private static byte[] CreatePng()
    {
        using var image = new Image<Rgba32>(24, 16, Color.ForestGreen);
        using var stream = new MemoryStream();
        image.SaveAsPng(stream);
        return stream.ToArray();
    }
}
