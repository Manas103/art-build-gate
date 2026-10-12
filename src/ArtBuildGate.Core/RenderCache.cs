namespace ArtBuildGate.Core;

/// <summary>
/// Content-addressed cache for a character's 8-angle render: the cache key
/// is CharacterParams.ContentHash(), so a regeneration run only pays the
/// rasterization cost for assets whose proportions actually changed.
/// Stored on disk (not in memory) so this survives across process
/// invocations the way a real CI render-cache step would.
/// </summary>
public sealed class RenderCache
{
    private readonly string _dir;

    public RenderCache(string dir)
    {
        _dir = dir;
        Directory.CreateDirectory(_dir);
    }

    private string PathFor(ulong hash) => Path.Combine(_dir, $"{hash:x16}.sil");

    public Silhouette[]? TryGet(ulong hash)
    {
        string path = PathFor(hash);
        if (!File.Exists(path)) return null;
        using var fs = File.OpenRead(path);
        using var r = new BinaryReader(fs);
        int count = r.ReadInt32();
        var result = new Silhouette[count];
        for (int i = 0; i < count; i++) result[i] = Silhouette.ReadFrom(r);
        return result;
    }

    public void Store(ulong hash, Silhouette[] renders)
    {
        string path = PathFor(hash);
        using var fs = File.Create(path);
        using var w = new BinaryWriter(fs);
        w.Write(renders.Length);
        foreach (var s in renders) s.WriteTo(w);
    }

    /// <summary>
    /// Renders every asset's 8-angle silhouette set, reusing the cache for
    /// any asset whose ContentHash() already has a cached render. Returns
    /// (renders by asset name, number of assets that were actually
    /// rasterized rather than served from cache).
    /// </summary>
    public (Dictionary<string, Silhouette[]> Renders, int CacheMisses) RenderLibrary(AssetRecord[] assets)
    {
        var renders = new Dictionary<string, Silhouette[]>(assets.Length);
        int misses = 0;
        foreach (var asset in assets)
        {
            ulong hash = asset.Params.ContentHash();
            var cached = TryGet(hash);
            if (cached != null)
            {
                renders[asset.Name] = cached;
                continue;
            }
            misses++;
            var rendered = SilhouetteRenderer.RenderAllAngles(asset.Params);
            Store(hash, rendered);
            renders[asset.Name] = rendered;
        }
        return (renders, misses);
    }
}
