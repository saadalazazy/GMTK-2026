using DG.Tweening.Core.Easing;
using UnityEngine;

public class Floater : MonoBehaviour
{
    [Header("Components")]
    public Rigidbody rigidBody;

    [Header("Buoyancy Settings")]
    public float depthBeforeSubmerged = 1f;
    public float displacementAmount = 3f;
    public int floaterCount = 1;

    [Header("Water Resistance")]
    public float waterDrag = 0.99f;
    public float waterAngularDrag = 0.5f;

    private void FixedUpdate()
    {
        // 1. Distribute normal gravity force across all floaters on the object
        rigidBody.AddForceAtPosition(Physics.gravity / floaterCount, transform.position, ForceMode.Acceleration);

        // 2. Query WaveManager for the dynamic water height at this object's X position
        float waveHeight = WaterWaveManager.Instance.GetWaveHeight(transform.position.x);

        // 3. Check if this floater is below the moving wave height
        if (transform.position.y < waveHeight)
        {
            // Calculate how deeply submerged the floater is relative to the wave height
            float displacementMultiplier = Mathf.Clamp01((waveHeight - transform.position.y) / depthBeforeSubmerged) * displacementAmount;

            // Apply upward buoyancy force at this floater's specific position
            rigidBody.AddForceAtPosition(
                new Vector3(0f, Mathf.Abs(Physics.gravity.y) * displacementMultiplier, 0f),
                transform.position,
                ForceMode.Acceleration
            );

            // Apply linear drag (slows down linear movement underwater)
            rigidBody.AddForce(
                displacementMultiplier * -rigidBody.linearVelocity * waterDrag * Time.fixedDeltaTime,
                ForceMode.VelocityChange
            );

            // Apply angular drag (slows down rotation underwater)
            rigidBody.AddTorque(
                displacementMultiplier * -rigidBody.angularVelocity * waterAngularDrag * Time.fixedDeltaTime,
                ForceMode.VelocityChange
            );
        }
    }
}