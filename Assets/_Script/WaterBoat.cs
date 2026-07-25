using Ditzelgames;
using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Cinemachine;
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

    [Header("Audio")]
    [SerializeField] private AudioClip idleClip;
    [SerializeField] private AudioClip movingClip;
    [SerializeField] private float audioFadeSpeed = 2f;

    [Header("Impact")]
    [SerializeField] private AudioClip[] impactSounds;
    [SerializeField] private CinemachineCamera impactCamera;
    [SerializeField] private float impactForceThreshold = 2f;
    [SerializeField] private float impactShakeDuration = 0.3f;
    [SerializeField] private float impactShakeStrength = 1.5f;
    [SerializeField] private float impactCooldown = 0.4f;
    [SerializeField] private float impactPushbackForce = 8f;

    // -1..1: steer left/right. Set externally when UseKeyboardInput is false.
    public float SteerInput { get; set; }
    // -1..1: throttle forward/back. Set externally when UseKeyboardInput is false.
    public float ThrottleInput { get; set; }

    //used Components
    protected Rigidbody Rigidbody;
    protected Quaternion StartRotation;
    protected ParticleSystem ParticleSystem;
    protected Camera Camera;
    private AudioSource idleSource;
    private AudioSource movingSource;
    private AudioSource impactSource;
    private float lastImpactTime;

    //internal Properties
    protected Vector3 CamVel;

    public void Awake()
    {
        ParticleSystem = GetComponentInChildren<ParticleSystem>();
        Rigidbody = GetComponent<Rigidbody>();
        StartRotation = Motor.localRotation;
        Camera = Camera.main;

        idleSource = gameObject.AddComponent<AudioSource>();
        idleSource.clip = idleClip;
        idleSource.loop = true;
        idleSource.playOnAwake = true;
        idleSource.volume = 0f;
        idleSource.Play();

        movingSource = gameObject.AddComponent<AudioSource>();
        movingSource.clip = movingClip;
        movingSource.loop = true;
        movingSource.playOnAwake = false;
        movingSource.volume = 0f;
        movingSource.Play();

        impactSource = gameObject.AddComponent<AudioSource>();
        impactSource.playOnAwake = false;
    }

    public void FixedUpdate()
    {
        if (UseKeyboardInput)
            ReadKeyboardInput();

        //steer direction, clamp to [-1, 1] in case something feeds bad values
        var steer = Mathf.Clamp(SteerInput, -1f, 1f);

        //steering only works when moving
        float speed = Vector3.Dot(transform.forward, Rigidbody.linearVelocity);
        float steerFactor = Mathf.Clamp01(Mathf.Abs(speed) / 1f);

        //Rotational Force
        Rigidbody.AddForceAtPosition(steer * transform.right * SteerPower / 100f * steerFactor, Motor.position);

        //compute vectors
        var forward = Vector3.Scale(new Vector3(1, 0, 1), transform.forward);

        //forward/backward power, driven by ThrottleInput [-1,1]
        var throttle = Mathf.Clamp(ThrottleInput, -1f, 1f);
        if (throttle > 0f)
            PhysicsHelper.ApplyForceToReachVelocity(Rigidbody, forward * MaxSpeed * throttle, Power);
        else if (throttle < 0f)
            PhysicsHelper.ApplyForceToReachVelocity(Rigidbody, forward * MaxSpeed * throttle, Power);

        //Motor Animation // Particle system
        // Motor.SetPositionAndRotation(Motor.position, transform.rotation * StartRotation * Quaternion.Euler(0, 30f * steer, 0));

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

        //Audio crossfade based on actual movement
        bool isMoving = Rigidbody.linearVelocity.magnitude > 1f;
        float targetIdle = isMoving ? 0f : 0.1f;
        float targetMoving = isMoving ? 0.1f : 0f;
        idleSource.volume = Mathf.MoveTowards(idleSource.volume, targetIdle, audioFadeSpeed * Time.fixedDeltaTime);
        movingSource.volume = Mathf.MoveTowards(movingSource.volume, targetMoving, audioFadeSpeed * Time.fixedDeltaTime);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (Time.time - lastImpactTime < impactCooldown) return;

        float force = collision.relativeVelocity.magnitude;
        if (force < impactForceThreshold) return;

        lastImpactTime = Time.time;

        Rigidbody.AddForce(collision.GetContact(0).normal * impactPushbackForce, ForceMode.Impulse);

        if (impactSounds.Length > 0)
        {
            impactSource.PlayOneShot(impactSounds[UnityEngine.Random.Range(0, impactSounds.Length)]);
        }

        if (impactCamera != null)
        {
            float scaledStrength = impactShakeStrength * Mathf.Clamp01(force / (impactForceThreshold * 3f));
            impactCamera.transform.DOShakeRotation(impactShakeDuration, scaledStrength, 8, 60);
        }
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
