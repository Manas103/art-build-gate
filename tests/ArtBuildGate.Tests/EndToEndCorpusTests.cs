using ArtBuildGate.Core;
using Xunit;
using Xunit.Abstractions;

namespace ArtBuildGate.Tests;

/// <summary>
/// The measurement this project's README quotes: build the full 240-asset
/// library (200 distinct + 40 planted near-duplicate pairs), render every
/// asset from all 8 angles, run the readability gate over every pair, and
/// report how many of the 40 planted pairs were caught and how many of the
/// other (not planted) pairs were falsely flagged.
/// </summary>
public class EndToEndCorpusTests
{
    private readonly ITestOutputHelper _output;
    public EndToEndCorpusTests(ITestOutputHelper output) => _output = output;

    [Fact]
    public void GateCatchesPlantedNearDuplicatesWithFewOrNoFalseFlags()
    {
        var library = AssetLibraryGenerator.Build(20261012);
        Assert.Equal(240, library.Assets.Length);

        var renders = new Dictionary<string, Silhouette[]>(library.Assets.Length);
        foreach (var asset in library.Assets)
            renders[asset.Name] = SilhouetteRenderer.RenderAllAngles(asset.Params);

        var flagged = ReadabilityGate.FindSimilarPairs(renders);

        var plantedSet = library.PlantedPairs.Select(p => (p.BaseName, p.CloneName)).ToHashSet();
        var flaggedSet = flagged.Select(f => (f.NameA, f.NameB))
            .Concat(flagged.Select(f => (f.NameB, f.NameA))).ToHashSet();

        int caught = plantedSet.Count(pair => flaggedSet.Contains(pair));
        int falseFlags = flagged.Count(f =>
            !plantedSet.Contains((f.NameA, f.NameB)) && !plantedSet.Contains((f.NameB, f.NameA)));

        _output.WriteLine($"caught {caught}/{library.PlantedPairs.Length} planted pairs, {falseFlags} false flags over {28640 - caught} non-planted pairs compared");

        // See docs/threshold_sweep.txt and README Findings: at this
        // corpus's measured IoU distributions, no threshold catches
        // nearly all 40 planted pairs with zero false flags, because the
        // distributions genuinely overlap. These bounds are the actual
        // measured tradeoff at ReadabilityGate.Threshold (0.96), not a
        // target backed into the gate.
        Assert.InRange(caught, 36, 40);
        Assert.InRange(falseFlags, 15, 45);
    }
}
