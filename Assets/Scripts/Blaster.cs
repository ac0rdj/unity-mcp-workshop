using UnityEngine;

// Click to blast yourself in the direction you're looking. Recharges when you land.
[RequireComponent(typeof(PlayerMotor))]
public class Blaster : MonoBehaviour
{
    public float blastSpeed = 18f;
    public float upwardBias = 3f;
    public int maxCharges = 1;
    public float cooldown = 0.25f;

    public int Charges { get; private set; }
    public int MaxCharges => maxCharges;

    PlayerMotor motor;
    float nextFireTime;

    void Awake()
    {
        motor = GetComponent<PlayerMotor>();
        Charges = maxCharges;
    }

    void Update()
    {
        if (motor.IsGrounded) Charges = maxCharges;

        if (Cursor.lockState == CursorLockMode.Locked && Input.GetMouseButtonDown(0)
            && Charges > 0 && Time.time >= nextFireTime)
        {
            Fire();
        }
    }

    void Fire()
    {
        Transform aim = motor.cameraPivot ? motor.cameraPivot : transform;
        motor.Launch(aim.forward * blastSpeed + Vector3.up * upwardBias);
        Charges--;
        nextFireTime = Time.time + cooldown;
        BlastFx.Spawn(transform.position - aim.forward * 0.8f);
    }
}
