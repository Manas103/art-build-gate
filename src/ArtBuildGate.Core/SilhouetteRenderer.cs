using System.Numerics;

namespace ArtBuildGate.Core;

/// <summary>
/// Renders a character's silhouette from a fixed set of angles by
/// rasterizing the capsule stand-in for its body. This replaces what the
/// real pipeline runs headless inside Unity (see unity_tool/, committed
/// but not executed on this machine, no Unity Editor installed); see the
/// README "Honest framing" for why a software rasterizer stands in here.
/// </summary>
public static class SilhouetteRenderer
{
    public const int Resolution = 128;
    public static readonly float[] AngleDegrees = { 0, 45, 90, 135, 180, 225, 270, 315 };

    // Assumes CharacterParams proportions are authored so a HeightScale of
    // 1.0 character is about 1.0 world unit tall; see AssetLibraryGenerator.
    // 0.42 leaves headroom for HeightScale up to ~1.3 without clipping the
    // 128px frame.
    private const float PixelsPerUnit = Resolution * 0.42f;

    public static Silhouette[] RenderAllAngles(CharacterParams p)
    {
        var baseCapsules = Skeleton.Build(p);
        var result = new Silhouette[AngleDegrees.Length];
        for (int i = 0; i < AngleDegrees.Length; i++)
        {
            var rotated = Skeleton.Rotate(baseCapsules, AngleDegrees[i] * MathF.PI / 180f);
            result[i] = RenderOne(rotated);
        }
        return result;
    }

    private static Silhouette RenderOne(Capsule[] capsules)
    {
        var sil = new Silhouette(Resolution, Resolution);
        // Feet at world Y=0 map to the bottom row; X=0 is horizontally centered.
        Vector2 ToPixel(Vector3 v) => new(Resolution / 2f + v.X * PixelsPerUnit, Resolution - v.Y * PixelsPerUnit);

        foreach (var cap in capsules)
        {
            Vector2 a = ToPixel(cap.Start);
            Vector2 b = ToPixel(cap.End);
            float rPix = cap.Radius * PixelsPerUnit;

            int minX = Math.Max(0, (int)MathF.Floor(Math.Min(a.X, b.X) - rPix));
            int maxX = Math.Min(Resolution - 1, (int)MathF.Ceiling(Math.Max(a.X, b.X) + rPix));
            int minY = Math.Max(0, (int)MathF.Floor(Math.Min(a.Y, b.Y) - rPix));
            int maxY = Math.Min(Resolution - 1, (int)MathF.Ceiling(Math.Max(a.Y, b.Y) + rPix));

            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    var pixelCenter = new Vector2(x + 0.5f, y + 0.5f);
                    if (Geometry.PointSegmentDistance(pixelCenter, a, b) <= rPix)
                        sil.Set(x, y);
                }
            }
        }
        return sil;
    }
}
