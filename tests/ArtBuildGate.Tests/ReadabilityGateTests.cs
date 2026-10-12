using ArtBuildGate.Core;
using Xunit;

namespace ArtBuildGate.Tests;

public class ReadabilityGateTests
{
    [Fact]
    public void IdenticalAssetsAreFlagged()
    {
        var p = new CharacterParams(1, 0.28f, 0.22f, 0.32f, 0.35f, 0.47f, 0.08f, 0.13f);
        var renders = new Dictionary<string, Silhouette[]>
        {
            ["a"] = SilhouetteRenderer.RenderAllAngles(p),
            ["b"] = SilhouetteRenderer.RenderAllAngles(p),
        };
        var flagged = ReadabilityGate.FindSimilarPairs(renders);
        Assert.Single(flagged);
    }

    [Fact]
    public void WildlyDifferentAssetsAreNotFlagged()
    {
        var small = new CharacterParams(0.85f, 0.22f, 0.18f, 0.28f, 0.30f, 0.42f, 0.06f, 0.11f);
        var big = new CharacterParams(1.15f, 0.34f, 0.28f, 0.36f, 0.40f, 0.52f, 0.11f, 0.15f);
        var renders = new Dictionary<string, Silhouette[]>
        {
            ["small"] = SilhouetteRenderer.RenderAllAngles(small),
            ["big"] = SilhouetteRenderer.RenderAllAngles(big),
        };
        var flagged = ReadabilityGate.FindSimilarPairs(renders);
        Assert.Empty(flagged);
    }
}
