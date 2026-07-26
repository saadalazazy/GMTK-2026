using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class ArrowController : MonoBehaviour
{
    [SerializeField] private float lifetime = 5f;
    [SerializeField] private float rotationSmoothing = 10f;
    [SerializeField] private float damageAmount = 50f;

    Rigidbody rb;
    bool hasHit;

    void OnEnable()
    {
        rb = GetComponent<Rigidbody>();
    }

    // void FixedUpdate()
    // {
    //     if (hasHit || rb.linearVelocity.sqrMagnitude < 0.1f) return;
    //
    //     Quaternion targetRotation = Quaternion.LookRotation(rb.linearVelocity.normalized, Vector3.up);
    //     rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRotation, rotationSmoothing * Time.fixedDeltaTime));
    // }

    void OnCollisionEnter(Collision collision)
    {
        print("Arrow hit shark: " + collision.gameObject.name);

        hasHit = true;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.isKinematic = true;

        SharkEnemy shark = collision.gameObject.GetComponentInParent<SharkEnemy>();
        if (shark != null)
        {
            shark.TakeDamage(damageAmount);
        }

        transform.SetParent(collision.transform);

        ContactPoint contact = collision.GetContact(0);
        transform.rotation = Quaternion.LookRotation(-contact.normal, Vector3.up);
    }
}
