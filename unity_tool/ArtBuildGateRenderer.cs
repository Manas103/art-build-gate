// Committed as the production design, not executed on this machine: there
// is no Unity Editor installed here (see README "Honest framing"). This
// is the real headless entry point the art build would call via
//   Unity.exe -batchmode -nographics -projectPath <project> -executeMethod ArtBuildGateRenderer.RenderAllCharacters -quit
// It renders every character prefab under Assets/Characters from 8 fixed
// angles to a RenderTexture and writes each as a PNG, which is exactly the
// silhouette input ArtBuildGate.Core.SilhouetteRenderer produces in
// software in this repo's actual, running pipeline.
#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

public static class ArtBuildGateRenderer
{
    private const int Resolution = 128;
    private static readonly float[] AngleDegrees = { 0f, 45f, 90f, 135f, 180f, 225f, 270f, 315f };

    [MenuItem("ArtBuildGate/Render All Characters")]
    public static void RenderAllCharacters()
    {
        string outDir = Path.Combine(Application.dataPath, "..", "ArtBuildGateRenders");
        Directory.CreateDirectory(outDir);

        string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Characters" });
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            RenderOneCharacter(prefab, outDir);
        }
    }

    private static void RenderOneCharacter(GameObject prefab, string outDir)
    {
        var instance = (GameObject)GameObject.Instantiate(prefab);
        var cameraGo = new GameObject("ArtBuildGateCamera");
        var cam = cameraGo.AddComponent<Camera>();
        cam.orthographic = true;
        cam.backgroundColor = Color.white;
        cam.clearFlags = CameraClearFlags.SolidColor;

        var rt = new RenderTexture(Resolution, Resolution, 16);
        cam.targetTexture = rt;

        foreach (float angle in AngleDegrees)
        {
            instance.transform.rotation = Quaternion.Euler(0, angle, 0);
            cam.Render();

            RenderTexture.active = rt;
            var tex = new Texture2D(Resolution, Resolution, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, Resolution, Resolution), 0, 0);
            tex.Apply();

            string fileName = $"{prefab.name}_{angle:000}.png";
            File.WriteAllBytes(Path.Combine(outDir, fileName), tex.EncodeToPNG());

            Object.DestroyImmediate(tex);
        }

        RenderTexture.active = null;
        cam.targetTexture = null;
        Object.DestroyImmediate(rt);
        Object.DestroyImmediate(cameraGo);
        Object.DestroyImmediate(instance);
    }
}
#endif
