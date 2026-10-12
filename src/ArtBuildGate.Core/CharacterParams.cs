namespace ArtBuildGate.Core;

/// <summary>
/// The full set of body proportions this project treats as "the skin's
/// silhouette-relevant shape". Everything here is a fraction of standing
/// height except HeightScale itself, so the proportions, not just the
/// overall size, are what two silhouettes are actually compared on.
/// </summary>
public readonly record struct CharacterParams(
    float HeightScale,
    float ShoulderWidth,
    float HipWidth,
    float TorsoLength,
    float ArmLength,
    float LegLength,
    float LimbThickness,
    float HeadSize)
{
    /// <summary>
    /// Deterministic 64-bit hash over the quantized (not raw float) fields.
    /// Quantizing to a fixed grid before hashing is what makes this safe to
    /// use as a content-addressed cache key: two CharacterParams that are
    /// visually identical but differ by float noise in the last few bits
    /// (e.g. after a round trip through a different serializer) must still
    /// hash the same, or the cache would never hit in practice.
    /// </summary>
    public ulong ContentHash()
    {
        ulong h = 1469598103934665603UL;
        foreach (var f in new[] { HeightScale, ShoulderWidth, HipWidth, TorsoLength, ArmLength, LegLength, LimbThickness, HeadSize })
        {
            int quantized = (int)Math.Round(f * 10000f);
            h ^= unchecked((ulong)quantized);
            h *= 1099511628211UL;
        }
        return h;
    }
}
