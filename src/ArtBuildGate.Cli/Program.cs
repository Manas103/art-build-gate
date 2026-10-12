using System.Diagnostics;
using ArtBuildGate.Core;

string mode = "full";
string cacheDir = Path.Combine(Path.GetTempPath(), "art-build-gate-cache");
int mutateCount = 0;
int seed = 20261012;

for (int i = 0; i < args.Length; i++)
{
    switch (args[i])
    {
        case "--mode": mode = args[++i]; break;
        case "--cache": cacheDir = args[++i]; break;
        case "--mutate": mutateCount = int.Parse(args[++i]); break;
        case "--seed": seed = int.Parse(args[++i]); break;
    }
}

var library = AssetLibraryGenerator.Build(seed);
Console.WriteLine($"library: {library.Assets.Length} assets, {library.PlantedPairs.Length} planted near-duplicate pairs");

if (mode == "full" && Directory.Exists(cacheDir))
{
    Directory.Delete(cacheDir, recursive: true);
}

var assets = library.Assets;
if (mode == "rerun" && mutateCount > 0)
{
    var rng = new Random(seed + 1);
    for (int i = 0; i < mutateCount; i++)
    {
        int idx = rng.Next(AssetLibraryGenerator.DistinctCount);
        var old = assets[idx];
        var p = old.Params;
        // a genuine content change: shoulder width nudged well outside the
        // near-duplicate perturbation band, so this is not meant to look
        // like a planted near-duplicate, just "an artist changed a skin".
        var changed = p with { ShoulderWidth = p.ShoulderWidth * 1.15f };
        assets[idx] = old with { Params = changed };
    }
    Console.WriteLine($"mutated {mutateCount} assets before this run");
}

var cache = new RenderCache(cacheDir);
var sw = Stopwatch.StartNew();
var (renders, misses) = cache.RenderLibrary(assets);
sw.Stop();
Console.WriteLine($"render: {sw.Elapsed.TotalMilliseconds:F1} ms total, {misses}/{assets.Length} cache misses (rasterized), {assets.Length - misses} cache hits");

var gateSw = Stopwatch.StartNew();
var flagged = ReadabilityGate.FindSimilarPairs(renders);
gateSw.Stop();
Console.WriteLine($"gate: {gateSw.Elapsed.TotalMilliseconds:F1} ms, {flagged.Count} pairs flagged over {assets.Length} assets ({(long)assets.Length * (assets.Length - 1) / 2} pairs compared)");

var plantedSet = library.PlantedPairs.Select(p => (p.BaseName, p.CloneName)).ToHashSet();
int caught = 0;
var flaggedSet = flagged.Select(f => (f.NameA, f.NameB)).Concat(flagged.Select(f => (f.NameB, f.NameA))).ToHashSet();
foreach (var (baseName, cloneName) in plantedSet)
{
    if (flaggedSet.Contains((baseName, cloneName))) caught++;
}
int falsePositives = flagged.Count(f => !plantedSet.Contains((f.NameA, f.NameB)) && !plantedSet.Contains((f.NameB, f.NameA)));

Console.WriteLine($"planted pairs caught: {caught}/{library.PlantedPairs.Length}");
Console.WriteLine($"false flags (pairs flagged that were not planted near-duplicates): {falsePositives}");

return 0;
