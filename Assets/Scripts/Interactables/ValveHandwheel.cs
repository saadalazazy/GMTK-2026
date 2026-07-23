using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

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

    [Header("Events")]
    public UnityEvent onSealed;

    public InputActionAsset actions;
    InputAction rotateAction;

    float targetRotation;
    float surgeStartPercentage;
    bool hasSealed;

    void OnEnable()
    {
        var gameplay = actions.FindActionMap("Player");
        rotateAction = gameplay.FindAction("Attack");
    }

    void OnDisable()
    {
        currentSealedPercentage = 0f;
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
                if (cameraTransform != null)
                    cameraTransform.DOShakeRotation(surgeTimer, shakeStrength, 10, 90, true).SetLoops(-1, LoopType.Restart);
            }
            else
            {
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
                currentSealedPercentage += 5f;

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
}
