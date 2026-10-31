using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

// Captures editor windows straight from Unity's renderer (works without a visible desktop).
// Parkour/Capture/Setup Screenshots -> <project>/../workshop-media/setup/*.png
public static class EditorWindowCapture
{
    static readonly Queue<Action> steps = new Queue<Action>();
    static int waitFrames;

    [MenuItem("Parkour/Capture/Setup Screenshots")]
    public static void CaptureSetup()
    {
        steps.Clear();
        // Show Claude Code in the client dropdown, and start from a closed MCP window (the menu item toggles).
        EditorPrefs.SetString("MCPForUnity.LastSelectedClientId", "claudecode");
        foreach (var w in Resources.FindObjectsOfTypeAll<EditorWindow>().Where(w => w.titleContent.text.Contains("MCP for Unity")).ToList()) w.Close();
        Enqueue(() => EditorApplication.ExecuteMenuItem("Window/MCP for Unity/Toggle MCP Window"));
        EnqueueCapture(w => w.titleContent.text.Contains("MCP For Unity") || w.titleContent.text.Contains("MCP for Unity"), "02-mcp-window.png", 1000, 900);
        Enqueue(() => SettingsService.OpenProjectSettings("Project/AI/Unity MCP Server"));
        EnqueueCapture(w => w.GetType().Name == "ProjectSettingsWindow", "03-official-mcp.png", 1100, 760);
        Enqueue(() => EditorApplication.ExecuteMenuItem("Window/Package Manager"));
        steps.Enqueue(() => waitFrames = 900); // package list loads slowly
        EnqueueCapture(w => w.GetType().Name.Contains("PackageManager"), "01b-package-manager.png", 1200, 760);
        EditorApplication.update += Tick;
    }

    // Records the MCP for Unity connect flow as numbered frames: stopped -> Start Server -> connected.
    [MenuItem("Parkour/Capture/MCP Connect Sequence")]
    public static void CaptureConnectSequence()
    {
        steps.Clear();
        EditorPrefs.SetString("MCPForUnity.LastSelectedClientId", "claudecode");
        Func<EditorWindow, bool> mcp = w => w.titleContent.text.Contains("MCP for Unity");
        if (!Resources.FindObjectsOfTypeAll<EditorWindow>().Any(mcp))
            Enqueue(() => EditorApplication.ExecuteMenuItem("Window/MCP for Unity/Toggle MCP Window"));
        Enqueue(() => ClickButton(mcp, "Stop Server"));
        steps.Enqueue(() => waitFrames = 300);
        EnqueueCapture(mcp, "seq/02-connect-0-stopped.png", 1000, 560);
        steps.Enqueue(() => ClickButton(mcp, "Start Server"));
        for (int i = 1; i <= 8; i++)
        {
            steps.Enqueue(() => waitFrames = 100);
            int n = i;
            steps.Enqueue(() => { var w = Resources.FindObjectsOfTypeAll<EditorWindow>().FirstOrDefault(mcp); if (w) Grab(w, $"seq/02-connect-{n}.png"); });
        }
        steps.Enqueue(() => Debug.Log("[Capture] connect sequence done"));
        EditorApplication.update += Tick;
    }

    static void ClickButton(Func<EditorWindow, bool> match, string text)
    {
        var win = Resources.FindObjectsOfTypeAll<EditorWindow>().FirstOrDefault(match);
        var button = win ? win.rootVisualElement.Query<Button>().Where(b => b.text == text && b.resolvedStyle.display != DisplayStyle.None).First() : null;
        if (button == null) { Debug.LogWarning($"[Capture] button '{text}' not found"); return; }
        using (var e = NavigationSubmitEvent.GetPooled()) { e.target = button; button.SendEvent(e); }
        Debug.Log($"[Capture] clicked '{text}'");
    }

    static void Enqueue(Action a)
    {
        steps.Enqueue(a);
        steps.Enqueue(() => waitFrames = 120); // let the window lay out and repaint
    }

    static void EnqueueCapture(Func<EditorWindow, bool> match, string file, int w, int h)
    {
        steps.Enqueue(() =>
        {
            var win = Resources.FindObjectsOfTypeAll<EditorWindow>().FirstOrDefault(match);
            if (!win) { Debug.LogWarning($"[Capture] window for {file} not found"); return; }
            win.position = new Rect(40, 40, w, h);
            win.Focus();
            win.Repaint();
        });
        steps.Enqueue(() => waitFrames = 90);
        steps.Enqueue(() =>
        {
            var win = Resources.FindObjectsOfTypeAll<EditorWindow>().FirstOrDefault(match);
            if (win) Grab(win, file);
        });
    }

    static void Tick()
    {
        if (waitFrames-- > 0) { InternalEditorUtility_RepaintAll(); return; }
        if (steps.Count == 0) { EditorApplication.update -= Tick; Debug.Log("[Capture] setup screenshots done"); return; }
        try { steps.Dequeue()(); } catch (Exception e) { Debug.LogException(e); }
    }

    static void InternalEditorUtility_RepaintAll() => UnityEditorInternal.InternalEditorUtility.RepaintAllViews();

    static void Grab(EditorWindow win, string file)
    {
        var parentField = typeof(EditorWindow).GetField("m_Parent", BindingFlags.Instance | BindingFlags.NonPublic);
        var view = parentField?.GetValue(win);
        var grab = view?.GetType().GetMethod("GrabPixels", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        if (grab == null) { Debug.LogWarning("[Capture] GrabPixels not available"); return; }

        float scale = EditorGUIUtility.pixelsPerPoint;
        var viewPos = (Rect)view.GetType().GetProperty("position", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).GetValue(view);
        int w = Mathf.RoundToInt(viewPos.width * scale), h = Mathf.RoundToInt(viewPos.height * scale);
        var rt = new RenderTexture(w, h, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
        grab.Invoke(view, new object[] { rt, new Rect(0, 0, w, h) });

        RenderTexture.active = rt;
        var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
        RenderTexture.active = null;
        // GPU readback of editor views comes out upside down; flip rows.
        var px = tex.GetPixels32();
        var flipped = new Color32[px.Length];
        for (int y = 0; y < h; y++) Array.Copy(px, y * w, flipped, (h - 1 - y) * w, w);
        tex.SetPixels32(flipped);
        tex.Apply();

        string dir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "workshop-media", "setup"));
        string outPath = Path.Combine(dir, file);
        Directory.CreateDirectory(Path.GetDirectoryName(outPath));
        File.WriteAllBytes(outPath, tex.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(tex);
        rt.Release();
        Debug.Log($"[Capture] wrote {file} ({w}x{h})");
    }
}
