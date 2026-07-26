using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class ValveHandwheel : MonoBehaviour
{
    [Header("Valve Settings")]
    [SerializeField] private float currentSealedPercentage = 0f; // 0 to 100%
    [SerializeField] private float turnSpeed = 25f;
    [SerializeField] private float backpressureForce = 15f; // Pushes wheel back open

    [Header("Pressure Surge Mechanics")]
    [SerializeField] private bool isSurging = false;
    [SerializeField] private float surgeTimer = 0f;

    [Header("Camera Shake")]
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private float shakeDuration = 0.5f;
    [SerializeField] private float shakeStrength = 0.3f;

    [Header("Rotation Smoothing")]
    [SerializeField] private float rotationSmoothing = 10f;

    [Header("Valve UI")]
    [SerializeField] private Image actionIcon;
    [SerializeField] private TMP_Text actionText;
    [SerializeField] private Transform uiPulseTarget;

    [Header("Normal Hold UI")]
    [SerializeField] private Sprite normalIcon;
    [SerializeField] private string normalText = "Hold to turn valve";

    [Header("Surge Spam UI")]
    [SerializeField] private Sprite surgeIcon;
    [SerializeField] private string surgeText = "SPAM to hold the valve!";

    [Header("Sealed UI")]
    [SerializeField] private Sprite sealedIcon;
    [SerializeField] private string sealedText = "Valve sealed!";

    [Header("Spam UI Pulse")]
    [SerializeField] private float pulseSmallScale = 0.85f;
    [SerializeField] private float pulseLargeScale = 1.15f;
    [SerializeField] private float pulseDuration = 0.08f;

    [Header("Canvas Fade")]
    [SerializeField] private CanvasGroup uiCanvasGroup;
    [SerializeField] private float fadeDuration = 0.2f;

    [Header("Events")]
    public UnityEvent onSealed;

    public InputActionAsset actions;
    InputAction rotateAction;

    float targetRotation;
    float surgeStartPercentage;
    bool hasSealed;

    // UI-only variables
    private Vector3 uiDefaultScale;
    private Tween uiPulseTween;

    private void Awake()
    {
        if (uiPulseTarget != null)
            uiDefaultScale = uiPulseTarget.localScale;
    }
    public void ShowValveUi()
    {
        if (uiCanvasGroup == null) return;

        uiCanvasGroup.DOKill();
        uiCanvasGroup.DOFade(1f, fadeDuration);
        uiCanvasGroup.blocksRaycasts = true;
    }

    public void HideValveUi()
    {
        if (uiCanvasGroup == null) return;

        uiCanvasGroup.DOKill();
        uiCanvasGroup.DOFade(0f, fadeDuration);
        uiCanvasGroup.blocksRaycasts = false;
    }
    void OnEnable()
    {
        var gameplay = actions.FindActionMap("Player");
        rotateAction = gameplay.FindAction("Attack");

        SetValveUi(normalIcon, normalText);
        ShowValveUi();
    }

    void OnDisable()
    {
        currentSealedPercentage = 0f;

        uiPulseTween?.Kill();

        if (uiPulseTarget != null)
            uiPulseTarget.localScale = uiDefaultScale;

        if (uiCanvasGroup != null)
            uiCanvasGroup.alpha = 0f;
    }

    private void Update()
    {
        // Randomly trigger steam surges that push back harder
        surgeTimer -= Time.deltaTime;
        if (surgeTimer <= 0)
        {
            isSurging = !isSurging;
            surgeTimer = Random.Range(1.5f, 4f);

            if (isSurging)
            {
                surgeStartPercentage = currentSealedPercentage;

                // UI addition: switch to spam instruction.
                SetValveUi(surgeIcon, surgeText);

                if (cameraTransform != null)
                    cameraTransform.DOShakeRotation(surgeTimer, shakeStrength, 10, 90, true)
                        .SetLoops(-1, LoopType.Restart);
            }
            else
            {
                // UI addition: return to normal instruction.
                SetValveUi(normalIcon, normalText);

                if (cameraTransform != null)
                    cameraTransform.DOKill();
            }
        }

        // Apply constant backpressure pushing the valve open
        float currentResistance = backpressureForce * (isSurging ? 2.5f : 1.0f);
        currentSealedPercentage -= currentResistance * Time.deltaTime;
        currentSealedPercentage = Mathf.Clamp(currentSealedPercentage, 0f, 100f);

        if (isSurging)
        {
            // During surge: spam to hold ground, don't gain progress
            if (rotateAction.WasPressedThisFrame())
            {
                currentSealedPercentage += 5f;

                // UI addition: pulse when the player spams the button.
                PulseSpamUi();
            }

            currentSealedPercentage = Mathf.Min(currentSealedPercentage, surgeStartPercentage);
        }
        else
        {
            // Normal mode: hold to gain progress
            if (rotateAction.IsPressed())
                currentSealedPercentage += turnSpeed * Time.deltaTime;
        }

        currentSealedPercentage = Mathf.Clamp(currentSealedPercentage, 0f, 100f);

        if (currentSealedPercentage >= 100f && !hasSealed)
        {
            hasSealed = true;

            // UI addition: show completed state.
            SetValveUi(sealedIcon, sealedText);
            onSealed?.Invoke();
        }
        else if (currentSealedPercentage < 100f)
        {
            hasSealed = false;
        }

        // Smooth visual rotation
        targetRotation = currentSealedPercentage * 3.6f;
        float currentY = transform.localEulerAngles.y;
        float smoothedY = Mathf.LerpAngle(currentY, targetRotation, rotationSmoothing * Time.deltaTime);
        transform.localRotation = Quaternion.Euler(0, smoothedY, 0);
    }

    // ---------------- UI-ONLY METHODS ----------------

    private void SetValveUi(Sprite icon, string message)
    {
        if (actionIcon != null)
            actionIcon.sprite = icon;

        if (actionText != null)
            actionText.text = message;
    }

    private void PulseSpamUi()
    {
        if (uiPulseTarget == null)
            return;

        uiPulseTween?.Kill();

        uiPulseTarget.localScale = uiDefaultScale;
        uiPulseTween = uiPulseTarget
            .DOScale(uiDefaultScale * pulseSmallScale, pulseDuration)
            .SetEase(Ease.OutQuad)
            .OnComplete(() =>
            {
                uiPulseTween = uiPulseTarget
                    .DOScale(uiDefaultScale * pulseLargeScale, pulseDuration)
                    .SetEase(Ease.OutBack)
                    .OnComplete(() =>
                    {
                        uiPulseTween = uiPulseTarget
                            .DOScale(uiDefaultScale, pulseDuration)
                            .SetEase(Ease.OutQuad);
                    });
            });
    }
}