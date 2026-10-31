using UnityEngine;

// Placeholder blast effect: an expanding sphere that disappears. Swap for a real VFX later.
public class BlastFx : MonoBehaviour
{
    public float lifetime = 0.3f;
    public float endScale = 2.5f;

    float age;

    public static void Spawn(Vector3 position)
    {
        GameObject fx = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        fx.name = "BlastFx";
        Destroy(fx.GetComponent<Collider>());
        fx.transform.position = position;
        fx.transform.localScale = Vector3.one * 0.3f;
        var renderer = fx.GetComponent<Renderer>();
        renderer.material.color = new Color(1f, 0.55f, 0.1f);
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        fx.AddComponent<BlastFx>();
    }

    void Update()
    {
        age += Time.deltaTime;
        float t = age / lifetime;
        transform.localScale = Vector3.one * Mathf.Lerp(0.3f, endScale, t);
        if (t >= 1f) Destroy(gameObject);
    }
}
