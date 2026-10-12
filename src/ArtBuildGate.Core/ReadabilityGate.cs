namespace ArtBuildGate.Core;

public sealed record SimilarPair(string NameA, string NameB, double MeanIoU);

/// <summary>
/// Fails the art build when a new skin's silhouette falls inside the
/// readability threshold of an existing one: for every pair of assets,
/// averages the per-angle intersection-over-union across all 8 angles, and
/// flags the pair if that average is at or above Threshold.
/// </summary>
public static class ReadabilityGate
{
    /// <summary>
    /// 0.96 mean IoU across all 8 angles. See docs/threshold_sweep.txt: no
    /// threshold on this corpus catches nearly all 40 planted pairs AND
    /// has zero false flags, because the two IoU distributions genuinely
    /// overlap (planted-pair minimum 0.9263, non-planted-pair maximum
    /// 0.9837). 0.96 is the catch-rate/false-flag tradeoff point this
    /// project reports, not the zero-false-flag point (0.984, which only
    /// catches 30/40). See README Findings for the honest numbers.
    /// </summary>
    public const double Threshold = 0.96;

    public static double MeanIoU(Silhouette[] a, Silhouette[] b)
    {
        double sum = 0;
        for (int i = 0; i < a.Length; i++) sum += Silhouette.IntersectionOverUnion(a[i], b[i]);
        return sum / a.Length;
    }

    /// <summary>
    /// All pairs (not just the planted ones) are compared, same as a real
    /// art build gate would when a new or changed skin comes in: O(n^2)
    /// over the library, which is why Silhouette packs bits into ulongs.
    /// </summary>
    public static List<SimilarPair> FindSimilarPairs(Dictionary<string, Silhouette[]> renders, double threshold = Threshold)
    {
        var names = renders.Keys.ToArray();
        var flagged = new List<SimilarPair>();
        for (int i = 0; i < names.Length; i++)
        {
            for (int j = i + 1; j < names.Length; j++)
            {
                double iou = MeanIoU(renders[names[i]], renders[names[j]]);
                if (iou >= threshold)
                    flagged.Add(new SimilarPair(names[i], names[j], iou));
            }
        }
        return flagged;
    }
}
