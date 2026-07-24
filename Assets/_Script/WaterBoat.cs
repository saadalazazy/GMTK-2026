using Ditzelgames;
using System;
using System.Collections;
using System.Collections.Generic;
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

    [Header("Input")]
    [Tooltip("If true, this script reads WASD itself. Set to false when an external " +
             "controller (e.g. SteeringWheelInteractable) drives SteerInput/ThrottleInput instead.")]
    public bool UseKeyboardInput = true;

    // -1..1: steer left/right. Set externally when UseKeyboardInput is false.
    public float SteerInput { get; set; }
    // -1..1: throttle forward/back. Set externally when UseKeyboardInput is false.
    public float ThrottleInput { get; set; }

    //used Components
    protected Rigidbody Rigidbody;
    protected Quaternion StartRotation;
    protected ParticleSystem ParticleSystem;
    protected Camera Camera;

    //internal Properties
    protected Vector3 CamVel;

    public void Awake()
    {
        ParticleSystem = GetComponentInChildren<ParticleSystem>();
        Rigidbody = GetComponent<Rigidbody>();
        StartRotation = Motor.localRotation;
        Camera = Camera.main;
    }

    public void FixedUpdate()
    {
        if (UseKeyboardInput)
            ReadKeyboardInput();

        //steer direction, clamp to [-1, 1] in case something feeds bad values
        var steer = Mathf.Clamp(SteerInput, -1f, 1f);

        //Rotational Force
        Rigidbody.AddForceAtPosition(steer * transform.right * SteerPower / 100f, Motor.position);

        //compute vectors
        var forward = Vector3.Scale(new Vector3(1, 0, 1), transform.forward);

        //forward/backward power, driven by ThrottleInput [-1,1]
        var throttle = Mathf.Clamp(ThrottleInput, -1f, 1f);
        if (throttle > 0f)
            PhysicsHelper.ApplyForceToReachVelocity(Rigidbody, forward * MaxSpeed * throttle, Power);
        else if (throttle < 0f)
            PhysicsHelper.ApplyForceToReachVelocity(Rigidbody, forward * MaxSpeed * throttle, Power);

        //Motor Animation // Particle system
        Motor.SetPositionAndRotation(Motor.position, transform.rotation * StartRotation * Quaternion.Euler(0, 30f * steer, 0));

        if (ParticleSystem != null)
        {
            if (Mathf.Abs(throttle) > 0.01f)
                ParticleSystem.Play();
            else
                ParticleSystem.Pause();
        }

        //moving forward: use Dot product (not Cross) to check alignment with facing direction
        var movingForward = Vector3.Dot(transform.forward, Rigidbody.linearVelocity) > 0;

        //move in direction (aligns velocity toward facing direction, or away from it if reversing)
        Rigidbody.linearVelocity = Quaternion.AngleAxis(
            Vector3.SignedAngle(Rigidbody.linearVelocity, (movingForward ? 1f : -1f) * transform.forward, Vector3.up) * Drag,
            Vector3.up
        ) * Rigidbody.linearVelocity;

        //camera position
        //Camera.transform.LookAt(transform.position + transform.forward * 6f + transform.up * 2f);
        //Camera.transform.position = Vector3.SmoothDamp(Camera.transform.position, transform.position + transform.forward * -8f + transform.up * 2f, ref CamVel, 0.05f);
    }

    private void ReadKeyboardInput()
    {
        float steer = 0f;
        if (Input.GetKey(KeyCode.A))
            steer = 1f;
        if (Input.GetKey(KeyCode.D))
            steer = -1f;

        float throttle = 0f;
        if (Input.GetKey(KeyCode.W))
            throttle = 1f;
        if (Input.GetKey(KeyCode.S))
            throttle = -1f;

        SteerInput = steer;
        ThrottleInput = throttle;
    }
}