using UnityEngine;

// Trigger volume at the end of the course.
[RequireComponent(typeof(Collider))]
public class Goal : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<PlayerMotor>() && GameHUD.Instance) GameHUD.Instance.Finish();
    }
}
