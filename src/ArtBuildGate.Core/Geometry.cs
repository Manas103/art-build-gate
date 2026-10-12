using System.Numerics;

namespace ArtBuildGate.Core;

public static class Geometry
{
    /// <summary>
    /// Shortest distance from point p to the segment [a, b], clamped
    /// projection form. This is the one piece of math the whole
    /// rasterizer depends on, so it is cross-checked in
    /// GeometryTests.PointSegmentDistance_MatchesIndependentFormula
    /// against an algebraically different derivation (minimizing the
    /// squared distance along the parametrized line directly, rather than
    /// projecting and clamping) rather than trusted on inspection.
    /// </summary>
    public static float PointSegmentDistance(Vector2 p, Vector2 a, Vector2 b)
    {
        Vector2 ab = b - a;
        float lenSq = ab.LengthSquared();
        if (lenSq < 1e-12f) return Vector2.Distance(p, a);
        float t = Vector2.Dot(p - a, ab) / lenSq;
        t = Math.Clamp(t, 0f, 1f);
        Vector2 closest = a + t * ab;
        return Vector2.Distance(p, closest);
    }
}
