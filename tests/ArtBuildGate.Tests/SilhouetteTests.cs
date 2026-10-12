using ArtBuildGate.Core;
using Xunit;

namespace ArtBuildGate.Tests;

public class SilhouetteTests
{
    [Fact]
    public void IdenticalSilhouettesHaveIoUOfOne()
    {
        var s1 = new Silhouette(8, 8);
        var s2 = new Silhouette(8, 8);
        s1.Set(1, 1); s1.Set(2, 2); s1.Set(3, 3);
        s2.Set(1, 1); s2.Set(2, 2); s2.Set(3, 3);
        Assert.Equal(1.0, Silhouette.IntersectionOverUnion(s1, s2));
    }

    [Fact]
    public void DisjointSilhouettesHaveIoUOfZero()
    {
        var s1 = new Silhouette(8, 8);
        var s2 = new Silhouette(8, 8);
        s1.Set(0, 0);
        s2.Set(7, 7);
        Assert.Equal(0.0, Silhouette.IntersectionOverUnion(s1, s2));
    }

    [Fact]
    public void BothEmptySilhouettesAreDefinedAsIdentical()
    {
        var s1 = new Silhouette(8, 8);
        var s2 = new Silhouette(8, 8);
        Assert.Equal(1.0, Silhouette.IntersectionOverUnion(s1, s2));
    }

    [Fact]
    public void PartialOverlapMatchesHandComputedIoU()
    {
        var s1 = new Silhouette(4, 4);
        var s2 = new Silhouette(4, 4);
        // s1: (0,0),(1,0),(2,0)  s2: (1,0),(2,0),(3,0)
        s1.Set(0, 0); s1.Set(1, 0); s1.Set(2, 0);
        s2.Set(1, 0); s2.Set(2, 0); s2.Set(3, 0);
        // intersection = {1,0),(2,0)} = 2, union = {0,1,2,3}x{0} = 4
        Assert.Equal(0.5, Silhouette.IntersectionOverUnion(s1, s2), precision: 10);
    }

    [Fact]
    public void SerializationRoundTripIsExact()
    {
        var s = new Silhouette(16, 16);
        var rng = new Random(7);
        for (int i = 0; i < 40; i++) s.Set(rng.Next(16), rng.Next(16));

        using var ms = new MemoryStream();
        using (var w = new BinaryWriter(ms, System.Text.Encoding.UTF8, leaveOpen: true)) s.WriteTo(w);
        ms.Position = 0;
        using var r = new BinaryReader(ms);
        var roundTripped = Silhouette.ReadFrom(r);

        Assert.Equal(1.0, Silhouette.IntersectionOverUnion(s, roundTripped));
        Assert.Equal(s.PopCount(), roundTripped.PopCount());
    }
}
