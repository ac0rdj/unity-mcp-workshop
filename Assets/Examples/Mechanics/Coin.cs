using UnityEngine;

// Spinning, bobbing pickup. Collected when the player touches its trigger.
[RequireComponent(typeof(Collider))]
public class Coin : MonoBehaviour
{
    public float spinSpeed = 180f;
    public float bobHeight = 0.2f;
    public float bobSpeed = 2f;

    Vector3 basePosition;
    float phase;

    void Start()
    {
        basePosition = transform.position;
        phase = Random.value * Mathf.PI * 2f;
    }

    void Update()
    {
        transform.Rotate(0f, spinSpeed * Time.deltaTime, 0f, Space.Self);
        transform.position = basePosition + Vector3.up * (Mathf.Sin(Time.time * bobSpeed + phase) * bobHeight);
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.GetComponent<PlayerMotor>()) return;
        if (CoinCounter.Instance) CoinCounter.Instance.Collect();
        Destroy(gameObject);
    }
}
