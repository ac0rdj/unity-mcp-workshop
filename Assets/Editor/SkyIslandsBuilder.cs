using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

// Builds the Art-track example: the base course re-skinned as floating sky islands, with a giant waterfall
// behind the goal, a sea of clouds, distant peaks and post-processing. All assets are CC0 from Poly Haven.
//   Parkour/Build Sky Islands (Art example) -> Assets/Scenes/Examples/Track_Art.unity
// Gameplay is untouched: the original box colliders stay, only their renderers are hidden.
public static class SkyIslandsBuilder
{
    const string Root = "Assets/Art/SkyIslands";
    const string Tex = Root + "/Textures/";
    const string MeshDir = Root + "/Meshes/";
    const string MatDir = Root + "/Materials/";
    const string OutScene = "Assets/Scenes/Examples/Track_Art.unity";

    static readonly Vector3 SunDir = new Vector3(-0.42f, 0, 0.91f).normalized; // horizontal direction toward the sun
    const float SunElevation = 40f;

    static Material islandMat, bigIslandMat, mountainMat, grassMat, fernMat, leavesMat, branchesMat, trunkMat, mossRockMat;
    static Material waterfallMat, cloudMat, cloudBaseMat, mistMat, ringMat, goalRingMat, beamMat, bannerMat, poleMat;
    static Mesh[] grassMeshes, fernMeshes, rockMeshes, treeMeshes;
    static Mesh[] debris;

    public static void BuildAndStills() { Build(); WorkshopCapture.CaptureStills(OutScene); }

    // Batch entry: build, then render the stills.
    public static void BuildAndRender()
    {
        Build();
        WorkshopCapture.CaptureStills(OutScene);
        WorkshopCapture.CaptureFlythrough(OutScene, 150);
    }

    [MenuItem("Parkour/Build Sky Islands (Art example)")]
    public static void Build()
    {
        AssetDatabase.DeleteAsset(MeshDir.TrimEnd('/'));
        Directory.CreateDirectory(MeshDir);
        AssetDatabase.Refresh();
        Directory.CreateDirectory(MatDir);
        Directory.CreateDirectory(Root + "/Generated");
        ConfigureImports();
        MakeTextures();
        MakeMaterials();
        LoadModels();

        EditorSceneManager.OpenScene("Assets/Scenes/Parkour.unity");
        var scene = EditorSceneManager.GetActiveScene();
        EditorSceneManager.SaveScene(scene, OutScene, true);
        scene = EditorSceneManager.OpenScene(OutScene);

        var rng = new System.Random(7);
        var scenery = new GameObject("Scenery").transform;
        var visuals = new GameObject("IslandVisuals").transform;

        // --- Course islands (visual skins over the unchanged colliders) ---
        var level = GameObject.Find("Level").transform;
        Transform B(string n) => level.Find(n);
        int seed = 10;
        foreach (var n in new[] { "Start", "Jump_01", "Jump_02", "Jump_03", "Jump_04", "Rest_01", "BlastGap_Landing", "Goal_Platform" })
        {
            var b = B(n);
            Vector3 s = b.localScale, p = b.position;
            float top = p.y + s.y / 2;
            float depth = 0.9f * Mathf.Max(s.x, s.z) + 3f + (float)rng.NextDouble() * 2f;
            var mesh = SaveMesh(IslandMesh(s.x, s.z, depth, seed++, 0.12f, 0.6f, 64), "Island_" + n);
            var go = Place(mesh, islandMat, new Vector3(p.x, top, p.z), visuals, "Island_" + n);
            b.GetComponent<Renderer>().enabled = false;
            DressTop(go.transform, s.x, s.z, top, rng, n == "Start" || n == "Goal_Platform" || n == "Rest_01");
            Debris(new Vector3(p.x, top - depth * 0.6f, p.z), Mathf.Max(s.x, s.z) * 0.7f, rng, visuals, 1 + rng.Next(2));
        }
        // Wall + WallTop become one rock mass whose front face matches the wall collider (z = 58).
        {
            var wall = B("Wall"); var wallTop = B("WallTop");
            float top = wallTop.position.y + wallTop.localScale.y / 2;           // 9.5
            float zMin = wall.position.z - wall.localScale.z / 2;                // 58
            float zMax = wallTop.position.z + wallTop.localScale.z / 2;          // 67
            float w = wallTop.localScale.x, d = zMax - zMin;
            float skirt = top - (B("BlastGap_Landing").position.y + 0.5f) + 0.6f; // vertical face down past the landing
            var mesh = SaveMesh(IslandMesh(w, d, 22f, 99, 0f, skirt, 64, inwardOnly: true), "Island_WallMass");
            var go = Place(mesh, islandMat, new Vector3(0, top, (zMin + zMax) / 2), visuals, "Island_WallMass");
            wall.GetComponent<Renderer>().enabled = false;
            wallTop.GetComponent<Renderer>().enabled = false;
            DressTop(go.transform, w, d, top, rng, false);
            Debris(new Vector3(0, top - 16, (zMin + zMax) / 2), 6, rng, visuals, 2);
        }

        // Checkpoints: hide the blue cylinders, add glowing rings.
        var ring = SaveMesh(Torus(1.5f, 0.07f, 64, 10), "CheckpointRing");
        foreach (var cp in Object.FindObjectsByType<Checkpoint>(FindObjectsSortMode.None))
        {
            cp.GetComponent<Renderer>().enabled = false;
            var platform = cp.transform.position - new Vector3(0, 1.5f, 0);
            Place(ring, ringMat, platform + new Vector3(0, 0.56f, 0), cp.transform.parent, cp.name + "_Ring");
        }

        // Goal: dark pole, waving banner and a golden ring.
        {
            var pole = B("Goal_Pole"); pole.GetComponent<Renderer>().sharedMaterial = poleMat;
            B("Goal_Flag").GetComponent<Renderer>().enabled = false;
            var banner = Place(SaveMesh(BannerMesh(2.4f, 1.4f, 24, 10), "Banner"), bannerMat,
                pole.position + new Vector3(0.15f, 2.2f, 0), level, "Goal_Banner");
            banner.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.TwoSided;
            var gp = B("Goal_Platform").position;
            Place(SaveMesh(Torus(3.2f, 0.1f, 96, 10), "GoalRing"), goalRingMat, gp + new Vector3(0, 0.56f, 0), level, "Goal_Ring");
        }

        // --- The big one: a vast floating mountain behind the goal with a waterfall pouring off it ---
        {
            var mesh = SaveMesh(IslandMesh(170, 80, 230, 3, 0.5f, 3f, 192, lumpScale: 2.2f, round: 0.75f), "Island_Giant");
            Vector3 c = new Vector3(-10, 62, 215);
            Place(mesh, bigIslandMat, c, scenery, "Giant_Island");
            DressBig(c, 170, 80, rng, scenery, 26, 70);
            float lipZ = c.z - 40.5f;
            Waterfall(new Vector3(-4, c.y + 0.15f, lipZ + 0.6f), 22, 44, c.y + 34, 15f, scenery, "Giant_Waterfall", 1f);
            Waterfall(new Vector3(-4, c.y + 0.3f, lipZ + 0.3f), 12, 26, c.y + 34, 17f, scenery, "Giant_Waterfall_Front", 1.35f);
            River(new Vector3(-4, c.y + 0.12f, lipZ + 0.6f), 22, 30, scenery);
            Waterfall(new Vector3(48, c.y - 4.5f, lipZ + 9f), 4, 9, c.y + 34, 5f, scenery, "Giant_Waterfall_Side", 1.2f);
            Mist(new Vector3(-4, -24, lipZ - 16), new Vector3(70, 4, 26), 28, scenery, "Waterfall_Mist", 50);
            Mist(new Vector3(-4, c.y - 10, lipZ - 9), new Vector3(22, 4, 5), 9, scenery, "Waterfall_Spray", 20);
        }

        // --- Mid-distance islands for depth (a few with trees and thin falls) ---
        var mids = new (Vector3 pos, float w, float d, bool trees, bool fall)[]
        {
            (new Vector3(-34, -4, 30), 16, 13, true, false),
            (new Vector3(36, 9, 52), 20, 15, true, true),
            (new Vector3(-52, 20, 84), 26, 20, true, true),
            (new Vector3(40, -12, 104), 14, 11, false, false),
            (new Vector3(-26, 26, 132), 18, 14, true, false),
            (new Vector3(74, 30, 150), 30, 22, true, true),
            (new Vector3(-90, 8, 150), 34, 26, true, true),
            (new Vector3(22, -8, -30), 18, 14, true, false),
            (new Vector3(-30, 12, -42), 22, 16, true, false),
            (new Vector3(10, 34, 70), 9, 8, false, false),
        };
        int mi = 0;
        foreach (var m in mids)
        {
            float depth = 0.9f * Mathf.Max(m.w, m.d) + 6f;
            var mesh = SaveMesh(IslandMesh(m.w, m.d, depth, 200 + mi, 0.3f, 1.2f, 96, lumpScale: 1.5f), "Island_Mid_" + mi);
            Place(mesh, islandMat, m.pos, scenery, "Mid_Island_" + mi);
            DressBig(m.pos, m.w, m.d, rng, scenery, m.trees ? 2 + rng.Next(3) : 0, 10);
            Debris(m.pos - new Vector3(0, depth * 0.6f, 0), m.w, rng, scenery, 1 + rng.Next(3));
            if (m.fall)
            {
                float edgeZ = m.pos.z - m.d / 2 - 0.2f;
                Waterfall(new Vector3(m.pos.x + m.w * 0.15f, m.pos.y + 0.1f, edgeZ + 1.2f), 2.2f, 5f, m.pos.y + 34, 3.5f, scenery, "Mid_Fall_" + mi, 1.2f);
            }
            mi++;
        }
        // Loose floating rocks around the course.
        for (int i = 0; i < 18; i++)
        {
            Vector3 p;
            do p = new Vector3(Rand(rng, -80, 80), Rand(rng, -20, 40), Rand(rng, -40, 140));
            while (Mathf.Abs(p.x) < 22 && p.z > -25 && p.z < 110);
            var go = Place(debris[rng.Next(debris.Length)], islandMat, p, scenery, "Float_Rock_" + i);
            go.transform.localScale = Vector3.one * Rand(rng, 0.6f, 1.8f);
            go.transform.rotation = Quaternion.Euler(Rand(rng, -15, 15), Rand(rng, 0, 360), Rand(rng, -15, 15));
        }

        // --- Distant peaks and the cloud sea ---
        var far = new (Vector3 pos, float w, float d, bool fall)[]
        {
            (new Vector3(-280, 110, 430), 220, 150, true),
            (new Vector3(310, 70, 390), 190, 120, false),
            (new Vector3(70, 190, 640), 280, 170, true),
            (new Vector3(-440, 60, 170), 170, 120, false),
            (new Vector3(400, 120, 140), 150, 100, true),
            (new Vector3(30, 100, -420), 220, 150, false),
            (new Vector3(-330, 150, -200), 160, 110, false),
        };
        int fi = 0;
        foreach (var f in far)
        {
            var mesh = SaveMesh(IslandMesh(f.w, f.d, f.w * 1.3f, 700 + fi, 0.5f, 4f, 128, lumpScale: 2f, round: 0.8f), "Island_Far_" + fi);
            Place(mesh, mountainMat, f.pos, scenery, "Far_Island_" + fi);
            DressBig(f.pos, f.w, f.d, rng, scenery, 0, 0, 2.5f);
            if (f.fall)
            {
                var toCourse = new Vector3(0, 0, 60) - f.pos; toCourse.y = 0; toCourse.Normalize();
                var lip = f.pos + toCourse * (Mathf.Min(f.w, f.d) / 2 - 1f);
                var wf = Waterfall(new Vector3(lip.x, f.pos.y + 0.2f, lip.z), 14, 30, f.pos.y + 36, 14f, scenery, "Far_Fall_" + fi, 0.8f);
                wf.transform.rotation = Quaternion.LookRotation(-toCourse);
            }
            fi++;
        }
        var cloudMesh = SaveMesh(Grid(16000, 16), "CloudPlane");
        Place(cloudMesh, cloudMat, new Vector3(0, -26, 60), scenery, "CloudSea").GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        Place(cloudMesh, cloudBaseMat, new Vector3(0, -40, 60), scenery, "CloudSea_Base").GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        Mist(new Vector3(0, 6, 50), new Vector3(90, 30, 140), 0.2f, scenery, "Floating_Motes", 0, motes: true);

        Lighting(scene);
        PostFX();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        string lsPath = Root + "/SkyIslandsLighting.lighting";
        var ls = AssetDatabase.LoadAssetAtPath<LightingSettings>(lsPath);
        if (!ls) { ls = new LightingSettings { bakedGI = false, realtimeGI = false, name = "SkyIslandsLighting" }; AssetDatabase.CreateAsset(ls, lsPath); }
        Lightmapping.lightingSettings = ls;
        Lightmapping.Bake(); // environment lighting + reflections from the sky
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("[SkyIslands] built " + OutScene);
    }

    // ---------------------------------------------------------------- scene dressing

    static void DressTop(Transform island, float w, float d, float top, System.Random rng, bool hero)
    {
        Vector3 c = island.position;
        var grass = new List<CombineInstance>();
        int count = Mathf.RoundToInt(w * d * 2.2f);
        for (int i = 0; i < count; i++)
        {
            var p = new Vector3(c.x + Rand(rng, -w / 2, w / 2) * 0.97f, top, c.z + Rand(rng, -d / 2, d / 2) * 0.97f);
            grass.Add(CI(grassMeshes[rng.Next(grassMeshes.Length)], p, Rand(rng, 0, 360), Rand(rng, 1.8f, 2.8f)));
        }
        // ferns hug the rim
        int ferns = Mathf.RoundToInt((w + d) * 0.9f);
        var fern = new List<CombineInstance>();
        for (int i = 0; i < ferns; i++)
        {
            float th = Rand(rng, 0, Mathf.PI * 2);
            var o = Superellipse(th, w / 2 * Rand(rng, 0.8f, 0.98f), d / 2 * Rand(rng, 0.8f, 0.98f));
            fern.Add(CI(fernMeshes[rng.Next(fernMeshes.Length)], new Vector3(c.x + o.x, top, c.z + o.y), Rand(rng, 0, 360), Rand(rng, 1.1f, 1.7f)));
        }
        Combined(grass, grassMat, island, "Grass");
        Combined(fern, fernMat, island, "Ferns");
        if (!hero) return;
        // a tree and some mossy rocks on the back corners of the big platforms
        var corner = new Vector3(c.x - w * 0.36f, top, c.z + d * 0.34f);
        Tree(corner, Rand(rng, 1.5f, 1.8f), Rand(rng, 0, 360), island);
        for (int i = 0; i < 3; i++)
        {
            var rp = new Vector3(c.x + w * Rand(rng, 0.25f, 0.42f), top - 0.1f, c.z + d * Rand(rng, 0.2f, 0.42f));
            Place(rockMeshes[rng.Next(rockMeshes.Length)], mossRockMat, rp, island, "MossRock").transform.rotation = Quaternion.Euler(0, Rand(rng, 0, 360), 0);
        }
    }

    static void DressBig(Vector3 c, float w, float d, System.Random rng, Transform parent, int trees, int rocks, float grassScale = 1f)
    {
        var grass = new List<CombineInstance>();
        int count = grassScale > 2f ? 0 : Mathf.Min(700, Mathf.RoundToInt(w * d * 0.3f));
        for (int i = 0; i < count; i++)
        {
            var o = Superellipse(Rand(rng, 0, Mathf.PI * 2), w / 2 * Mathf.Sqrt((float)rng.NextDouble()), d / 2 * Mathf.Sqrt((float)rng.NextDouble()));
            grass.Add(CI(grassMeshes[rng.Next(grassMeshes.Length)], c + new Vector3(o.x, 0, o.y), Rand(rng, 0, 360), Rand(rng, 3f, 5f) * grassScale));
        }
        var holder = new GameObject("Dressing").transform; holder.SetParent(parent); holder.position = c;
        Combined(grass, grassMat, holder, "Grass");
        for (int i = 0; i < trees; i++)
        {
            var o = Superellipse(Rand(rng, 0, Mathf.PI * 2), w / 2 * Rand(rng, 0.1f, 0.85f), d / 2 * Rand(rng, 0.1f, 0.85f));
            Tree(c + new Vector3(o.x, 0, o.y), Rand(rng, 1.8f, 3.2f) * Mathf.Clamp(w / 20f, 1, 2.5f), Rand(rng, 0, 360), holder);
        }
        for (int i = 0; i < rocks; i++)
        {
            var o = Superellipse(Rand(rng, 0, Mathf.PI * 2), w / 2 * Rand(rng, 0.5f, 0.95f), d / 2 * Rand(rng, 0.5f, 0.95f));
            var r = Place(rockMeshes[rng.Next(rockMeshes.Length)], mossRockMat, c + new Vector3(o.x, -0.2f, o.y), holder, "MossRock");
            r.transform.rotation = Quaternion.Euler(0, Rand(rng, 0, 360), 0);
            r.transform.localScale = Vector3.one * Rand(rng, 1f, 2.5f) * Mathf.Clamp(w / 30f, 1, 3);
        }
    }

    static void Debris(Vector3 center, float size, System.Random rng, Transform parent, int n)
    {
        for (int i = 0; i < n; i++)
        {
            var p = center + new Vector3(Rand(rng, -size, size) * 0.7f, Rand(rng, -size, size) * 0.4f, Rand(rng, -size, size) * 0.7f);
            var go = Place(debris[rng.Next(debris.Length)], islandMat, p, parent, "Debris");
            go.transform.localScale = Vector3.one * Rand(rng, 0.25f, 0.6f) * Mathf.Clamp(size / 6f, 0.6f, 4f);
            go.transform.rotation = Quaternion.Euler(Rand(rng, -20, 20), Rand(rng, 0, 360), Rand(rng, -20, 20));
        }
    }

    static void Tree(Vector3 pos, float scale, float yaw, Transform parent)
    {
        var t = new GameObject("Tree").transform;
        t.SetParent(parent); t.position = pos; t.rotation = Quaternion.Euler(0, yaw, 0); t.localScale = Vector3.one * scale;
        var mats = new[] { trunkMat, branchesMat, leavesMat };
        for (int i = 0; i < 3; i++)
        {
            var g = new GameObject(treeMeshes[i].name);
            g.transform.SetParent(t, false);
            g.AddComponent<MeshFilter>().sharedMesh = treeMeshes[i];
            g.AddComponent<MeshRenderer>().sharedMaterial = mats[i];
        }
    }

    static GameObject Waterfall(Vector3 lip, float w0, float w1, float drop, float throwDist, Transform parent, string name, float speed)
    {
        // Projectile-ish arc: forward offset grows with sqrt(fall), toward -z (the player side).
        int seg = 70; float H = drop;
        var v = new List<Vector3>(); var uv = new List<Vector2>(); var col = new List<Color>(); var tri = new List<int>();
        int cols = 5;
        for (int i = 0; i <= seg; i++)
        {
            float s = Mathf.Pow(i / (float)seg, 1.6f);
            float h = s * H;
            float fwd = throwDist * Mathf.Sqrt(h / H) + (h < 1 ? 0 : 0);
            float width = Mathf.Lerp(w0, w1, s);
            float a = Mathf.Clamp01(i / 3f) * Mathf.Clamp01((H - h) / 14f);
            for (int j = 0; j < cols; j++)
            {
                float u = j / (float)(cols - 1);
                float bulge = Mathf.Sin(u * Mathf.PI) * Mathf.Min(1.2f, width * 0.08f);
                v.Add(new Vector3((u - 0.5f) * width, -h, -fwd - bulge));
                uv.Add(new Vector2(u, h));
                col.Add(new Color(1, 1, 1, a));
            }
        }
        for (int i = 0; i < seg; i++)
            for (int j = 0; j < cols - 1; j++)
            {
                int a = i * cols + j, b = a + 1, c = a + cols, d = c + 1;
                tri.AddRange(new[] { a, c, b, b, c, d });
            }
        var m = new Mesh { name = name };
        m.SetVertices(v); m.SetUVs(0, uv); m.SetColors(col); m.SetTriangles(tri, 0);
        m.RecalculateNormals(); m.RecalculateBounds();
        var go = Place(SaveMesh(m, name), waterfallMat, lip, parent, name);
        var r = go.GetComponent<Renderer>();
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        if (speed != 1f)
        {
            var mat = new Material(waterfallMat) { name = "Waterfall_x" + speed };
            mat.SetFloat("_Speed", speed);
            AssetDatabase.CreateAsset(mat, MatDir + mat.name + ".mat");
            r.sharedMaterial = mat;
        }
        return go;
    }

    static void River(Vector3 lip, float width, float length, Transform parent)
    {
        // Flat water on top of the giant island, flowing to the lip (+z is upstream).
        var m = new Mesh { name = "River" };
        int seg = 20, cols = 4;
        var v = new List<Vector3>(); var uv = new List<Vector2>(); var col = new List<Color>(); var tri = new List<int>();
        for (int i = 0; i <= seg; i++)
            for (int j = 0; j < cols; j++)
            {
                float s = i / (float)seg, u = j / (float)(cols - 1);
                float wobble = Mathf.Sin(s * 5f) * 1.5f;
                v.Add(new Vector3((u - 0.5f) * width * (1 - s * 0.35f) + wobble, 0, length * s));
                uv.Add(new Vector2(u, -length * s));
                col.Add(new Color(1, 1, 1, Mathf.Clamp01((1 - s) * 4f)));
            }
        for (int i = 0; i < seg; i++)
            for (int j = 0; j < cols - 1; j++)
            {
                int a = i * cols + j, b = a + 1, c = a + cols, d = c + 1;
                tri.AddRange(new[] { a, c, b, b, c, d });
            }
        m.SetVertices(v); m.SetUVs(0, uv); m.SetColors(col); m.SetTriangles(tri, 0); m.RecalculateNormals(); m.RecalculateBounds();
        var mat = new Material(waterfallMat) { name = "River_Water" };
        mat.SetFloat("_Speed", 0.35f);
        AssetDatabase.CreateAsset(mat, MatDir + mat.name + ".mat");
        Place(SaveMesh(m, "River"), mat, lip, parent, "River").GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
    }

    static void Mist(Vector3 pos, Vector3 box, float size, Transform parent, string name, float rate, bool motes = false)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent); go.transform.position = pos;
        var ps = go.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.loop = true; main.prewarm = true;
        main.startLifetime = motes ? 12f : 9f;
        main.startSpeed = motes ? new ParticleSystem.MinMaxCurve(0.05f, 0.3f) : new ParticleSystem.MinMaxCurve(0.5f, 2.5f);
        main.startSize = motes ? new ParticleSystem.MinMaxCurve(0.06f, 0.16f) : new ParticleSystem.MinMaxCurve(size * 0.6f, size * 1.2f);
        main.startRotation = new ParticleSystem.MinMaxCurve(0, Mathf.PI * 2);
        main.startColor = motes ? new Color(1f, 0.95f, 0.8f, 0.8f) : new Color(1, 1, 1, 0.32f);
        main.maxParticles = motes ? 400 : 300;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        var em = ps.emission; em.rateOverTime = motes ? 30 : rate;
        var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Box; sh.scale = box;
        var col = ps.colorOverLifetime; col.enabled = true;
        var g = new Gradient();
        g.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) },
                  new[] { new GradientAlphaKey(0, 0), new GradientAlphaKey(1, 0.25f), new GradientAlphaKey(1, 0.6f), new GradientAlphaKey(0, 1) });
        col.color = g;
        var sz = ps.sizeOverLifetime; sz.enabled = !motes; sz.size = new ParticleSystem.MinMaxCurve(1, AnimationCurve.Linear(0, 0.7f, 1, 1.5f));
        var vel = ps.velocityOverLifetime; vel.enabled = true; vel.space = ParticleSystemSimulationSpace.World;
        vel.x = new ParticleSystem.MinMaxCurve(-0.4f, 0.4f); vel.y = new ParticleSystem.MinMaxCurve(motes ? -0.1f : 0.3f, motes ? 0.2f : 1.5f); vel.z = new ParticleSystem.MinMaxCurve(-0.4f, 0.4f);
        var r = go.GetComponent<ParticleSystemRenderer>();
        r.sharedMaterial = mistMat;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        r.maxParticleSize = 5f;
    }

    // ---------------------------------------------------------------- lighting & post

    static void Lighting(UnityEngine.SceneManagement.Scene scene)
    {
        var sky = new Material(Shader.Find("Skybox/Panoramic")) { name = "Skybox_Kloofendal" };
        sky.SetTexture("_MainTex", AssetDatabase.LoadAssetAtPath<Texture>(Tex + "kloofendal_48d_partly_cloudy_puresky_2k.hdr"));
        sky.SetFloat("_Mapping", 1); sky.SetFloat("_ImageType", 0);
        sky.SetFloat("_Exposure", 1.25f);
        sky.SetColor("_Tint", new Color(0.56f, 0.64f, 0.64f));
        // HDRI sun sits at u = 0.6 (atan2(z,x) = -36deg); rotate it to our sun direction.
        float phiSun = Mathf.Atan2(SunDir.z, SunDir.x) * Mathf.Rad2Deg;
        sky.SetFloat("_Rotation", Mathf.Repeat(-36f - phiSun, 360f));
        AssetDatabase.CreateAsset(sky, MatDir + "Skybox_Kloofendal.mat");
        RenderSettings.skybox = sky;
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Skybox;
        RenderSettings.ambientIntensity = 1.25f;
        RenderSettings.defaultReflectionMode = UnityEngine.Rendering.DefaultReflectionMode.Skybox;
        RenderSettings.reflectionIntensity = 0.8f;
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = new Color(0.68f, 0.84f, 0.9f);
        RenderSettings.fogStartDistance = 15;
        RenderSettings.fogEndDistance = 720;

        var sun = Object.FindObjectsByType<Light>(FindObjectsSortMode.None).FirstOrDefault(l => l.type == LightType.Directional);
        if (!sun) sun = new GameObject("Directional Light").AddComponent<Light>();
        sun.type = LightType.Directional;
        sun.color = new Color(1f, 0.95f, 0.86f);
        sun.intensity = 1.25f;
        sun.shadows = LightShadows.Soft;
        sun.shadowStrength = 0.7f;
        sun.shadowNormalBias = 0.6f;
        var toSun = Quaternion.AngleAxis(-SunElevation, Vector3.Cross(Vector3.up, SunDir)) * SunDir; // tilt up toward the sky
        sun.transform.rotation = Quaternion.LookRotation(-toSun);
        RenderSettings.sun = sun;

        QualitySettings.shadowDistance = 220;
        QualitySettings.shadowCascades = 4;
        QualitySettings.shadows = ShadowQuality.All;
        QualitySettings.shadowResolution = ShadowResolution.VeryHigh;

        var cam = MainCam();
        cam.farClipPlane = 9000;
        cam.allowHDR = true;
        cloudMat.SetVector("_SunDir", new Vector4(SunDir.x, 0, SunDir.z, 0));
        cloudBaseMat.SetVector("_SunDir", new Vector4(SunDir.x, 0, SunDir.z, 0));
    }

    static Camera MainCam() => GameObject.FindWithTag("MainCamera").GetComponent<Camera>();

    public static PostProcessResources Resources =>
        AssetDatabase.LoadAssetAtPath<PostProcessResources>("Packages/com.unity.postprocessing/PostProcessing/PostProcessResources.asset");

    static void PostFX()
    {
        string path = Root + "/SkyIslands_PostFX.asset";
        AssetDatabase.DeleteAsset(path);
        var profile = ScriptableObject.CreateInstance<PostProcessProfile>();
        AssetDatabase.CreateAsset(profile, path);
        var bloom = profile.AddSettings<Bloom>();
        bloom.intensity.Override(2.2f); bloom.threshold.Override(0.95f); bloom.softKnee.Override(0.6f); bloom.diffusion.Override(8f);
        var cg = profile.AddSettings<ColorGrading>();
        cg.gradingMode.Override(GradingMode.HighDefinitionRange);
        cg.tonemapper.Override(Tonemapper.ACES);
        cg.postExposure.Override(0.12f); cg.saturation.Override(16f); cg.contrast.Override(6f); cg.temperature.Override(-6f); cg.tint.Override(-4f);
        cg.lift.Override(new Vector4(-0.02f, 0.01f, 0.05f, -0.03f)); cg.gain.Override(new Vector4(0.04f, 0.02f, -0.02f, 0f));
        var vig = profile.AddSettings<Vignette>(); vig.intensity.Override(0.22f); vig.smoothness.Override(0.45f);
        var ao = profile.AddSettings<AmbientOcclusion>(); ao.mode.Override(AmbientOcclusionMode.MultiScaleVolumetricObscurance); ao.intensity.Override(0.55f);
        foreach (var s in profile.settings) { s.hideFlags = HideFlags.HideInInspector | HideFlags.HideInHierarchy; AssetDatabase.AddObjectToAsset(s, profile); }
        EditorUtility.SetDirty(profile);

        var vol = new GameObject("PostFX").AddComponent<PostProcessVolume>();
        vol.isGlobal = true; vol.sharedProfile = profile; vol.priority = 1;

        var cam = MainCam().gameObject;
        var layer = cam.GetComponent<PostProcessLayer>();
        if (!layer) layer = cam.AddComponent<PostProcessLayer>();
        layer.Init(Resources);
        layer.volumeLayer = 1; // Default
        layer.antialiasingMode = PostProcessLayer.Antialiasing.SubpixelMorphologicalAntialiasing;
    }

    // ---------------------------------------------------------------- assets

    static void ConfigureImports()
    {
        foreach (var path in Directory.GetFiles(Tex).Where(p => !p.EndsWith(".meta")).Select(p => p.Replace('\\', '/')))
        {
            var ti = AssetImporter.GetAtPath(path) as TextureImporter;
            if (!ti) continue;
            bool normal = path.Contains("_nor_gl");
            bool changed = false;
            if (normal && ti.textureType != TextureImporterType.NormalMap) { ti.textureType = TextureImporterType.NormalMap; changed = true; }
            if (path.EndsWith("_rgba.png") && !ti.alphaIsTransparency) { ti.alphaIsTransparency = true; changed = true; }
            if (ti.maxTextureSize != 2048) { ti.maxTextureSize = 2048; changed = true; }
            if (path.EndsWith(".hdr") && ti.wrapModeV != TextureWrapMode.Clamp) { ti.wrapModeU = TextureWrapMode.Repeat; ti.wrapModeV = TextureWrapMode.Clamp; ti.mipmapEnabled = false; ti.textureCompression = TextureImporterCompression.CompressedHQ; changed = true; }
            if (changed) ti.SaveAndReimport();
        }
        foreach (var path in Directory.GetFiles(Root + "/Models", "*.fbx").Select(p => p.Replace('\\', '/')))
        {
            var mi = (ModelImporter)AssetImporter.GetAtPath(path);
            if (mi.materialImportMode == ModelImporterMaterialImportMode.None) continue;
            mi.materialImportMode = ModelImporterMaterialImportMode.None;
            mi.importAnimation = false;
            mi.SaveAndReimport();
        }
    }

    static void MakeTextures()
    {
        // Tileable fbm noise for water and clouds, and a soft dot for mist particles.
        string noisePath = Root + "/Generated/Noise.png", softPath = Root + "/Generated/Soft.png", beamPath = Root + "/Generated/Beam.png";
        if (!File.Exists(noisePath))
        {
            const int N = 256;
            var t = new Texture2D(N, N, TextureFormat.RGB24, false);
            for (int y = 0; y < N; y++)
                for (int x = 0; x < N; x++)
                {
                    float v = 0, amp = 0.5f, tot = 0; int period = 8;
                    for (int o = 0; o < 5; o++) { v += amp * PeriodicNoise(x * period / (float)N, y * period / (float)N, period, o); tot += amp; amp *= 0.5f; period *= 2; }
                    v /= tot;
                    t.SetPixel(x, y, new Color(v, v, v));
                }
            File.WriteAllBytes(noisePath, t.EncodeToPNG());
            var s = new Texture2D(128, 128, TextureFormat.RGBA32, false);
            for (int y = 0; y < 128; y++)
                for (int x = 0; x < 128; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), new Vector2(63.5f, 63.5f)) / 63.5f;
                    float a = Mathf.Clamp01(1 - d); a = a * a * (3 - 2 * a);
                    s.SetPixel(x, y, new Color(1, 1, 1, a));
                }
            File.WriteAllBytes(softPath, s.EncodeToPNG());
            var b = new Texture2D(64, 128, TextureFormat.RGBA32, false);
            for (int y = 0; y < 128; y++)
                for (int x = 0; x < 64; x++)
                {
                    float vy = y / 127f, a = Mathf.Pow(1 - vy, 1.6f);
                    b.SetPixel(x, y, new Color(a, a, a, a));
                }
            File.WriteAllBytes(beamPath, b.EncodeToPNG());
            AssetDatabase.Refresh();
            var ni = (TextureImporter)AssetImporter.GetAtPath(noisePath); ni.sRGBTexture = false; ni.wrapMode = TextureWrapMode.Repeat; ni.SaveAndReimport();
            var si = (TextureImporter)AssetImporter.GetAtPath(softPath); si.alphaIsTransparency = true; si.wrapMode = TextureWrapMode.Clamp; si.SaveAndReimport();
            var bi = (TextureImporter)AssetImporter.GetAtPath(beamPath); bi.wrapMode = TextureWrapMode.Clamp; bi.SaveAndReimport();
        }
    }

    static Texture T(string file) => AssetDatabase.LoadAssetAtPath<Texture>(Tex + file);

    static void MakeMaterials()
    {
        var tri = Shader.Find("SkyIslands/IslandTriplanar");
        islandMat = Mat("Island_Rock_Grass", tri, m =>
        {
            m.SetTexture("_GrassTex", T("leafy_grass_Diffuse.jpg")); m.SetTexture("_GrassNormal", T("leafy_grass_nor_gl.jpg"));
            m.SetTexture("_MossTex", T("aerial_grass_rock_Diffuse.jpg"));
            m.SetTexture("_RockTex", T("mossy_rock_Diffuse.jpg")); m.SetTexture("_RockNormal", T("mossy_rock_nor_gl.jpg"));
            m.SetColor("_GrassTint", new Color(0.4f, 0.58f, 0.3f)); m.SetColor("_RockTint", new Color(0.58f, 0.68f, 0.68f));
            m.SetColor("_MossTint", new Color(0.5f, 0.68f, 0.38f));
            m.SetFloat("_GrassScale", 0.28f); m.SetFloat("_RockScale", 0.085f); m.SetFloat("_GrassLevel", 0.66f); m.SetFloat("_NormalStrength", 1.4f);
        });
        bigIslandMat = Mat("Island_Giant", tri, m =>
        {
            m.CopyPropertiesFromMaterial(islandMat);
            m.SetFloat("_GrassScale", 0.12f); m.SetFloat("_RockScale", 0.035f); m.SetFloat("_GrassLevel", 0.6f);
        });
        mountainMat = Mat("Mountains", tri, m =>
        {
            m.CopyPropertiesFromMaterial(islandMat);
            m.SetFloat("_GrassScale", 0.02f); m.SetFloat("_RockScale", 0.006f); m.SetFloat("_GrassLevel", 0.55f);
            m.SetColor("_RockTint", new Color(0.6f, 0.7f, 0.72f));
        });
        var foliage = Shader.Find("Art/FoliageCutout");
        grassMat = Mat("Grass_Clumps", foliage, m => { m.enableInstancing = true; m.SetTexture("_MainTex", T("grass_medium_01_rgba.png")); m.SetColor("_Color", new Color(0.62f, 0.8f, 0.52f)); m.SetFloat("_Cutoff", 0.45f); });
        fernMat = Mat("Ferns", foliage, m => { m.enableInstancing = true; m.SetTexture("_MainTex", T("fern_02_rgba.png")); m.SetColor("_Color", new Color(0.7f, 0.85f, 0.6f)); m.SetFloat("_Cutoff", 0.45f); });
        leavesMat = Mat("Tree_Leaves", foliage, m => { m.SetTexture("_MainTex", T("island_tree_02_leaves_rgba.png")); m.SetTexture("_BumpMap", T("island_tree_02_leaves_nor_gl_1k.png")); m.SetFloat("_Cutoff", 0.4f); m.SetColor("_Color", new Color(0.9f, 1f, 0.8f)); });
        var std = Shader.Find("Standard");
        branchesMat = Mat("Tree_Branches", std, m => { m.SetTexture("_MainTex", T("island_tree_02_branches_diff_1k.png")); m.SetTexture("_BumpMap", T("island_tree_02_branches_nor_gl_1k.png")); m.EnableKeyword("_NORMALMAP"); m.SetFloat("_Glossiness", 0.1f); });
        trunkMat = Mat("Tree_Trunk", std, m => { m.SetTexture("_MainTex", T("island_tree_02_diff_1k.jpg")); m.SetTexture("_BumpMap", T("island_tree_02_nor_gl_1k.exr")); m.EnableKeyword("_NORMALMAP"); m.SetFloat("_Glossiness", 0.1f); });
        mossRockMat = Mat("Moss_Rocks", std, m => { m.SetTexture("_MainTex", T("rock_moss_set_01_diff_1k.jpg")); m.SetColor("_Color", new Color(0.48f, 0.55f, 0.52f)); m.SetTexture("_BumpMap", T("rock_moss_set_01_nor_gl_1k.exr")); m.EnableKeyword("_NORMALMAP"); m.SetFloat("_Glossiness", 0.1f); });
        poleMat = Mat("Goal_Pole_Metal", std, m => { m.SetColor("_Color", new Color(0.12f, 0.12f, 0.13f)); m.SetFloat("_Metallic", 0.85f); m.SetFloat("_Glossiness", 0.55f); });

        var noise = AssetDatabase.LoadAssetAtPath<Texture>(Root + "/Generated/Noise.png");
        waterfallMat = Mat("Waterfall", Shader.Find("SkyIslands/Waterfall"), m => { m.SetTexture("_NoiseTex", noise); m.SetColor("_Color", new Color(0.55f, 0.85f, 0.9f)); m.SetColor("_FoamColor", new Color(1.7f, 1.8f, 1.85f)); });
        cloudMat = Mat("CloudSea", Shader.Find("SkyIslands/CloudSea"), m => { m.SetTexture("_NoiseTex", noise); });
        cloudBaseMat = Mat("CloudSea_Base", Shader.Find("SkyIslands/CloudSea"), m =>
        {
            m.SetTexture("_NoiseTex", noise); m.SetFloat("_Cover", 0.05f); m.SetFloat("_Scale", 0.0011f);
            m.SetColor("_Lit", new Color(1.05f, 1.1f, 1.12f)); m.SetColor("_Shade", new Color(0.55f, 0.68f, 0.76f));
            m.renderQueue = 2980;
        });
        var particle = Shader.Find("Legacy Shaders/Particles/Alpha Blended") ?? Shader.Find("Mobile/Particles/Alpha Blended");
        mistMat = Mat("Mist", particle, m => { m.SetTexture("_MainTex", AssetDatabase.LoadAssetAtPath<Texture>(Root + "/Generated/Soft.png")); m.SetColor("_TintColor", new Color(0.62f, 0.66f, 0.68f, 0.5f)); });
        ringMat = Mat("Checkpoint_Glow", std, m => Emissive(m, new Color(0.3f, 0.75f, 2.6f) * 1.6f));
        goalRingMat = Mat("Goal_Glow", std, m => Emissive(m, new Color(2.6f, 1.7f, 0.4f) * 1.5f));
        bannerMat = Mat("Goal_Banner", Shader.Find("SkyIslands/Banner"), m => m.SetColor("_Color", new Color(0.8f, 0.2f, 0.08f)));
        var additive = Shader.Find("Legacy Shaders/Particles/Additive") ?? Shader.Find("Mobile/Particles/Additive");
        beamMat = Mat("Goal_Beam", additive, m => { m.SetTexture("_MainTex", AssetDatabase.LoadAssetAtPath<Texture>(Root + "/Generated/Beam.png")); m.SetColor("_TintColor", new Color(0.16f, 0.12f, 0.04f, 0.5f)); });
    }

    static void Emissive(Material m, Color hdr)
    {
        m.SetColor("_Color", Color.black);
        m.EnableKeyword("_EMISSION");
        m.SetColor("_EmissionColor", hdr);
        m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
    }

    static Material Mat(string name, Shader shader, System.Action<Material> setup)
    {
        string path = MatDir + name + ".mat";
        AssetDatabase.DeleteAsset(path);
        var m = new Material(shader) { name = name };
        setup(m);
        AssetDatabase.CreateAsset(m, path);
        return m;
    }

    static void LoadModels()
    {
        Mesh[] Meshes(string file) => AssetDatabase.LoadAllAssetsAtPath(Root + "/Models/" + file).OfType<Mesh>().ToArray();
        grassMeshes = Meshes("grass_medium_01.fbx").Where(m => m.name.Contains("mid") || m.name.Contains("tall")).ToArray();
        fernMeshes = Meshes("fern_02.fbx");
        rockMeshes = Meshes("rock_moss_set_01.fbx");
        var tree = Meshes("island_tree_02.fbx");
        treeMeshes = new[] { "Trunk", "Branches", "Leaves" }.Select(n => tree.First(m => m.name.StartsWith(n))).ToArray();
        foreach (var m in tree) Debug.Log($"[SkyIslands] tree mesh {m.name} bounds {m.bounds.size}");
        Debug.Log($"[SkyIslands] grass {grassMeshes.Length} {grassMeshes[0].bounds.size}, fern {fernMeshes.Length}, rocks {rockMeshes.Length} {rockMeshes[0].bounds.size}");
        debris = new Mesh[5];
        for (int i = 0; i < debris.Length; i++)
            debris[i] = SaveMesh(IslandMesh(3f + i * 0.4f, 2.6f + i * 0.3f, 4.5f + i, 500 + i, 0.2f, 0.4f, 32), "Debris_" + i);
    }

    // ---------------------------------------------------------------- mesh generation

    // Floating island: flat grassy top matching a w x d footprint, a short vertical skirt, then a lumpy
    // rock body that tapers to a hanging tip `depth` metres below. Top centre is the pivot.
    static Mesh IslandMesh(float w, float d, float depth, int seed, float overhang, float skirt, int N,
                           bool inwardOnly = false, float lumpScale = 1f, float round = 0.5f)
    {
        var rnd = new System.Random(seed);
        float ax = w / 2 + overhang, az = d / 2 + overhang;
        float size = Mathf.Max(ax, az);
        Vector2 tip = new Vector2(Rand(rnd, -0.2f, 0.2f) * ax, Rand(rnd, -0.2f, 0.2f) * az);
        float so = seed * 13.37f;
        int L = Mathf.Clamp(Mathf.RoundToInt(depth / 0.6f), 12, 48);

        var v = new List<Vector3>(); var c = new List<Color>(); var t = new List<int>();
        var rim = new Vector2[N];
        for (int i = 0; i < N; i++)
        {
            float th = i / (float)N * Mathf.PI * 2;
            var o = Superellipse(th, ax, az, round);
            var dir = o.normalized;
            float bump = inwardOnly ? 0 : Mathf.Max(0, Noise3(dir.x * 2.2f + so, 0, dir.y * 2.2f)) * 0.35f;
            rim[i] = o + dir * bump;
        }

        // Cap: centre fan + 3 inner rings, normals straight up.
        int capStart = v.Count;
        v.Add(Vector3.zero); c.Add(Color.white);
        float[] fr = { 0.3f, 0.6f, 0.85f, 1f };
        foreach (var f in fr)
            for (int i = 0; i < N; i++) { v.Add(new Vector3(rim[i].x * f, 0, rim[i].y * f)); c.Add(Color.white); }
        for (int i = 0; i < N; i++) t.AddRange(new[] { capStart, capStart + 1 + (i + 1) % N, capStart + 1 + i });
        for (int r = 0; r < fr.Length - 1; r++)
            for (int i = 0; i < N; i++)
            {
                int a = capStart + 1 + r * N + i, b = capStart + 1 + r * N + (i + 1) % N;
                int a2 = a + N, b2 = b + N;
                t.AddRange(new[] { a, b, a2, b, b2, a2 });
            }

        // Sides
        int sideStart = v.Count;
        float body = depth - skirt;
        for (int j = 0; j <= L; j++)
        {
            float y, f, s;
            int skirtRings = Mathf.Max(1, Mathf.RoundToInt(skirt / 0.5f));
            if (j <= skirtRings) { y = -skirt * j / skirtRings; s = 0; }
            else { s = (j - skirtRings) / (float)(L - skirtRings); y = -skirt - body * s; }
            // belly just under the rim, then taper; strata terraces add ledges
            f = Mathf.Pow(1 - s, 0.75f) * (1 + 0.12f * Mathf.Sin(Mathf.PI * Mathf.Min(s * 3f, 1f)));
            for (int i = 0; i < N; i++)
            {
                var o = rim[i];
                var dir = o.normalized;
                float ny = y / size;
                float lump = Noise3(dir.x * 1.6f * lumpScale + so, ny * 2.5f, dir.y * 1.6f * lumpScale) * 0.28f
                           + Noise3(dir.x * 4f * lumpScale + so, ny * 7f, dir.y * 4f * lumpScale) * 0.1f;
                float strata = 0.05f * Mathf.Sin(y * 2.4f + Noise3(dir.x * 3 + so, ny * 3, dir.y * 3) * 4f);
                Vector2 p;
                if (s <= 0)
                {
                    float inset = inwardOnly ? (0.04f + Mathf.Abs(lump) * 0.25f) : -lump * 0.25f;
                    p = o - dir * Mathf.Clamp(inset, -0.25f, 0.3f) * (j == 0 ? 0 : 1);
                }
                else
                {
                    float k = f * (1 + (lump + strata) * Mathf.Min(1, s * 4f + 0.25f));
                    p = o * k + tip * s * s;
                }
                v.Add(new Vector3(p.x, y, p.y));
                float ao = Mathf.Lerp(1f, 0.35f, Mathf.Pow(Mathf.Clamp01(s), 0.7f));
                c.Add(new Color(ao, j == 0 ? 1 : 0.85f, 1, 1));
            }
        }
        for (int j = 0; j < L; j++)
            for (int i = 0; i < N; i++)
            {
                int a = sideStart + j * N + i, b = sideStart + j * N + (i + 1) % N;
                int a2 = a + N, b2 = b + N;
                t.AddRange(new[] { a, a2, b, b, a2, b2 });
            }
        int tipIdx = v.Count;
        v.Add(new Vector3(tip.x, -depth - size * 0.05f, tip.y)); c.Add(new Color(0.3f, 0, 1, 1));
        for (int i = 0; i < N; i++)
            t.AddRange(new[] { sideStart + L * N + i, tipIdx, sideStart + L * N + (i + 1) % N });

        var m = new Mesh { name = "Island", indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
        m.SetVertices(v); m.SetColors(c); m.SetTriangles(t, 0);
        m.SetUVs(0, v.Select(p => new Vector2(p.x, p.z) * 0.1f).ToList());
        FixWinding(m, sideStart, N);
        m.RecalculateNormals(); m.RecalculateTangents(); m.RecalculateBounds();
        return m;
    }

    // Make sure both cap and sides face outward (Unity uses clockwise front faces).
    static void FixWinding(Mesh m, int sideStart, int N)
    {
        var v = m.vertices; var t = m.triangles;
        Vector3 Nrm(int tri) => Vector3.Cross(v[t[tri + 1]] - v[t[tri]], v[t[tri + 2]] - v[t[tri]]);
        bool capUp = Nrm(0).y > 0;
        int firstSide = System.Array.FindIndex(Enumerable.Range(0, t.Length / 3).ToArray(), k => t[k * 3] >= sideStart) * 3;
        var sn = Nrm(firstSide);
        var centre = (v[t[firstSide]] + v[t[firstSide + 1]] + v[t[firstSide + 2]]) / 3f;
        bool sideOut = Vector3.Dot(sn, new Vector3(centre.x, 0, centre.z)) > 0;
        for (int k = 0; k < t.Length; k += 3)
        {
            bool isSide = t[k] >= sideStart;
            if ((isSide && !sideOut) || (!isSide && !capUp)) (t[k + 1], t[k + 2]) = (t[k + 2], t[k + 1]);
        }
        m.triangles = t;
    }

    static Mesh MountainRing(float radius, float maxH, int seed)
    {
        int seg = 512, rows = 18;
        var v = new List<Vector3>(); var c = new List<Color>(); var t = new List<int>();
        for (int i = 0; i < seg; i++)
        {
            float th = i / (float)seg * Mathf.PI * 2;
            // ridged noise for sharp peaks, taller in the background (+z) and lower behind the start
            float ridge = 1 - Mathf.Abs(Noise3(Mathf.Cos(th) * 3 + seed, 0, Mathf.Sin(th) * 3));
            float ridge2 = 1 - Mathf.Abs(Noise3(Mathf.Cos(th) * 9 + seed, 5, Mathf.Sin(th) * 9));
            float h = maxH * (0.25f + 0.55f * ridge * ridge + 0.2f * ridge2) * (0.75f + 0.35f * Mathf.Sin(th));
            for (int r = 0; r <= rows; r++)
            {
                float k = r / (float)rows;
                float y = h * k;
                float jag = Noise3(Mathf.Cos(th) * 20, k * 6 + seed, Mathf.Sin(th) * 20) * 25f * k;
                float rr = radius - 260 * (1 - k) * (1 - k) + jag;
                v.Add(new Vector3(Mathf.Cos(th) * rr, y, Mathf.Sin(th) * rr));
                c.Add(new Color(1, 1, 1, 1));
            }
        }
        for (int i = 0; i < seg; i++)
            for (int r = 0; r < rows; r++)
            {
                int a = i * (rows + 1) + r, b = ((i + 1) % seg) * (rows + 1) + r;
                t.AddRange(new[] { a, b, a + 1, b, b + 1, a + 1 });
            }
        var m = new Mesh { name = "Mountains", indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
        m.SetVertices(v); m.SetColors(c); m.SetTriangles(t, 0);
        m.SetUVs(0, v.Select(p => new Vector2(p.x, p.z) * 0.01f).ToList());
        // faces must point inward, toward the course
        var tv = m.vertices; var tt = m.triangles;
        var n0 = Vector3.Cross(tv[tt[1]] - tv[tt[0]], tv[tt[2]] - tv[tt[0]]);
        if (Vector3.Dot(n0, -tv[tt[0]]) < 0) { for (int k = 0; k < tt.Length; k += 3) (tt[k + 1], tt[k + 2]) = (tt[k + 2], tt[k + 1]); m.triangles = tt; }
        m.RecalculateNormals(); m.RecalculateTangents(); m.RecalculateBounds();
        return m;
    }

    static Mesh Grid(float size, int n)
    {
        var v = new List<Vector3>(); var t = new List<int>();
        for (int y = 0; y <= n; y++) for (int x = 0; x <= n; x++) v.Add(new Vector3((x / (float)n - 0.5f) * size, 0, (y / (float)n - 0.5f) * size));
        for (int y = 0; y < n; y++) for (int x = 0; x < n; x++) { int a = y * (n + 1) + x; t.AddRange(new[] { a, a + n + 1, a + 1, a + 1, a + n + 1, a + n + 2 }); }
        var m = new Mesh { name = "Grid" }; m.SetVertices(v); m.SetTriangles(t, 0); m.RecalculateNormals(); m.RecalculateBounds();
        return m;
    }

    static Mesh Torus(float R, float r, int seg, int sides)
    {
        var v = new List<Vector3>(); var t = new List<int>();
        for (int i = 0; i <= seg; i++)
            for (int j = 0; j <= sides; j++)
            {
                float a = i / (float)seg * Mathf.PI * 2, b = j / (float)sides * Mathf.PI * 2;
                v.Add(new Vector3((R + r * Mathf.Cos(b)) * Mathf.Cos(a), r * Mathf.Sin(b), (R + r * Mathf.Cos(b)) * Mathf.Sin(a)));
            }
        for (int i = 0; i < seg; i++)
            for (int j = 0; j < sides; j++)
            {
                int a = i * (sides + 1) + j, b = a + sides + 1;
                t.AddRange(new[] { a, a + 1, b, b, a + 1, b + 1 });
            }
        var m = new Mesh { name = "Torus" }; m.SetVertices(v); m.SetTriangles(t, 0); m.RecalculateNormals(); m.RecalculateBounds();
        return m;
    }

    static Mesh BannerMesh(float w, float h, int nx, int ny)
    {
        var v = new List<Vector3>(); var t = new List<int>();
        for (int y = 0; y <= ny; y++) for (int x = 0; x <= nx; x++) v.Add(new Vector3(x / (float)nx * w, (y / (float)ny - 0.5f) * h, 0));
        for (int y = 0; y < ny; y++) for (int x = 0; x < nx; x++) { int a = y * (nx + 1) + x; t.AddRange(new[] { a, a + nx + 1, a + 1, a + 1, a + nx + 1, a + nx + 2 }); }
        var m = new Mesh { name = "Banner" }; m.SetVertices(v); m.SetTriangles(t, 0); m.RecalculateNormals(); m.RecalculateBounds();
        m.bounds = new Bounds(new Vector3(w / 2, 0, 0), new Vector3(w, h, 2));
        return m;
    }

    // ---------------------------------------------------------------- helpers

    static Mesh SaveMesh(Mesh m, string name)
    {
        string path = MeshDir + name + ".asset";
        AssetDatabase.DeleteAsset(path);
        m.name = name;
        AssetDatabase.CreateAsset(m, path);
        return m;
    }

    static GameObject Place(Mesh mesh, Material mat, Vector3 pos, Transform parent, string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, true);
        go.transform.position = pos;
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        go.AddComponent<MeshRenderer>().sharedMaterial = mat;
        return go;
    }

    static CombineInstance CI(Mesh m, Vector3 pos, float yaw, float scale) =>
        new CombineInstance { mesh = m, transform = Matrix4x4.TRS(pos, Quaternion.Euler(0, yaw, 0), Vector3.one * scale) };

    // Scatter as individual GPU-instanced objects that share the Poly Haven meshes (keeps the project small).
    static void Combined(List<CombineInstance> parts, Material mat, Transform parent, string name)
    {
        if (parts.Count == 0) return;
        var group = new GameObject(name).transform;
        group.SetParent(parent, true);
        foreach (var p in parts)
        {
            var go = new GameObject(p.mesh.name);
            go.transform.SetParent(group, true);
            go.transform.SetPositionAndRotation(p.transform.GetColumn(3), p.transform.rotation);
            go.transform.localScale = p.transform.lossyScale;
            go.AddComponent<MeshFilter>().sharedMesh = p.mesh;
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }
    }

    static Vector2 Superellipse(float th, float a, float b, float e = 0.5f)
    {
        float c = Mathf.Cos(th), s = Mathf.Sin(th);
        return new Vector2(a * Mathf.Sign(c) * Mathf.Pow(Mathf.Abs(c), e), b * Mathf.Sign(s) * Mathf.Pow(Mathf.Abs(s), e));
    }

    static float Rand(System.Random r, float a, float b) => a + (float)r.NextDouble() * (b - a);

    static float Hash(int x, int y, int z)
    {
        unchecked
        {
            int h = x * 374761393 + y * 668265263 + z * 2147483647;
            h = (h ^ (h >> 13)) * 1274126177;
            return ((h ^ (h >> 16)) & 0xffffff) / (float)0xffffff;
        }
    }

    // Smooth 3D value noise in [-1, 1].
    static float Noise3(float x, float y, float z)
    {
        int xi = Mathf.FloorToInt(x), yi = Mathf.FloorToInt(y), zi = Mathf.FloorToInt(z);
        float xf = x - xi, yf = y - yi, zf = z - zi;
        float u = xf * xf * (3 - 2 * xf), v = yf * yf * (3 - 2 * yf), w = zf * zf * (3 - 2 * zf);
        float L(int dx, int dy, int dz) => Hash(xi + dx, yi + dy, zi + dz);
        float x00 = Mathf.Lerp(L(0, 0, 0), L(1, 0, 0), u), x10 = Mathf.Lerp(L(0, 1, 0), L(1, 1, 0), u);
        float x01 = Mathf.Lerp(L(0, 0, 1), L(1, 0, 1), u), x11 = Mathf.Lerp(L(0, 1, 1), L(1, 1, 1), u);
        return Mathf.Lerp(Mathf.Lerp(x00, x10, v), Mathf.Lerp(x01, x11, v), w) * 2 - 1;
    }

    static float PeriodicNoise(float x, float y, int period, int octave)
    {
        int xi = Mathf.FloorToInt(x), yi = Mathf.FloorToInt(y);
        float xf = x - xi, yf = y - yi;
        float u = xf * xf * (3 - 2 * xf), v = yf * yf * (3 - 2 * yf);
        float L(int dx, int dy) => Hash(((xi + dx) % period + period) % period, ((yi + dy) % period + period) % period, octave * 31);
        return Mathf.Lerp(Mathf.Lerp(L(0, 0), L(1, 0), u), Mathf.Lerp(L(0, 1), L(1, 1), u), v);
    }
}
