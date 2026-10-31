using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Generates the workshop scenes from code so they're easy to rebuild and easy for an agent to read.
//   Parkour/Rebuild Parkour Scene  -> Assets/Scenes/Parkour.unity  (the base game)
//   Parkour/Rebuild Rock Demo Scene -> Assets/Scenes/RockDemo.unity (empty grass plane for the MCP demo)
public static class LevelBuilder
{
    const string MaterialDir = "Assets/Materials";
    const string SceneDir = "Assets/Scenes";
    const string ParkourScene = SceneDir + "/Parkour.unity";
    const string RockDemoScene = SceneDir + "/RockDemo.unity";

    // Batch-mode entry point: builds both scenes.
    public static void BuildAll()
    {
        if (!AssetDatabase.IsValidFolder(SceneDir)) AssetDatabase.CreateFolder("Assets", "Scenes");
        BuildRockDemo();
        BuildParkour();
        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(ParkourScene, true),
            new EditorBuildSettingsScene(RockDemoScene, true),
        };
        AssetDatabase.SaveAssets();
    }

    [MenuItem("Parkour/Rebuild Parkour Scene")]
    public static void BuildParkour()
    {
        if (!AssetDatabase.IsValidFolder(SceneDir)) AssetDatabase.CreateFolder("Assets", "Scenes");
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        Object.DestroyImmediate(GameObject.Find("Main Camera"));
        RenderSettings.fog = true;
        RenderSettings.fogColor = new Color(0.62f, 0.75f, 0.9f);
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogStartDistance = 60f;
        RenderSettings.fogEndDistance = 220f;

        Material start = Mat("Start", new Color(0.35f, 0.75f, 0.45f));
        Material platform = Mat("Platform", new Color(0.82f, 0.82f, 0.86f));
        Material blastPad = Mat("BlastPlatform", new Color(1f, 0.6f, 0.2f));
        Material checkpoint = Mat("Checkpoint", new Color(0.3f, 0.6f, 1f, 0.35f), transparent: true);
        Material goal = Mat("Goal", new Color(1f, 0.85f, 0.2f));

        var level = new GameObject("Level").transform;

        // Section 1: warm-up jumps (no blaster needed).
        Block("Start", level, new Vector3(0, 0, 0), new Vector3(10, 1, 10), start);
        Block("Jump_01", level, new Vector3(0, 0, 10), new Vector3(4, 1, 4), platform);
        Block("Jump_02", level, new Vector3(3, 1, 16), new Vector3(4, 1, 4), platform);
        Block("Jump_03", level, new Vector3(0, 2, 22), new Vector3(4, 1, 4), platform);
        Block("Jump_04", level, new Vector3(-3, 3, 28), new Vector3(4, 1, 4), platform);
        Block("Rest_01", level, new Vector3(0, 3, 36), new Vector3(8, 1, 8), platform);
        CheckpointVolume("Checkpoint_01", level, new Vector3(0, 3, 36), checkpoint);

        // Section 2: long gap — jump, then blast forward.
        Block("BlastGap_Landing", level, new Vector3(0, 3, 55), new Vector3(6, 1, 6), blastPad);

        // Section 3: tall wall — jump, then blast upward.
        Block("Wall", level, new Vector3(0, 6, 58.5f), new Vector3(6, 7, 1), platform);
        Block("WallTop", level, new Vector3(0, 9, 63), new Vector3(6, 1, 8), blastPad);
        CheckpointVolume("Checkpoint_02", level, new Vector3(0, 9, 63), checkpoint);

        // Section 4: final drop-gap to the goal.
        Block("Goal_Platform", level, new Vector3(0, 6, 86), new Vector3(10, 1, 10), start);
        Block("Goal_Pole", level, new Vector3(0, 9, 88), new Vector3(0.3f, 6, 0.3f), goal);
        Block("Goal_Flag", level, new Vector3(1, 11, 88), new Vector3(2, 1.2f, 0.1f), goal);
        var goalTrigger = new GameObject("Goal_Trigger");
        goalTrigger.transform.SetParent(level);
        goalTrigger.transform.position = new Vector3(0, 8, 86);
        var goalBox = goalTrigger.AddComponent<BoxCollider>();
        goalBox.isTrigger = true;
        goalBox.size = new Vector3(10, 4, 10);
        goalTrigger.AddComponent<Goal>();

        // Empty parents for students / agents to fill.
        new GameObject("Props");
        new GameObject("Collectibles");

        // Player
        var player = new GameObject("Player");
        player.tag = "Player";
        player.transform.position = new Vector3(0, 1.6f, -2);
        var cc = player.AddComponent<CharacterController>();
        cc.height = 1.8f;
        cc.radius = 0.4f;
        var pivot = new GameObject("CameraPivot").transform;
        pivot.SetParent(player.transform, false);
        pivot.localPosition = new Vector3(0, 0.7f, 0);
        var cam = new GameObject("Main Camera");
        cam.tag = "MainCamera";
        cam.transform.SetParent(pivot, false);
        cam.AddComponent<Camera>().fieldOfView = 80f;
        cam.AddComponent<AudioListener>();
        var motor = player.AddComponent<PlayerMotor>();
        motor.cameraPivot = pivot;
        var blaster = player.AddComponent<Blaster>();

        var hud = new GameObject("GameHUD").AddComponent<GameHUD>();
        hud.blaster = blaster;

        EditorSceneManager.SaveScene(scene, ParkourScene);
    }

    [MenuItem("Parkour/Rebuild Rock Demo Scene")]
    public static void BuildRockDemo()
    {
        if (!AssetDatabase.IsValidFolder(SceneDir)) AssetDatabase.CreateFolder("Assets", "Scenes");
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        var cam = GameObject.Find("Main Camera").transform;
        cam.SetPositionAndRotation(new Vector3(0, 18, -28), Quaternion.Euler(32, 0, 0));

        var grass = GameObject.CreatePrimitive(PrimitiveType.Plane);
        grass.name = "Grass";
        grass.transform.localScale = new Vector3(4, 1, 4);
        grass.GetComponent<Renderer>().sharedMaterial = Mat("Grass", new Color(0.36f, 0.6f, 0.25f));

        new GameObject("Rocks");

        EditorSceneManager.SaveScene(scene, RockDemoScene);
    }

    static GameObject Block(string name, Transform parent, Vector3 position, Vector3 size, Material material)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.position = position;
        go.transform.localScale = size;
        go.GetComponent<Renderer>().sharedMaterial = material;
        return go;
    }

    static void CheckpointVolume(string name, Transform parent, Vector3 platformCenter, Material material)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.position = platformCenter + new Vector3(0, 1.5f, 0);
        go.transform.localScale = new Vector3(2, 1, 2);
        go.GetComponent<Renderer>().sharedMaterial = material;
        Object.DestroyImmediate(go.GetComponent<Collider>());
        var box = go.AddComponent<BoxCollider>();
        box.isTrigger = true;
        go.AddComponent<Checkpoint>();
    }

    static readonly Dictionary<string, Material> cache = new Dictionary<string, Material>();

    static Material Mat(string name, Color color, bool transparent = false)
    {
        if (cache.TryGetValue(name, out var cached) && cached) return cached;
        if (!AssetDatabase.IsValidFolder(MaterialDir)) AssetDatabase.CreateFolder("Assets", "Materials");
        string path = $"{MaterialDir}/{name}.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (!mat)
        {
            mat = new Material(Shader.Find("Standard"));
            AssetDatabase.CreateAsset(mat, path);
        }
        mat.color = color;
        if (transparent)
        {
            // Standard shader "Fade" mode
            mat.SetFloat("_Mode", 2);
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.renderQueue = 3000;
        }
        EditorUtility.SetDirty(mat);
        cache[name] = mat;
        return mat;
    }
}
