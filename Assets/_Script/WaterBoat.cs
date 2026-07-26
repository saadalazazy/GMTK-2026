using Ditzelgames;
using DG.Tweening;
using TMPro; // ==================== NEW ====================
using Unity.Cinemachine;
using UnityEngine;

[RequireComponent(typeof(WaterFloat))]
public class WaterBoat : MonoBehaviour
{
    public Transform Motor;
    public float SteerPower = 500f;
    public float Power = 5f;
    public float MaxSpeed = 10f;
    public float Drag = 0.1f;

    [Header("Input")]
    [Tooltip("If true, this script reads WASD itself. Set to false when an external controller drives SteerInput/ThrottleInput instead.")]
    public bool UseKeyboardInput = true;

    [Header("Audio")]
    [SerializeField] private AudioClip idleClip;
    [SerializeField] private AudioClip movingClip;
    [SerializeField] private AudioClip engineFailClip;
    [SerializeField] private AudioClip engineStopClip;
    [SerializeField] private float audioFadeSpeed = 2f;

    [Header("Steam Warning")]
    [SerializeField] private bool steamWarning;
    [SerializeField] private AudioClip steamWarningClip;
    [SerializeField, Range(0f, 1f)] private float steamWarningVolume = 1f;
    [SerializeField] private ParticleSystem[] steamWarningParticles;

    // ==================== NEW: STATE UI ====================
    [Header("Boat State UI")]
    [SerializeField] private TMP_Text stateText;
    [SerializeField] private CanvasGroup stateCanvasGroup;
    [SerializeField] private float stateFadeDuration = 0.2f;

    [TextArea] [SerializeField] private string normalStateText = "Boat is stable";
    [TextArea] [SerializeField] private string engineFailedStateText = "Engine failed!";
    [TextArea] [SerializeField] private string steamWarningStateText = "Steam pressure warning!";
    [TextArea] [SerializeField] private string engineFireAndSteamStateText =
        "Engine fire and steam pressure warning!";
    // ================== END NEW: STATE UI ==================

    [Header("Impact")]
    [SerializeField] private AudioClip[] impactSounds;
    [SerializeField] private CinemachineCamera impactCamera;
    [SerializeField] private float impactForceThreshold = 2f;
    [SerializeField] private float impactShakeDuration = 0.3f;
    [SerializeField] private float impactShakeStrength = 1.5f;
    [SerializeField] private float impactCooldown = 0.4f;
    [SerializeField] private float impactPushbackForce = 8f;
    [SerializeField] private EngienManager engienManager;

    public float SteerInput { get; set; }
    public float ThrottleInput { get; set; }

    protected Rigidbody Rigidbody;
    protected Quaternion StartRotation;
    protected ParticleSystem ParticleSystem;
    protected Camera Camera;
    private AudioSource idleSource;
    private AudioSource movingSource;
    private AudioSource impactSource;
    private AudioSource engineEffectSource;
    private AudioSource steamWarningSource;
    private float lastImpactTime;
    private bool engineHasFailed;
    private bool wasMoving;
    private bool wasTryingToMoveWhileFailed;

    // ==================== NEW: STATE UI ====================
    private BoatState currentBoatState = (BoatState)(-1);
    private Tween stateFadeTween;

    private enum BoatState
    {
        Normal,
        EngineFailed,
        SteamWarning,
        EngineFireAndSteam
    }
    // ================== END NEW: STATE UI ==================

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

        engineEffectSource = gameObject.AddComponent<AudioSource>();
        engineEffectSource.playOnAwake = false;

        steamWarningSource = gameObject.AddComponent<AudioSource>();
        steamWarningSource.clip = steamWarningClip;
        steamWarningSource.loop = true;
        steamWarningSource.playOnAwake = false;
        steamWarningSource.volume = steamWarningVolume;

        StopSteamWarningParticles();

        if (steamWarning)
        {
            if (steamWarningClip != null)
                steamWarningSource.Play();

            PlaySteamWarningParticles();
        }
    }

    public void FixedUpdate()
    {
        if (UseKeyboardInput)
            ReadKeyboardInput();

        bool hasEngineFire = engienManager != null && engienManager.HasActiveFires;

        // ==================== NEW: UPDATE UI STATE ====================
        if (hasEngineFire && steamWarning)
            SetBoatStateUI(BoatState.EngineFireAndSteam);
        else if (hasEngineFire)
            SetBoatStateUI(BoatState.EngineFailed);
        else if (steamWarning)
            SetBoatStateUI(BoatState.SteamWarning);
        else
            SetBoatStateUI(BoatState.Normal);
        // ================== END NEW: UPDATE UI STATE ==================

        if (hasEngineFire)
        {
            if (!engineHasFailed)
            {
                if (wasMoving)
                    PlayEngineEffect(engineStopClip);

                engineHasFailed = true;
                wasMoving = false;
            }

            bool isTryingToMove = Mathf.Abs(ThrottleInput) > 0.01f;
            if (isTryingToMove && !wasTryingToMoveWhileFailed)
                PlayEngineEffect(engineFailClip);

            wasTryingToMoveWhileFailed = isTryingToMove;
            SteerInput = 0f;
            ThrottleInput = 0f;
            Rigidbody.linearVelocity = Vector3.zero;
            Rigidbody.angularVelocity = Vector3.zero;

            if (ParticleSystem != null)
                ParticleSystem.Pause();

            idleSource.volume = 0.1f;
            movingSource.volume = 0f;
            return;
        }

        if (steamWarning)
        {
            StopBoat();
            return;
        }

        engineHasFailed = false;
        wasTryingToMoveWhileFailed = false;

        var steer = Mathf.Clamp(SteerInput, -1f, 1f);
        float speed = Vector3.Dot(transform.forward, Rigidbody.linearVelocity);
        float steerFactor = Mathf.Clamp01(Mathf.Abs(speed));
        Rigidbody.AddForceAtPosition(steer * transform.right * SteerPower / 100f * steerFactor, Motor.position);

        var forward = Vector3.Scale(new Vector3(1, 0, 1), transform.forward);
        var throttle = Mathf.Clamp(ThrottleInput, -1f, 1f);
        if (throttle != 0f)
            PhysicsHelper.ApplyForceToReachVelocity(Rigidbody, forward * MaxSpeed * throttle, Power);

        if (ParticleSystem != null)
        {
            if (Mathf.Abs(throttle) > 0.01f)
                ParticleSystem.Play();
            else
                ParticleSystem.Pause();
        }

        var movingForward = Vector3.Dot(transform.forward, Rigidbody.linearVelocity) > 0;
        Rigidbody.linearVelocity = Quaternion.AngleAxis(
            Vector3.SignedAngle(Rigidbody.linearVelocity, (movingForward ? 1f : -1f) * transform.forward, Vector3.up) * Drag,
            Vector3.up) * Rigidbody.linearVelocity;

        bool isMoving = Rigidbody.linearVelocity.magnitude > 1f;
        if (wasMoving && !isMoving)
            PlayEngineEffect(engineStopClip);

        wasMoving = isMoving;
        if (engienManager != null)
            engienManager.SetBoatMoving(isMoving);

        float targetIdle = isMoving ? 0f : 0.1f;
        float targetMoving = isMoving ? 0.1f : 0f;
        idleSource.volume = Mathf.MoveTowards(idleSource.volume, targetIdle, audioFadeSpeed * Time.fixedDeltaTime);
        movingSource.volume = Mathf.MoveTowards(movingSource.volume, targetMoving, audioFadeSpeed * Time.fixedDeltaTime);
    }

    private void PlayEngineEffect(AudioClip clip)
    {
        if (clip != null)
            engineEffectSource.PlayOneShot(clip);
    }

    public void OpenSteamWarning()
    {
        if (steamWarning)
            return;

        steamWarning = true;

        if (steamWarningClip != null && !steamWarningSource.isPlaying)
            steamWarningSource.Play();

        PlaySteamWarningParticles();
    }

    public void CloseSteamWarning()
    {
        steamWarning = false;
        steamWarningSource.Stop();
        StopSteamWarningParticles();
    }

    private void PlaySteamWarningParticles()
    {
        foreach (ParticleSystem warningParticle in steamWarningParticles)
        {
            if (warningParticle == null)
                continue;

            warningParticle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            warningParticle.Play(true);
        }
    }

    private void StopSteamWarningParticles()
    {
        foreach (ParticleSystem warningParticle in steamWarningParticles)
        {
            if (warningParticle != null)
                warningParticle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }

    private void StopBoat()
    {
        if (wasMoving)
            PlayEngineEffect(engineStopClip);

        wasMoving = false;
        SteerInput = 0f;
        ThrottleInput = 0f;
        Rigidbody.linearVelocity = Vector3.zero;
        Rigidbody.angularVelocity = Vector3.zero;

        if (ParticleSystem != null)
            ParticleSystem.Pause();

        idleSource.volume = 0.1f;
        movingSource.volume = 0f;

        if (engienManager != null)
            engienManager.SetBoatMoving(false);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player")) return;
        if (Time.time - lastImpactTime < impactCooldown) return;

        float force = collision.relativeVelocity.magnitude;
        if (force < impactForceThreshold) return;

        lastImpactTime = Time.time;
        Rigidbody.AddForce(collision.GetContact(0).normal * impactPushbackForce, ForceMode.Impulse);

        if (impactSounds.Length > 0)
            impactSource.PlayOneShot(impactSounds[Random.Range(0, impactSounds.Length)]);

        if (impactCamera != null)
        {
            float scaledStrength = impactShakeStrength * Mathf.Clamp01(force / (impactForceThreshold * 3f));
            impactCamera.transform.DOShakeRotation(impactShakeDuration, scaledStrength, 8, 60);
        }
    }

    private void ReadKeyboardInput()
    {
        float steer = 0f;
        if (Input.GetKey(KeyCode.A)) steer = 1f;
        if (Input.GetKey(KeyCode.D)) steer = -1f;

        float throttle = 0f;
        if (Input.GetKey(KeyCode.W)) throttle = 1f;
        if (Input.GetKey(KeyCode.S)) throttle = -1f;

        SteerInput = steer;
        ThrottleInput = throttle;
    }

    // ==================== NEW: UI FADE FUNCTIONS ====================
    private void SetBoatStateUI(BoatState newState)
    {
        if (currentBoatState == newState)
            return;

        currentBoatState = newState;
        stateFadeTween?.Kill();

        if (stateCanvasGroup == null)
            return;

        // Boat is stable: hide the UI only.
        if (newState == BoatState.Normal)
        {
            stateFadeTween = stateCanvasGroup.DOFade(0f, stateFadeDuration);
            return;
        }

        string newText = newState switch
        {
            BoatState.EngineFailed => engineFailedStateText,
            BoatState.SteamWarning => steamWarningStateText,
            BoatState.EngineFireAndSteam => engineFireAndSteamStateText,
            _ => string.Empty
        };

        if (stateText == null)
            return;

        // Warning state: fade out old text, change it, then fade in.
        stateFadeTween = DOTween.Sequence()
            .Append(stateCanvasGroup.DOFade(0f, stateFadeDuration))
            .AppendCallback(() => stateText.text = newText)
            .Append(stateCanvasGroup.DOFade(1f, stateFadeDuration));
    }
    // ================== END NEW: UI FADE FUNCTIONS ==================
}