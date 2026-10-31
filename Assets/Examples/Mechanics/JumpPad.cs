using UnityEngine;

// Launches the player when stepped on. Launch velocity is in the pad's local space.
[RequireComponent(typeof(Collider))]
public class JumpPad : MonoBehaviour
{
    public Vector3 launchVelocity = new Vector3(0f, 17f, 6f);

    void OnTriggerEnter(Collider other)
    {
        var motor = other.GetComponent<PlayerMotor>();
        if (motor) motor.Launch(transform.TransformDirection(launchVelocity));
    }
}
