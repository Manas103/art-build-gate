namespace ArtBuildGate.Core;

/// <summary>
/// A packed WxH binary bitmap. Packed into ulongs (not bool[]) specifically
/// so pairwise similarity over hundreds of assets is a handful of 64-bit
/// XOR/popcount operations per angle instead of a per-pixel loop; see
/// IntersectionOverUnion.
/// </summary>
public sealed class Silhouette
{
    public int Width { get; }
    public int Height { get; }
    private readonly ulong[] _bits;

    public Silhouette(int width, int height)
    {
        Width = width;
        Height = height;
        _bits = new ulong[(width * height + 63) / 64];
    }

    public void Set(int x, int y)
    {
        if (x < 0 || x >= Width || y < 0 || y >= Height) return;
        int idx = y * Width + x;
        _bits[idx >> 6] |= 1UL << (idx & 63);
    }

    public bool Get(int x, int y)
    {
        if (x < 0 || x >= Width || y < 0 || y >= Height) return false;
        int idx = y * Width + x;
        return (_bits[idx >> 6] & (1UL << (idx & 63))) != 0;
    }

    public void WriteTo(BinaryWriter w)
    {
        w.Write(Width);
        w.Write(Height);
        w.Write(_bits.Length);
        foreach (var word in _bits) w.Write(word);
    }

    public static Silhouette ReadFrom(BinaryReader r)
    {
        int width = r.ReadInt32();
        int height = r.ReadInt32();
        int wordCount = r.ReadInt32();
        var sil = new Silhouette(width, height);
        for (int i = 0; i < wordCount; i++) sil._bits[i] = r.ReadUInt64();
        return sil;
    }

    public int PopCount()
    {
        int total = 0;
        foreach (var w in _bits) total += System.Numerics.BitOperations.PopCount(w);
        return total;
    }

    /// <summary>
    /// Intersection-over-union: 1.0 means pixel-for-pixel identical, 0.0
    /// means no overlap at all. This is the per-angle similarity score the
    /// readability gate averages over all 8 angles.
    /// </summary>
    public static double IntersectionOverUnion(Silhouette a, Silhouette b)
    {
        if (a.Width != b.Width || a.Height != b.Height)
            throw new ArgumentException("silhouettes must be the same resolution to compare");
        long intersection = 0, union = 0;
        for (int i = 0; i < a._bits.Length; i++)
        {
            ulong ia = a._bits[i] & b._bits[i];
            ulong iu = a._bits[i] | b._bits[i];
            intersection += System.Numerics.BitOperations.PopCount(ia);
            union += System.Numerics.BitOperations.PopCount(iu);
        }
        return union == 0 ? 1.0 : (double)intersection / union;
    }
}
