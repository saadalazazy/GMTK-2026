using UnityEngine;
using UnityEngine.Events;

public class LighthouseGoal : MonoBehaviour
{
    [Header("Events")]
    public UnityEvent onBoatReached;

    private bool triggered;

    void OnTriggerEnter(Collider other)
    {
        if (triggered) return;

        WaterBoat boat = other.GetComponentInParent<WaterBoat>();
        if (boat == null && other.attachedRigidbody != null)
            boat = other.attachedRigidbody.GetComponent<WaterBoat>();

        if (boat != null)
        {
            triggered = true;
            onBoatReached?.Invoke();
        }
    }
}
