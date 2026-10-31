using UnityEngine;

// First-person movement that keeps momentum in the air, so blaster hops carry you far.
[RequireComponent(typeof(CharacterController))]
public class PlayerMotor : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 7f;
    public float groundAccel = 60f;
    public float airAccel = 12f;
    public float jumpSpeed = 8f;
    public float gravity = -20f;

    [Header("Look")]
    public Transform cameraPivot;
    public float mouseSensitivity = 2f;

    [Header("Respawn")]
    public float killHeight = -15f;

    public Vector3 Velocity => velocity;
    public bool IsGrounded => controller.isGrounded;

    CharacterController controller;
    Vector3 velocity;
    float pitch;
    Vector3 respawnPoint;
    Quaternion respawnRotation;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        SetCheckpoint(transform.position, transform.rotation);
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        HandleCursor();
        if (Cursor.lockState == CursorLockMode.Locked) Look();
        Move();

        if (transform.position.y < killHeight) Respawn();
    }

    void HandleCursor()
    {
        if (Input.GetKeyDown(KeyCode.Escape)) Cursor.lockState = CursorLockMode.None;
        if (Input.GetMouseButtonDown(0) && Cursor.lockState != CursorLockMode.Locked)
            Cursor.lockState = CursorLockMode.Locked;
    }

    void Look()
    {
        transform.Rotate(0f, Input.GetAxis("Mouse X") * mouseSensitivity, 0f);
        pitch = Mathf.Clamp(pitch - Input.GetAxis("Mouse Y") * mouseSensitivity, -89f, 89f);
        if (cameraPivot) cameraPivot.localEulerAngles = new Vector3(pitch, 0f, 0f);
    }

    void Move()
    {
        float dt = Time.deltaTime;
        Vector3 input = transform.right * Input.GetAxisRaw("Horizontal") + transform.forward * Input.GetAxisRaw("Vertical");
        input = Vector3.ClampMagnitude(input, 1f);

        Vector3 horizontal = new Vector3(velocity.x, 0f, velocity.z);
        if (controller.isGrounded)
        {
            horizontal = Vector3.MoveTowards(horizontal, input * moveSpeed, groundAccel * dt);
        }
        else
        {
            // Air control steers but never brakes below current speed, so momentum is kept.
            float cap = Mathf.Max(horizontal.magnitude, moveSpeed);
            horizontal = Vector3.ClampMagnitude(horizontal + input * airAccel * dt, cap);
        }
        velocity.x = horizontal.x;
        velocity.z = horizontal.z;

        if (controller.isGrounded && velocity.y < 0f) velocity.y = -2f;
        if (controller.isGrounded && Input.GetButtonDown("Jump")) velocity.y = jumpSpeed;

        velocity.y += gravity * dt;
        CollisionFlags flags = controller.Move(velocity * dt);
        if ((flags & CollisionFlags.Above) != 0 && velocity.y > 0f) velocity.y = 0f;
    }

    public void Launch(Vector3 newVelocity)
    {
        velocity = newVelocity;
    }

    public void SetCheckpoint(Vector3 position, Quaternion rotation)
    {
        respawnPoint = position;
        respawnRotation = rotation;
    }

    public void Respawn()
    {
        controller.enabled = false;
        transform.SetPositionAndRotation(respawnPoint, respawnRotation);
        controller.enabled = true;
        velocity = Vector3.zero;
    }
}
