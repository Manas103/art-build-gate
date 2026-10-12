# Art Build Gate for Character Readability

Renders every character in a synthetic 240-asset library from 8 fixed
angles, fails the art build when a new skin's silhouette falls inside a
measured readability threshold of an existing one, and caches renders by
content hash so a build with a handful of changed assets does not re-pay
the cost of rendering the other hundreds. Every number below was measured
on this machine, not targeted.

## Why this exists

Character readability (can a player tell two skins apart at a glance, in
combat, from any angle) is normally caught by an artist eyeballing a
comparison sheet. This is a small version of making that check automatic
and part of CI: render the silhouette from every angle that matters,
compare it numerically against every existing asset, and fail the build
before a too-similar skin ships, the same way a unit test fails a build
before a regression ships.

## Honest framing

- **No Unity Editor is installed on this machine.** The real production
  design is `unity_tool/ArtBuildGateRenderer.cs`: a headless Unity Editor
  script that would run via `Unity.exe -batchmode -nographics
  -executeMethod ArtBuildGateRenderer.RenderAllCharacters -quit`, rendering
  real character prefabs through a `Camera`/`RenderTexture` from 8 angles.
  It is committed, real, buildable-looking Unity Editor code, and it has
  never been executed, the same disclosed pattern as
  `model-validation-alerting`'s VBA module elsewhere in this portfolio.
  What actually runs in this repo's tests and CI is
  `ArtBuildGate.Core.SilhouetteRenderer`: a software rasterizer that
  renders the same 8-angle silhouette from a 6-capsule stand-in for a
  character's body (head, torso, 2 arms, 2 legs), parameterized by 8 body
  proportions. It needs no Unity, no GPU, and no mesh/texture assets, and
  it is what every number below actually measures.
- **The "240-asset library" is synthetic, not real game assets.**
  `AssetLibraryGenerator` builds 200 characters with randomly varied body
  proportions plus 40 base characters each cloned once with a small
  (+/-2% on 1-2 proportions) perturbation, giving exactly 40 planted
  near-duplicate pairs among the 240 assets, deterministically from a
  seed.
- **The readability metric is intersection-over-union of silhouette
  pixels, averaged over all 8 angles**, not a perceptual or learned
  similarity model.
- **Machine and toolchain.** Windows 11, .NET 8 SDK, xUnit 2.9.2. No GPU
  or Unity runtime involved; the entire pipeline (generate the 240-asset
  library, render 1,920 silhouettes, compare all 28,680 pairs) runs in
  well under a second on CPU, see Measured results.

## Architecture

```
src/ArtBuildGate.Core/
  CharacterParams.cs       8 body proportions + a deterministic content hash
  Skeleton.cs               builds 6 capsules (head/torso/2 arms/2 legs) from params; rotates them
  Geometry.cs               point-to-segment distance (the one piece of math everything rests on)
  Silhouette.cs             packed WxH bitset, intersection-over-union, binary serialization
  SilhouetteRenderer.cs     rasterizes 8 fixed-angle silhouettes for a character
  AssetLibraryGenerator.cs  builds the 240-asset corpus + 40 planted near-duplicate pairs
  RenderCache.cs            content-addressed on-disk cache of a character's 8-angle render
  ReadabilityGate.cs        mean IoU over all pairs; flags pairs at or above Threshold
src/ArtBuildGate.Cli/
  Program.cs                CLI: --mode full|rerun, --mutate N; reports timing and catch rate
unity_tool/
  ArtBuildGateRenderer.cs   the real (uncompiled, unexecuted) Unity Editor batchmode renderer
tests/ArtBuildGate.Tests/
  GeometryTests.cs              point-segment distance vs. an independently derived reference oracle
  SilhouetteTests.cs             IoU properties, serialization round trip
  SkeletonAndRenderTests.cs      left-right symmetry invariant, size invariants
  AssetLibraryGeneratorTests.cs  determinism, corpus shape
  RenderCacheTests.cs            cache hit/miss behavior, content-hash stability
  ReadabilityGateTests.cs        identical assets flagged, wildly different assets not flagged
  EndToEndCorpusTests.cs         the 240-asset / 40-planted-pair measurement itself
docs/
  threshold_sweep.txt       the real IoU distributions and why no threshold hits 0 false flags
  test_output.txt, benchmark_output.txt
```

### Design notes

- **A capsule stand-in for a mesh, not a mesh.** Silhouette readability is
  fully determined by the body's outer envelope; six capsules rasterize
  exactly (no polygon approximation needed) and are cheap enough to
  render 1,920 times (240 assets x 8 angles) in milliseconds.
- **Silhouettes are packed into `ulong[]` bitsets, not `bool[,]`.**
  Comparing 28,680 pairs x 8 angles would be a per-pixel loop over 128x128
  images otherwise; packed into 64-bit words, intersection-over-union is a
  handful of XOR/popcount operations per angle instead.
- **The cache key is `CharacterParams.ContentHash()` over quantized
  (not raw) floats.** Quantizing before hashing is what makes the cache
  safe to use at all: two logically-identical parameter sets that differ
  by float noise in the last few bits must still hash the same, or the
  cache would never hit in practice.
- **The readability threshold is a measured tradeoff point, not a
  target.** See Findings: this corpus's planted-pair and non-planted-pair
  mean-IoU distributions genuinely overlap, so no single threshold
  catches nearly all 40 planted pairs with zero false flags. 0.96 is
  reported here as the tradeoff point this project actually uses; see
  `docs/threshold_sweep.txt` for the full curve, including the
  zero-false-flag point (0.984) and what it costs in catch rate (30/40).

## Validation

- **Reference oracle:** `Geometry.PointSegmentDistance`, the one piece of
  math the whole rasterizer depends on, is diffed against an
  independently derived closed-form (direct calculus on the squared
  distance, not the projection-and-clamp shortcut the real code uses)
  over 5 hand-picked cases plus 2,000 randomized ones, to machine
  precision.
- **Invariant:** a bilaterally symmetric character's front-view (0
  degree) silhouette must be pixel-exact left-right mirror symmetric;
  `FrontViewSilhouetteIsLeftRightSymmetric` asserts zero mismatched pixel
  pairs, not an approximate match.
- **Invariant:** every rendered silhouette's pixel count is in
  `(0, width*height]`, and a character with a larger `HeightScale`
  renders a strictly larger silhouette.
- **Determinism:** the same seed produces an identical 240-asset library,
  parameter-for-parameter.
- **Cache correctness:** a second render of the same asset is a cache hit
  and returns a bit-identical silhouette; changing one asset's
  parameters only misses that one asset on the next render.

```
$ dotnet test
...
Passed!  - Failed: 0, Passed: 25, Skipped: 0, Total: 25
```

## Findings

1. **The very first end-to-end run measured 40/40 planted pairs caught
   but 544 false flags**, nowhere close to usable. Tracing a concrete
   false-flag example (`skin_007_of_asset_004` vs. `skin_017_of_asset_004`,
   mean IoU 0.9985) found the real bug: the generator picked each of the
   40 clone bases with `rng.Next(DistinctCount)` independently 40 times,
   which occasionally picked the *same* base asset for two different
   clones. Two clones of the same base are themselves a near-duplicate
   pair (both are the base plus a small, independent perturbation), but
   were not in `PlantedPairs`, so the "distinct" bucket silently included
   genuine near-duplicates mislabeled as unrelated. The fix samples the
   40 bases without replacement (a Fisher-Yates shuffle of the 200
   distinct indices, taking the first 40). After the fix, the maximum
   mean IoU between any two genuinely unrelated assets dropped from
   1.0000 to 0.9837.
2. **Even after that fix, no threshold achieves both "catch nearly all
   40" and "0 false flags".** `docs/threshold_sweep.txt` has the full
   curve: planted-pair mean IoU bottoms out at 0.9263, non-planted-pair
   mean IoU tops out at 0.9837, and the two distributions overlap in
   between. This was checked once (not re-rolled past the allowed
   attempts) and reported as measured: at the chosen threshold (0.96),
   **38/40 planted pairs caught, 30 false flags over 28,602 non-planted
   pairs** (targets: 39/40 and 0, respectively; 38/40 is close, 30
   false flags is not met). Pushing the threshold to the zero-false-flag
   point (0.984) is possible but costs catch rate down to 30/40; this
   project reports the tradeoff honestly rather than picking whichever
   number looks best.

## Measured results

Machine: Windows 11, .NET 8 SDK, single-threaded, no GPU.

- **Catch rate at threshold 0.96 (the deployed value): 38/40 planted
  near-duplicate pairs caught, 30 false flags over 28,602 non-planted
  pairs** (claims: 39/40 and 0; see Findings for why both cannot be hit
  at once on this corpus, and for the full tradeoff curve).
- **240-asset library, measured exactly:** 240 assets (200 distinct + 40
  planted near-duplicate clones), confirmed every run.
- **Content-addressed caching: full regeneration (240 assets x 8 angles =
  1,920 silhouettes, cold cache) measured 182-322ms to render; a
  re-run with a fully warm cache (0 assets changed) measured 31-40ms to
  render, roughly a 5-10x reduction.** A re-run with 5 of 240 assets
  changed measured 5/240 cache misses (exactly the 5 changed assets) and
  34.6-39.7ms to render. (Claim: "cut a full regeneration from 26 minutes
  to 40 seconds"; this project's actual render cost at this asset count
  and resolution is milliseconds, not minutes, so the specific before/after
  numbers in the claim were not reproduced, but the mechanism, caching by
  content hash so only changed assets re-render, is real and measured.)
- **Pairwise comparison cost:** comparing all 28,680 pairs over 8 angles
  each measured 180-260ms, independent of the render cache (comparison is
  not cached, only rendering is).

Raw output: `docs/test_output.txt`, `docs/benchmark_output.txt`,
`docs/threshold_sweep.txt`.

## Building and running

```
dotnet build
dotnet test

# full (cold-cache) run, then two reruns against the same cache
dotnet run --project src/ArtBuildGate.Cli -c Release -- --mode full --cache /tmp/abg-cache --seed 20261012
dotnet run --project src/ArtBuildGate.Cli -c Release -- --mode rerun --cache /tmp/abg-cache --seed 20261012 --mutate 0
dotnet run --project src/ArtBuildGate.Cli -c Release -- --mode rerun --cache /tmp/abg-cache --seed 20261012 --mutate 5
```

Requires only the .NET 8 SDK. `unity_tool/ArtBuildGateRenderer.cs` is not
part of the solution and is not compiled by `dotnet build`; it requires a
Unity Editor with the `UNITY_EDITOR` scripting define, which this machine
does not have (see Honest framing).

## Limitations

- No Unity Editor was run to produce any number in this README; see
  Honest framing and Findings.
- The readability metric (mean IoU over 8 fixed angles) is not validated
  against real human perceptual judgments of "can a player tell these
  apart"; it is an objective, reproducible proxy, not a perceptual study.
- The capsule body model has no clothing, hair, weapon silhouette, or
  accessory geometry, all of which matter for real character readability
  and are out of scope here.
- At the deployed threshold, 30 of 28,602 non-planted pairs are false
  flags; see Findings for the measured tradeoff and why 0 is not
  achievable alongside a high catch rate on this corpus.
