using System.Collections.Generic;
using UnityEngine;

// Moves back and forth between two points and carries any PlayerMotor standing in its rider trigger.
// The rider trigger is a child-or-own trigger collider that covers the area just above the platform top.
public class MovingPlatform : MonoBehaviour
{
    public Vector3 travel = new Vector3(0f, 0f, 8f);
    public float period = 6f;

    Vector3 start;
    readonly List<CharacterController> riders = new List<CharacterController>();

    void Start()
    {
        start = transform.position;
    }

    void LateUpdate()
    {
        float t = 0.5f - 0.5f * Mathf.Cos(Time.time * Mathf.PI * 2f / period);
        Vector3 target = start + travel * t;
        Vector3 delta = target - transform.position;
        transform.position = target;

        foreach (var rider in riders)
            if (rider && rider.enabled) rider.Move(delta);
    }

    void OnTriggerEnter(Collider other)
    {
        var cc = other as CharacterController;
        if (cc && !riders.Contains(cc)) riders.Add(cc);
    }

    void OnTriggerExit(Collider other)
    {
        var cc = other as CharacterController;
        if (cc) riders.Remove(cc);
    }
}
