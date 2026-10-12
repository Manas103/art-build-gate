using ArtBuildGate.Core;
using Xunit;

namespace ArtBuildGate.Tests;

public class RenderCacheTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "abg-cache-test-" + Guid.NewGuid());

    public void Dispose()
    {
        if (Directory.Exists(_dir)) Directory.Delete(_dir, recursive: true);
    }

    [Fact]
    public void ContentHashIsStableAndDistinguishesDifferentParams()
    {
        var p1 = new CharacterParams(1, 0.28f, 0.22f, 0.32f, 0.35f, 0.47f, 0.08f, 0.13f);
        var p2 = p1 with { ShoulderWidth = 0.29f };
        Assert.Equal(p1.ContentHash(), p1.ContentHash());
        Assert.NotEqual(p1.ContentHash(), p2.ContentHash());
    }

    [Fact]
    public void SecondRenderOfSameAssetIsACacheHitAndReturnsIdenticalSilhouettes()
    {
        var cache = new RenderCache(_dir);
        var asset = new AssetRecord("a", new CharacterParams(1, 0.28f, 0.22f, 0.32f, 0.35f, 0.47f, 0.08f, 0.13f));

        var (first, misses1) = cache.RenderLibrary(new[] { asset });
        var (second, misses2) = cache.RenderLibrary(new[] { asset });

        Assert.Equal(1, misses1);
        Assert.Equal(0, misses2);
        Assert.Equal(1.0, Silhouette.IntersectionOverUnion(first["a"][0], second["a"][0]));
    }

    [Fact]
    public void ChangingOneAssetOnlyMissesThatAssetOnRerender()
    {
        var cache = new RenderCache(_dir);
        var assets = new[]
        {
            new AssetRecord("a", new CharacterParams(1, 0.28f, 0.22f, 0.32f, 0.35f, 0.47f, 0.08f, 0.13f)),
            new AssetRecord("b", new CharacterParams(1, 0.25f, 0.20f, 0.30f, 0.33f, 0.45f, 0.07f, 0.12f)),
        };
        cache.RenderLibrary(assets);

        assets[0] = assets[0] with { Params = assets[0].Params with { ShoulderWidth = 0.31f } };
        var (_, misses) = cache.RenderLibrary(assets);
        Assert.Equal(1, misses);
    }
}
