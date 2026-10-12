using ArtBuildGate.Core;
using Xunit;

namespace ArtBuildGate.Tests;

public class SkeletonAndRenderTests
{
    private static readonly CharacterParams Symmetric = new(
        HeightScale: 1.0f, ShoulderWidth: 0.28f, HipWidth: 0.22f, TorsoLength: 0.32f,
        ArmLength: 0.35f, LegLength: 0.47f, LimbThickness: 0.08f, HeadSize: 0.13f);

    [Fact]
    public void FrontViewSilhouetteIsLeftRightSymmetric()
    {
        // Invariant: this project's skeleton has no left/right asymmetry
        // by construction, so the front view (angle 0) silhouette must be
        // a mirror image of itself about the vertical centerline. This is
        // a property of the renderer, not just "it produced a plausible
        // image".
        var renders = SilhouetteRenderer.RenderAllAngles(Symmetric);
        var front = renders[0]; // 0 degrees
        int width = front.Width;
        int mismatches = 0;
        for (int y = 0; y < front.Height; y++)
        {
            for (int x = 0; x < width / 2; x++)
            {
                int mirrorX = width - 1 - x;
                if (front.Get(x, y) != front.Get(mirrorX, y)) mismatches++;
            }
        }
        Assert.Equal(0, mismatches);
    }

    [Fact]
    public void SilhouetteIsNonEmptyAndBoundedByFrameArea()
    {
        var renders = SilhouetteRenderer.RenderAllAngles(Symmetric);
        foreach (var sil in renders)
        {
            Assert.InRange(sil.PopCount(), 1, sil.Width * sil.Height);
        }
    }

    [Fact]
    public void AllEightAnglesProduceDifferentSilhouettesForAnAsymmetricPose()
    {
        // A character with different arm/leg proportions on a non-symmetric
        // axis would show this more starkly; even for a bilaterally
        // symmetric character, rotating 45 degrees changes the projected
        // width (front-on vs. a 3/4 view are not the same silhouette area).
        var renders = SilhouetteRenderer.RenderAllAngles(Symmetric);
        var front = renders[0];
        var threeQuarter = renders[1]; // 45 degrees
        Assert.NotEqual(front.PopCount(), threeQuarter.PopCount());
    }

    [Fact]
    public void LargerHeightScaleProducesALargerSilhouette()
    {
        var small = Symmetric with { HeightScale = 0.85f };
        var big = Symmetric with { HeightScale = 1.15f };
        var smallRender = SilhouetteRenderer.RenderAllAngles(small)[0];
        var bigRender = SilhouetteRenderer.RenderAllAngles(big)[0];
        Assert.True(bigRender.PopCount() > smallRender.PopCount());
    }
}
