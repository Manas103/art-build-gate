using ArtBuildGate.Core;
using Xunit;

namespace ArtBuildGate.Tests;

public class AssetLibraryGeneratorTests
{
    [Fact]
    public void BuildsExactlyTwoHundredFortyAssetsAndFortyPlantedPairs()
    {
        var lib = AssetLibraryGenerator.Build(1);
        Assert.Equal(240, lib.Assets.Length);
        Assert.Equal(40, lib.PlantedPairs.Length);
    }

    [Fact]
    public void SameSeedProducesAnIdenticalLibrary()
    {
        var a = AssetLibraryGenerator.Build(2026);
        var b = AssetLibraryGenerator.Build(2026);
        Assert.Equal(a.Assets.Select(x => x.Name), b.Assets.Select(x => x.Name));
        for (int i = 0; i < a.Assets.Length; i++)
        {
            Assert.Equal(a.Assets[i].Params, b.Assets[i].Params);
        }
    }

    [Fact]
    public void DifferentSeedsProduceDifferentParams()
    {
        var a = AssetLibraryGenerator.Build(1);
        var b = AssetLibraryGenerator.Build(2);
        Assert.NotEqual(a.Assets[0].Params, b.Assets[0].Params);
    }

    [Fact]
    public void EveryPlantedPairReferencesTwoDistinctAssetsThatExist()
    {
        var lib = AssetLibraryGenerator.Build(5);
        var names = lib.Assets.Select(a => a.Name).ToHashSet();
        foreach (var pair in lib.PlantedPairs)
        {
            Assert.NotEqual(pair.BaseName, pair.CloneName);
            Assert.Contains(pair.BaseName, names);
            Assert.Contains(pair.CloneName, names);
        }
    }
}
