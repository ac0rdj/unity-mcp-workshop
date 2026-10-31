using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

// Renders slide-ready stills and fly-through frames of each workshop scene with an off-screen camera.
// Output: <project>/../workshop-media/renders/<scene>/...  (frames are stitched into mp4/gif with ffmpeg)
public static class WorkshopCapture
{
    const int Width = 1920, Height = 1080;

    static readonly string[] Scenes =
    {
        "Assets/Scenes/Parkour.unity",
        "Assets/Scenes/Examples/Track_Art.unity",
        "Assets/Scenes/Examples/Track_Mechanics.unity",
        "Assets/Scenes/Examples/Track_Polish.unity",
        "Assets/Scenes/RockDemo.unity",
        "Assets/Scenes/Examples/RockDemo_After.unity",
    };

    // Camera path along the course (position, look target).
    static readonly Vector3[] PathPos =
    {
        new Vector3(0, 3.5f, -9), new Vector3(1.5f, 5, 12), new Vector3(0, 7, 34),
        new Vector3(0, 8, 47), new Vector3(0, 13, 57), new Vector3(0, 13.5f, 68), new Vector3(0, 10, 80),
    };
    static readonly Vector3[] PathLook =
    {
        new Vector3(0, 1, 10), new Vector3(0, 3, 30), new Vector3(0, 4, 55),
        new Vector3(0, 7, 60), new Vector3(0, 10, 70), new Vector3(0, 7, 86), new Vector3(0, 8, 92),
    };

    [MenuItem("Parkour/Capture/Stills (all scenes)")]
    public static void CaptureAllStills()
    {
        foreach (var scene in Scenes) CaptureStills(scene);
    }

    [MenuItem("Parkour/Capture/Fly-throughs (all course scenes)")]
    public static void CaptureAllFlythroughs()
    {
        foreach (var scene in Scenes)
            if (!scene.Contains("RockDemo")) CaptureFlythrough(scene, 150);
    }

    public static void CaptureStills(string scenePath)
    {
        EditorSceneManager.OpenScene(scenePath);
        string dir = OutDir(scenePath);
        if (scenePath.Contains("RockDemo"))
        {
            Render(new Vector3(0, 18, -28), new Vector3(0, 0, 0), Path.Combine(dir, "overview.png"));
            Render(new Vector3(-12, 6, -14), new Vector3(2, 0, 2), Path.Combine(dir, "closeup.png"));
            return;
        }
        Render(new Vector3(-38, 30, 20), new Vector3(0, 4, 48), Path.Combine(dir, "overview.png"));
        Render(new Vector3(0, 2.4f, -2), new Vector3(0, 2, 20), Path.Combine(dir, "pov_start.png"));
        Render(new Vector3(0, 5.2f, 38), new Vector3(0, 4, 56), Path.Combine(dir, "pov_gap.png"));
        Render(new Vector3(0, 11.2f, 64), new Vector3(0, 7, 86), Path.Combine(dir, "pov_goal.png"));
        if (scenePath.Contains("Track_Art"))
        {
            Render(new Vector3(7, 12.5f, 70), new Vector3(-2, 26, 160), Path.Combine(dir, "hero_waterfall.png"));
            Render(new Vector3(-30, 8, -30), new Vector3(0, 10, 80), Path.Combine(dir, "hero_wide.png"));
            Render(new Vector3(14, 4, 26), new Vector3(-2, 2, 40), Path.Combine(dir, "hero_island.png"));
        }
    }

    public static void CaptureFlythrough(string scenePath, int frames)
    {
        EditorSceneManager.OpenScene(scenePath);
        string dir = Path.Combine(OutDir(scenePath), "frames");
        if (Directory.Exists(dir)) Directory.Delete(dir, true);
        Directory.CreateDirectory(dir);
        for (int i = 0; i < frames; i++)
        {
            float t = i / (float)(frames - 1) * (PathPos.Length - 1);
            Render(Sample(PathPos, t), Sample(PathLook, t), Path.Combine(dir, $"f{i:0000}.png"));
        }
    }

    static Vector3 Sample(Vector3[] pts, float t)
    {
        int i = Mathf.Clamp(Mathf.FloorToInt(t), 0, pts.Length - 2);
        float u = t - i;
        // Catmull-Rom for a smooth path
        Vector3 p0 = pts[Mathf.Max(i - 1, 0)], p1 = pts[i], p2 = pts[i + 1], p3 = pts[Mathf.Min(i + 2, pts.Length - 1)];
        return 0.5f * (2 * p1 + (-p0 + p2) * u + (2 * p0 - 5 * p1 + 4 * p2 - p3) * u * u + (-p0 + 3 * p1 - 3 * p2 + p3) * u * u * u);
    }

    static void Render(Vector3 position, Vector3 lookAt, string outPath)
    {
        var go = new GameObject("CaptureCam") { hideFlags = HideFlags.HideAndDontSave };
        var cam = go.AddComponent<Camera>();
        cam.fieldOfView = 60;
        cam.clearFlags = CameraClearFlags.Skybox;
        cam.farClipPlane = 9000;
        cam.allowHDR = true;
        // Scenes with a post-processing volume (the sky islands) get the same look in renders.
        bool post = Object.FindAnyObjectByType<PostProcessVolume>() != null;
        if (post)
        {
            var layer = go.AddComponent<PostProcessLayer>();
            layer.Init(SkyIslandsBuilder.Resources);
            layer.volumeLayer = 1;
            layer.antialiasingMode = PostProcessLayer.Antialiasing.SubpixelMorphologicalAntialiasing;
        }
        go.transform.position = position;
        go.transform.LookAt(lookAt);

        var rt = RenderTexture.GetTemporary(Width, Height, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB, 4);
        cam.targetTexture = rt;
        cam.Render();
        RenderTexture.active = rt;
        var tex = new Texture2D(Width, Height, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
        tex.Apply();
        File.WriteAllBytes(outPath, tex.EncodeToPNG());

        RenderTexture.active = null;
        cam.targetTexture = null;
        RenderTexture.ReleaseTemporary(rt);
        Object.DestroyImmediate(tex);
        Object.DestroyImmediate(go);
    }

    static string OutDir(string scenePath)
    {
        string dir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "workshop-media", "renders",
            Path.GetFileNameWithoutExtension(scenePath)));
        Directory.CreateDirectory(dir);
        return dir;
    }
}
