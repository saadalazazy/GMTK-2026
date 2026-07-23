using Ditzelgames;
using UnityEngine;

[RequireComponent(typeof(WaterFloat))]
public class WaterBoat : MonoBehaviour
{
    //visible Properties
    public Transform Motor;
    public float SteerPower = 500f;
    public float Power = 5f;
    public float MaxSpeed = 10f;
    public float Drag = 0.1f;
    public float SteerSmoothing = 5f; // higher = snappier turning response

    //used Components
    protected Rigidbody Rigidbody;
    protected Quaternion StartRotation;
    protected ParticleSystem ParticleSystem;

    //internal Properties
    protected float currentSteer;

    public void Awake()
    {
        ParticleSystem = GetComponentInChildren<ParticleSystem>();
        Rigidbody = GetComponent<Rigidbody>();
        StartRotation = Motor.localRotation;
    }

    public void FixedUpdate()
    {
        //steer direction [-1,0,1], smoothed so the rudder doesn't snap instantly
        float targetSteer = 0f;
        if (Input.GetKey(KeyCode.A)) targetSteer = 1f;
        if (Input.GetKey(KeyCode.D)) targetSteer = -1f;
        currentSteer = Mathf.MoveTowards(currentSteer, targetSteer, SteerSmoothing * Time.fixedDeltaTime);

        //Rotational Force
        Rigidbody.AddForceAtPosition(currentSteer * transform.right * SteerPower / 100f, Motor.position);

        //compute vectors
        var forward = Vector3.Scale(new Vector3(1, 0, 1), transform.forward);

        //forward/backward power
        if (Input.GetKey(KeyCode.W))
            PhysicsHelper.ApplyForceToReachVelocity(Rigidbody, forward * MaxSpeed, Power);
        if (Input.GetKey(KeyCode.S))
            PhysicsHelper.ApplyForceToReachVelocity(Rigidbody, forward * -MaxSpeed, Power);

        //hard cap so external forces (waves, collisions) can't push the boat past MaxSpeed indefinitely
        var flatVel = Vector3.Scale(Rigidbody.linearVelocity, new Vector3(1, 0, 1));
        if (flatVel.magnitude > MaxSpeed)
        {
            var clamped = Vector3.ClampMagnitude(flatVel, MaxSpeed);
            Rigidbody.linearVelocity = new Vector3(clamped.x, Rigidbody.linearVelocity.y, clamped.z);
        }

        //Motor Animation / Particle system
        Motor.SetPositionAndRotation(Motor.position, transform.rotation * StartRotation * Quaternion.Euler(0, 30f * currentSteer, 0));
        if (ParticleSystem != null)
        {
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.S))
                ParticleSystem.Play();
            else
                ParticleSystem.Pause();
        }

        //moving forward?
        var movingForward = Vector3.Cross(transform.forward, Rigidbody.linearVelocity).y < 0;

        //bleed off sideways drift, turning velocity toward facing direction
        Rigidbody.linearVelocity = Quaternion.AngleAxis(
            Vector3.SignedAngle(Rigidbody.linearVelocity, (movingForward ? 1f : 0f) * transform.forward, Vector3.up) * Drag,
            Vector3.up) * Rigidbody.linearVelocity;
    }
}
