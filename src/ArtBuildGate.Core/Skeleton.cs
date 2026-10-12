using System.Numerics;

namespace ArtBuildGate.Core;

/// <summary>A single capsule: a thick line segment from Start to End with radius Radius.</summary>
public readonly record struct Capsule(Vector3 Start, Vector3 End, float Radius);

/// <summary>
/// Builds the capsule stand-in for a character's body from its proportions.
/// This is deliberately not a real mesh: a silhouette, which is all the
/// readability check needs, is fully determined by the outer envelope of
/// head/torso/arms/legs, and seven capsules are cheap to rasterize exactly
/// instead of approximately.
/// </summary>
public static class Skeleton
{
    public static Capsule[] Build(CharacterParams p)
    {
        float s = p.HeightScale;
        float legLen = p.LegLength * s;
        float torsoLen = p.TorsoLength * s;
        float armLen = p.ArmLength * s;
        float headR = p.HeadSize * s * 0.5f;
        float limbR = p.LimbThickness * s * 0.5f;
        float hipHalf = p.HipWidth * s * 0.5f;
        float shoulderHalf = p.ShoulderWidth * s * 0.5f;

        float hipY = legLen;
        float shoulderY = legLen + torsoLen;
        float headY = shoulderY + headR;

        return new[]
        {
            // legs
            new Capsule(new Vector3(-hipHalf, 0, 0), new Vector3(-hipHalf, hipY, 0), limbR),
            new Capsule(new Vector3(hipHalf, 0, 0), new Vector3(hipHalf, hipY, 0), limbR),
            // torso
            new Capsule(new Vector3(0, hipY, 0), new Vector3(0, shoulderY, 0), hipHalf * 0.9f),
            // arms (hang down from the shoulder)
            new Capsule(new Vector3(-shoulderHalf, shoulderY, 0), new Vector3(-shoulderHalf, shoulderY - armLen, 0), limbR * 0.85f),
            new Capsule(new Vector3(shoulderHalf, shoulderY, 0), new Vector3(shoulderHalf, shoulderY - armLen, 0), limbR * 0.85f),
            // head (a zero-length capsule is a sphere)
            new Capsule(new Vector3(0, headY, 0), new Vector3(0, headY, 0), headR),
        };
    }

    /// <summary>Rotates every capsule about the vertical (Y) axis by angleRadians.</summary>
    public static Capsule[] Rotate(Capsule[] capsules, float angleRadians)
    {
        float cos = (float)Math.Cos(angleRadians);
        float sin = (float)Math.Sin(angleRadians);
        Vector3 Rot(Vector3 v) => new(v.X * cos + v.Z * sin, v.Y, -v.X * sin + v.Z * cos);
        var outCaps = new Capsule[capsules.Length];
        for (int i = 0; i < capsules.Length; i++)
        {
            outCaps[i] = new Capsule(Rot(capsules[i].Start), Rot(capsules[i].End), capsules[i].Radius);
        }
        return outCaps;
    }
}
