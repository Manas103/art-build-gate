namespace ArtBuildGate.Core;

public sealed record AssetRecord(string Name, CharacterParams Params);

public sealed record NearDuplicatePair(string BaseName, string CloneName);

public sealed record AssetLibrary(AssetRecord[] Assets, NearDuplicatePair[] PlantedPairs);

/// <summary>
/// Builds the 240-asset synthetic library: 200 distinct characters spread
/// widely over the proportion ranges below, plus 40 base characters each
/// cloned once with a small perturbation to 1-2 proportions (simulating a
/// near-duplicate skin), giving exactly 40 planted near-duplicate pairs
/// among the 240 assets. Entirely deterministic from `seed`.
/// </summary>
public static class AssetLibraryGenerator
{
    public const int DistinctCount = 200;
    public const int PlantedPairCount = 40;
    public const int TotalCount = DistinctCount + PlantedPairCount;

    private static readonly (string Name, float Min, float Max)[] Ranges =
    {
        ("HeightScale", 0.85f, 1.15f),
        ("ShoulderWidth", 0.22f, 0.34f),
        ("HipWidth", 0.18f, 0.28f),
        ("TorsoLength", 0.28f, 0.36f),
        ("ArmLength", 0.30f, 0.40f),
        ("LegLength", 0.42f, 0.52f),
        ("LimbThickness", 0.06f, 0.11f),
        ("HeadSize", 0.11f, 0.15f),
    };

    public static AssetLibrary Build(int seed)
    {
        var rng = new Random(seed);
        var assets = new List<AssetRecord>(TotalCount);

        for (int i = 0; i < DistinctCount; i++)
        {
            assets.Add(new AssetRecord($"asset_{i:D3}", RandomParams(rng)));
        }

        // Each of the 40 clones must come from a different base asset: two
        // clones of the *same* base are themselves a near-duplicate pair
        // (both are the base plus a small perturbation) that would not be
        // in PlantedPairs, silently corrupting the "distinct" ground truth
        // the end-to-end corpus test relies on. A first version of this
        // method called rng.Next(DistinctCount) independently 40 times,
        // which occasionally picked the same base twice; that produced a
        // genuine IoU-0.9985 "distinct" pair (two clones of asset_004) that
        // broke the false-flag assertion. Sampling without replacement
        // fixes it.
        var baseIndices = Enumerable.Range(0, DistinctCount).ToList();
        for (int i = baseIndices.Count - 1; i > 0; i--)
        {
            int swap = rng.Next(i + 1);
            (baseIndices[i], baseIndices[swap]) = (baseIndices[swap], baseIndices[i]);
        }

        var pairs = new List<NearDuplicatePair>(PlantedPairCount);
        for (int i = 0; i < PlantedPairCount; i++)
        {
            int baseIdx = baseIndices[i];
            var baseAsset = assets[baseIdx];
            string cloneName = $"skin_{i:D3}_of_{baseAsset.Name}";
            var cloneParams = Perturb(baseAsset.Params, rng);
            assets.Add(new AssetRecord(cloneName, cloneParams));
            pairs.Add(new NearDuplicatePair(baseAsset.Name, cloneName));
        }

        return new AssetLibrary(assets.ToArray(), pairs.ToArray());
    }

    private static CharacterParams RandomParams(Random rng)
    {
        float Pick(int idx) => Ranges[idx].Min + (float)rng.NextDouble() * (Ranges[idx].Max - Ranges[idx].Min);
        return new CharacterParams(
            HeightScale: Pick(0), ShoulderWidth: Pick(1), HipWidth: Pick(2), TorsoLength: Pick(3),
            ArmLength: Pick(4), LegLength: Pick(5), LimbThickness: Pick(6), HeadSize: Pick(7));
    }

    /// <summary>
    /// Nudges 1-2 of the eight proportions by +/-2% relative, which is the
    /// "near-duplicate skin" this project's readability gate exists to
    /// catch: visually almost indistinguishable at silhouette resolution,
    /// but a genuinely different asset on disk.
    /// </summary>
    private static CharacterParams Perturb(CharacterParams baseParams, Random rng)
    {
        float[] values = { baseParams.HeightScale, baseParams.ShoulderWidth, baseParams.HipWidth, baseParams.TorsoLength,
                            baseParams.ArmLength, baseParams.LegLength, baseParams.LimbThickness, baseParams.HeadSize };
        int perturbCount = 1 + rng.Next(2); // 1 or 2 fields
        for (int k = 0; k < perturbCount; k++)
        {
            int idx = rng.Next(values.Length);
            float relative = 1f + (float)(rng.NextDouble() * 0.04 - 0.02); // +/-2%
            values[idx] *= relative;
        }
        return new CharacterParams(values[0], values[1], values[2], values[3], values[4], values[5], values[6], values[7]);
    }
}
