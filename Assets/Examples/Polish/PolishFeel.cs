using UnityEngine;

// Game-feel layer for the parkour demo. Purely observational: it watches Blaster.Charges,
// PlayerMotor.Velocity / IsGrounded, Checkpoint and Goal volumes, and never touches the base scripts.
public class PolishFeel : MonoBehaviour
{
    const string BestKey = "Polish.BestTime";

    [Header("FOV")]
    public float baseFov = 80f;
    public float blastKickFov = 12f;
    public float kickRecovery = 6f;
    public float speedFovMax = 8f;
    public float speedFovRange = 20f;

    [Header("Shake / dip")]
    public float shakeDuration = 0.2f;
    public float shakeMagnitude = 0.06f;
    public float hardLandSpeed = 12f;
    public float landDipDepth = 0.25f;
    public float dipRecovery = 8f;

    PlayerMotor motor;
    Blaster blaster;
    Camera cam;
    Transform camT;
    Vector3 camBasePos;
    AudioSource audioSrc;
    AudioClip whoomp, thud, blip, chime;
    ParticleSystem blastPs, dustPs;

    Checkpoint[] checkpoints;
    Collider[] checkpointCols;
    bool[] inCheckpoint;
    Collider goalCol;
    bool inGoal;

    int lastCharges;
    bool wasGrounded;
    float fallSpeed;
    float fovKick, speedFov, shakeTimer, dip;

    float startTime, best = -1f, runTime = -1f;
    bool newBest;
    GUIStyle style;

    void Start()
    {
        motor = FindObjectOfType<PlayerMotor>();
        blaster = FindObjectOfType<Blaster>();
        if (!motor) { enabled = false; return; }

        cam = motor.GetComponentInChildren<Camera>();
        if (cam)
        {
            camT = cam.transform;
            camBasePos = camT.localPosition;
            cam.fieldOfView = baseFov;
        }

        audioSrc = gameObject.AddComponent<AudioSource>();
        audioSrc.spatialBlend = 0f;
        audioSrc.playOnAwake = false;
        whoomp = MakeWhoomp();
        thud = MakeThud();
        blip = MakeBlip();
        chime = MakeChime();

        blastPs = MakeParticles("BlastBurst", new Color(1f, 0.65f, 0.15f), 0.45f, 3f, 14f, 0.12f, 40, true);
        dustPs = MakeParticles("LandDust", new Color(0.75f, 0.7f, 0.6f, 0.7f), 0.6f, 0.5f, 3f, 0.35f, 14, false);

        checkpoints = FindObjectsOfType<Checkpoint>();
        checkpointCols = new Collider[checkpoints.Length];
        inCheckpoint = new bool[checkpoints.Length];
        for (int i = 0; i < checkpoints.Length; i++) checkpointCols[i] = checkpoints[i].GetComponent<Collider>();
        var goal = FindObjectOfType<Goal>();
        if (goal) goalCol = goal.GetComponent<Collider>();

        best = PlayerPrefs.GetFloat(BestKey, -1f);
        startTime = Time.time;
        lastCharges = blaster ? blaster.Charges : 0;
        wasGrounded = motor.IsGrounded;
    }

    void LateUpdate()
    {
        if (!motor) return;
        float dt = Time.deltaTime;
        Vector3 v = motor.Velocity;
        bool grounded = motor.IsGrounded;
        Vector3 feet = motor.transform.position;

        // Blast: charges dropped.
        bool blasted = false;
        if (blaster)
        {
            if (blaster.Charges < lastCharges) { blasted = true; OnBlast(feet); }
            lastCharges = blaster.Charges;
        }

        // Jump: pressed while grounded last frame.
        if (!blasted && wasGrounded && Input.GetButtonDown("Jump") && v.y > 1f)
            audioSrc.PlayOneShot(blip, 0.5f);

        // Landing.
        if (!grounded) fallSpeed = Mathf.Min(fallSpeed, v.y);
        if (grounded && !wasGrounded)
        {
            float impact = -fallSpeed;
            if (impact >= hardLandSpeed) OnHardLand(feet, impact);
            else if (impact > 5f) audioSrc.PlayOneShot(thud, 0.25f);
            fallSpeed = 0f;
        }
        if (grounded) fallSpeed = 0f;
        wasGrounded = grounded;

        PollTriggers(feet);
        UpdateCamera(dt, v);
    }

    void OnBlast(Vector3 pos)
    {
        Vector3 fwd = camT ? camT.forward : motor.transform.forward;
        Burst(blastPs, pos + Vector3.up * 0.9f - fwd * 0.8f, -fwd, 40);
        fovKick = blastKickFov;
        shakeTimer = shakeDuration;
        audioSrc.PlayOneShot(whoomp, 0.9f);
    }

    void OnHardLand(Vector3 feet, float impact)
    {
        dip = Mathf.Clamp01(impact / 25f) * landDipDepth + 0.05f;
        Burst(dustPs, feet + Vector3.up * 0.1f, Vector3.up, 14);
        audioSrc.PlayOneShot(thud, 0.7f);
    }

    void PollTriggers(Vector3 p)
    {
        Vector3 center = p + Vector3.up * 0.9f;
        for (int i = 0; i < checkpointCols.Length; i++)
        {
            if (!checkpointCols[i]) continue;
            bool inside = checkpointCols[i].bounds.Contains(center);
            if (inside && !inCheckpoint[i]) audioSrc.PlayOneShot(chime, 0.6f);
            inCheckpoint[i] = inside;
        }

        if (goalCol)
        {
            bool inside = goalCol.bounds.Contains(center);
            if (inside && !inGoal && runTime < 0f)
            {
                runTime = Time.time - startTime;
                newBest = best < 0f || runTime < best;
                if (newBest)
                {
                    best = runTime;
                    PlayerPrefs.SetFloat(BestKey, best);
                    PlayerPrefs.Save();
                }
                audioSrc.PlayOneShot(chime, 0.8f);
            }
            inGoal = inside;
        }
    }

    void UpdateCamera(float dt, Vector3 v)
    {
        if (!cam) return;
        float hSpeed = new Vector2(v.x, v.z).magnitude;
        float speedTarget = Mathf.Clamp01((hSpeed - motor.moveSpeed) / speedFovRange) * speedFovMax;
        speedFov = Mathf.Lerp(speedFov, speedTarget, 1f - Mathf.Exp(-3f * dt));
        fovKick = Mathf.Lerp(fovKick, 0f, 1f - Mathf.Exp(-kickRecovery * dt));
        cam.fieldOfView = baseFov + speedFov + fovKick;

        Vector3 offset = Vector3.zero;
        if (shakeTimer > 0f)
        {
            shakeTimer -= dt;
            offset += Random.insideUnitSphere * shakeMagnitude * Mathf.Clamp01(shakeTimer / shakeDuration);
        }
        dip = Mathf.Lerp(dip, 0f, 1f - Mathf.Exp(-dipRecovery * dt));
        offset += Vector3.down * dip;
        camT.localPosition = camBasePos + offset;
    }

    void OnGUI()
    {
        if (style == null) style = new GUIStyle(GUI.skin.label) { fontSize = 22 };
        string text = best >= 0f ? $"Best  {best:0.00} s" : "Best  --";
        if (runTime >= 0f && newBest) text += "  (new!)";
        GUI.Label(new Rect(20, 75, 400, 30), text, style);
    }

    // ---------- Particles ----------

    static Texture2D softTex;

    static Material MakeParticleMaterial()
    {
        if (softTex == null)
        {
            softTex = new Texture2D(32, 32, TextureFormat.RGBA32, false);
            for (int y = 0; y < 32; y++)
                for (int x = 0; x < 32; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), new Vector2(15.5f, 15.5f)) / 16f;
                    softTex.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01(1f - d) * Mathf.Clamp01(1f - d)));
                }
            softTex.Apply();
        }
        Shader sh = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (sh == null) sh = Shader.Find("Particles/Standard Unlit");
        if (sh == null) sh = Shader.Find("Sprites/Default");
        var mat = new Material(sh);
        mat.mainTexture = softTex;
        if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", softTex);
        return mat;
    }

    ParticleSystem MakeParticles(string name, Color color, float life, float minSpeed, float maxSpeed, float size, int count, bool sparks)
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform, false);
        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ps.main;
        main.playOnAwake = false;
        main.loop = false;
        main.duration = 1f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(life * 0.6f, life);
        main.startSpeed = new ParticleSystem.MinMaxCurve(minSpeed, maxSpeed);
        main.startSize = new ParticleSystem.MinMaxCurve(size * 0.6f, size * 1.4f);
        main.gravityModifier = sparks ? 0.6f : -0.05f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 200;
        if (sparks)
        {
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.85f, 0.2f), new Color(1f, 0.4f, 0.05f));
        }
        else main.startColor = color;

        var emission = ps.emission;
        emission.enabled = false;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = sparks ? 35f : 70f;
        shape.radius = sparks ? 0.1f : 0.4f;

        var col = ps.colorOverLifetime;
        col.enabled = true;
        var g = new Gradient();
        g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                  new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
        col.color = g;

        var sz = ps.sizeOverLifetime;
        sz.enabled = true;
        sz.size = new ParticleSystem.MinMaxCurve(1f, sparks ? AnimationCurve.Linear(0, 1, 1, 0.1f) : AnimationCurve.Linear(0, 0.6f, 1, 1.8f));

        var r = go.GetComponent<ParticleSystemRenderer>();
        r.sharedMaterial = MakeParticleMaterial();

        if (sparks)
        {
            // Smoke puff child.
            var smokeGo = new GameObject("Smoke");
            smokeGo.transform.SetParent(go.transform, false);
            var sps = smokeGo.AddComponent<ParticleSystem>();
            sps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var sm = sps.main;
            sm.playOnAwake = false;
            sm.loop = false;
            sm.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 0.9f);
            sm.startSpeed = new ParticleSystem.MinMaxCurve(0.5f, 3f);
            sm.startSize = new ParticleSystem.MinMaxCurve(0.4f, 0.8f);
            sm.startColor = new Color(0.45f, 0.45f, 0.45f, 0.6f);
            sm.simulationSpace = ParticleSystemSimulationSpace.World;
            sm.gravityModifier = -0.05f;
            var se = sps.emission; se.enabled = false;
            var ss = sps.shape; ss.shapeType = ParticleSystemShapeType.Sphere; ss.radius = 0.25f;
            var sc = sps.colorOverLifetime; sc.enabled = true; sc.color = g;
            var ssz = sps.sizeOverLifetime; ssz.enabled = true;
            ssz.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0, 0.5f, 1, 2f));
            smokeGo.GetComponent<ParticleSystemRenderer>().sharedMaterial = MakeParticleMaterial();
        }
        return ps;
    }

    static void Burst(ParticleSystem ps, Vector3 pos, Vector3 dir, int count)
    {
        ps.transform.position = pos;
        ps.transform.rotation = Quaternion.LookRotation(dir);
        ps.Emit(count);
        foreach (var child in ps.GetComponentsInChildren<ParticleSystem>())
        {
            if (child == ps) continue;
            child.transform.position = pos;
            child.Emit(Mathf.Max(6, count / 4));
        }
    }

    // ---------- Procedural audio ----------

    const int Rate = 44100;

    static AudioClip Build(string name, float seconds, System.Func<float, float, float> gen)
    {
        int n = (int)(Rate * seconds);
        var data = new float[n];
        for (int i = 0; i < n; i++) data[i] = Mathf.Clamp(gen(i / (float)Rate, i / (float)n), -1f, 1f);
        var clip = AudioClip.Create(name, n, 1, Rate, false);
        clip.SetData(data, 0);
        return clip;
    }

    static AudioClip MakeWhoomp()
    {
        float lp = 0f, phase = 0f;
        var rng = new System.Random(1);
        return Build("whoomp", 0.5f, (t, u) =>
        {
            float noise = (float)(rng.NextDouble() * 2 - 1);
            float cutoff = Mathf.Lerp(0.25f, 0.02f, u);        // filter closes over time
            lp += (noise - lp) * cutoff;
            float freq = Mathf.Lerp(160f, 40f, Mathf.Pow(u, 0.5f)); // pitch drop
            phase += 2 * Mathf.PI * freq / Rate;
            float env = Mathf.Exp(-5f * u) * Mathf.Min(1f, t * 200f);
            return (lp * 1.8f + Mathf.Sin(phase) * 0.7f) * env;
        });
    }

    static AudioClip MakeThud()
    {
        float lp = 0f, phase = 0f;
        var rng = new System.Random(2);
        return Build("thud", 0.18f, (t, u) =>
        {
            lp += ((float)(rng.NextDouble() * 2 - 1) - lp) * 0.08f;
            phase += 2 * Mathf.PI * Mathf.Lerp(90f, 50f, u) / Rate;
            return (Mathf.Sin(phase) * 0.8f + lp * 0.6f) * Mathf.Exp(-18f * t);
        });
    }

    static AudioClip MakeBlip()
    {
        float phase = 0f;
        return Build("blip", 0.1f, (t, u) =>
        {
            phase += 2 * Mathf.PI * Mathf.Lerp(500f, 900f, u) / Rate;
            return Mathf.Sin(phase) * 0.5f * (1f - u) * Mathf.Min(1f, t * 500f);
        });
    }

    static AudioClip MakeChime()
    {
        return Build("chime", 0.7f, (t, u) =>
        {
            float a = Mathf.Sin(2 * Mathf.PI * 880f * t) * Mathf.Exp(-6f * t);
            float tb = Mathf.Max(0f, t - 0.12f);
            float b = Mathf.Sin(2 * Mathf.PI * 1318.5f * tb) * Mathf.Exp(-6f * tb) * (t > 0.12f ? 1f : 0f);
            return (a + b) * 0.4f;
        });
    }
}
