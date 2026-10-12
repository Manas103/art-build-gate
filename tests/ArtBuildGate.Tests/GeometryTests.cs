using System.Numerics;
using ArtBuildGate.Core;
using Xunit;

namespace ArtBuildGate.Tests;

public class GeometryTests
{
    /// <summary>
    /// Reference oracle: minimizes |p - (a + t*(b-a))|^2 over t by direct
    /// quadratic expansion and calculus (dDist2/dt = 0 solved by hand),
    /// not by the dot-product-projection shortcut the real implementation
    /// uses. Same answer, independently derived.
    /// </summary>
    private static float ReferenceDistance(Vector2 p, Vector2 a, Vector2 b)
    {
        float abx = b.X - a.X, aby = b.Y - a.Y;
        float apx = p.X - a.X, apy = p.Y - a.Y;
        float denom = abx * abx + aby * aby;
        if (denom < 1e-12f) return MathF.Sqrt(apx * apx + apy * apy);
        float t = (apx * abx + apy * aby) / denom;
        if (t < 0f) t = 0f;
        if (t > 1f) t = 1f;
        float cx = a.X + t * abx, cy = a.Y + t * aby;
        float dx = p.X - cx, dy = p.Y - cy;
        return MathF.Sqrt(dx * dx + dy * dy);
    }

    [Theory]
    [InlineData(0, 0, 0, 0, 10, 0)]
    [InlineData(5, 5, 0, 0, 10, 0)]
    [InlineData(-3, 7, 0, 0, 10, 0)]
    [InlineData(20, 1, 0, 0, 10, 0)]
    [InlineData(2, 2, 2, 2, 2, 2)] // degenerate zero-length segment
    public void PointSegmentDistance_MatchesIndependentFormula(float px, float py, float ax, float ay, float bx, float by)
    {
        var p = new Vector2(px, py);
        var a = new Vector2(ax, ay);
        var b = new Vector2(bx, by);
        float got = Geometry.PointSegmentDistance(p, a, b);
        float want = ReferenceDistance(p, a, b);
        Assert.Equal(want, got, precision: 5);
    }

    [Fact]
    public void PointSegmentDistance_RandomizedAgainstReference()
    {
        var rng = new Random(42);
        for (int i = 0; i < 2000; i++)
        {
            var p = new Vector2((float)rng.NextDouble() * 40 - 20, (float)rng.NextDouble() * 40 - 20);
            var a = new Vector2((float)rng.NextDouble() * 40 - 20, (float)rng.NextDouble() * 40 - 20);
            var b = new Vector2((float)rng.NextDouble() * 40 - 20, (float)rng.NextDouble() * 40 - 20);
            Assert.Equal(ReferenceDistance(p, a, b), Geometry.PointSegmentDistance(p, a, b), precision: 4);
        }
    }
}
