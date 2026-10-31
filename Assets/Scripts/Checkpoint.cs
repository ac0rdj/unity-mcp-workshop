using UnityEngine;

// Trigger volume that updates where the player respawns.
[RequireComponent(typeof(Collider))]
public class Checkpoint : MonoBehaviour
{
    public Transform spawnPoint;

    void OnTriggerEnter(Collider other)
    {
        var motor = other.GetComponent<PlayerMotor>();
        if (!motor) return;
        Transform point = spawnPoint ? spawnPoint : transform;
        motor.SetCheckpoint(point.position, point.rotation);
    }
}
