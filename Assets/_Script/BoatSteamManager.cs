using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class BoatSteamManager : MonoBehaviour
{
    [Header("Lamp")]
    [SerializeField] private Renderer lampRenderer;
    [SerializeField] private Light warningLight;
    [SerializeField] private Color warningColor = Color.red;
    [SerializeField] private float emissionIntensity = 3f;

    [Header("Steam Timing")]
    [SerializeField] private float minTimeToCritical = 10f;
    [SerializeField] private float maxTimeToCritical = 25f;
    [SerializeField] private bool startOnGameStart = true;
    [SerializeField] private bool repeatSystem;
    [SerializeField] private float delayBeforeRepeat = 2f;

    [Header("Blink Speed")]
    [SerializeField] private float slowBlinkInterval = 0.8f;
    [SerializeField] private float fastBlinkInterval = 0.08f;

    [Header("Warning Sound")]
    [SerializeField] private AudioSource warningAudio;
    [SerializeField] private AudioClip warningBeep;
    [Tooltip("The beep starts when this many seconds remain.")]
    [SerializeField] private float beepStartsAtRemainingSeconds = 10f;
    [SerializeField] private float slowBeepInterval = 0.8f;
    [SerializeField] private float fastBeepInterval = 0.12f;

    [Header("Events")]
    [SerializeField] private UnityEvent onSteamReachedCritical;

    public float SteamProgress { get; private set; }
    public bool IsRunning => isRunning;

    private Material lampMaterial;
    private Coroutine steamRoutine;
    private bool lampIsOn;
    private bool isRunning;

    private void Awake()
    {
        if (lampRenderer != null)
        {
            lampMaterial = lampRenderer.material;

            if (lampMaterial.HasProperty("_EmissionColor"))
                lampMaterial.EnableKeyword("_EMISSION");
        }

        SetLamp(false);
    }

    private void Start()
    {
        if (startOnGameStart)
            StartSteamSystem();
    }

    public void StartSteamSystem()
    {
        StopSteamSystem();

        isRunning = true;
        steamRoutine = StartCoroutine(SteamRoutine());
    }

    public void StopSteamSystem()
    {
        isRunning = false;

        if (steamRoutine != null)
            StopCoroutine(steamRoutine);

        steamRoutine = null;
        SteamProgress = 0f;

        if (warningAudio != null)
            warningAudio.Stop();

        SetLamp(false);
    }

    private IEnumerator SteamRoutine()
    {
        do
        {
            float minTime = Mathf.Min(minTimeToCritical, maxTimeToCritical);
            float maxTime = Mathf.Max(minTimeToCritical, maxTimeToCritical);
            float finalTime = Random.Range(minTime, maxTime);

            float elapsed = 0f;
            float blinkTimer = 0f;
            float beepTimer = 0f;

            SteamProgress = 0f;
            SetLamp(false);

            while (elapsed < finalTime && isRunning)
            {
                elapsed += Time.deltaTime;
                SteamProgress = Mathf.Clamp01(elapsed / finalTime);

                // Red lamp blinks faster as danger increases.
                float blinkInterval = Mathf.Lerp(
                    slowBlinkInterval,
                    fastBlinkInterval,
                    SteamProgress
                );

                blinkTimer += Time.deltaTime;

                if (blinkTimer >= blinkInterval)
                {
                    blinkTimer = 0f;
                    SetLamp(!lampIsOn);
                }

                // Beep only in the final X seconds.
                float remainingTime = finalTime - elapsed;

                if (remainingTime <= beepStartsAtRemainingSeconds)
                {
                    float danger01 = 1f - Mathf.Clamp01(
                        remainingTime / beepStartsAtRemainingSeconds
                    );

                    float beepInterval = Mathf.Lerp(
                        slowBeepInterval,
                        fastBeepInterval,
                        danger01
                    );

                    beepTimer += Time.deltaTime;

                    if (beepTimer >= beepInterval)
                    {
                        beepTimer = 0f;

                        if (warningAudio != null && warningBeep != null)
                            warningAudio.PlayOneShot(warningBeep);
                    }
                }

                yield return null;
            }

            if (!isRunning)
                yield break;

            SteamProgress = 1f;
            SetLamp(true);

            onSteamReachedCritical?.Invoke();

            if (!repeatSystem || !isRunning)
                break;

            yield return new WaitForSeconds(delayBeforeRepeat);
        }
        while (repeatSystem && isRunning);

        steamRoutine = null;
    }

    private void SetLamp(bool isOn)
    {
        lampIsOn = isOn;

        if (warningLight != null)
        {
            warningLight.color = warningColor;
            warningLight.enabled = isOn;
        }

        if (lampMaterial != null && lampMaterial.HasProperty("_EmissionColor"))
        {
            Color emission = isOn
                ? warningColor * emissionIntensity
                : Color.black;

            lampMaterial.SetColor("_EmissionColor", emission);
        }
    }
    public void RepetSteamSystem()
    {
        StopSteamSystem();
        StartSteamSystem();
    }
    private void OnDisable()
    {
        StopSteamSystem();
    }
}