using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Goes on the same GameObject as your BaseInteractable (the steering wheel).
/// The first Interact starts the minigame via MinigameSwitcher (which locks the
/// player and swaps the camera). From then on MinigameSwitcher itself watches for
/// the next Interact press to end it. This script listens to
/// onMinigameStart/onMinigameEnd and drives the boat's SteerInput/ThrottleInput
/// from the shared Move action while driving, and smoothly rotates the wheel
/// mesh on its Y axis to visually match the steering input.
/// </summary>
[RequireComponent(typeof(BaseInteractable))]
public class SteeringWheelInteractable : MonoBehaviour
{
    [SerializeField] private WaterBoat boat;
    [SerializeField] private InputActionReference moveAction;

    [Header("Wheel Visual Rotation")]
    [Tooltip("The transform that actually spins (can be this object or a child mesh).")]
    [SerializeField] private Transform wheelTransform;
    [Tooltip("Max rotation angle in degrees when steering fully left/right.")]
    [SerializeField] private float maxWheelAngle = 90f;
    [Tooltip("How quickly the wheel rotates toward its target angle (higher = snappier).")]
    [SerializeField] private float rotationSmoothSpeed = 6f;

    private bool isDriving;
    private float currentWheelAngle;
    private float wheelAngleVelocity; // used by SmoothDamp

    private void Awake()
    {
        var baseInteractable = GetComponent<BaseInteractable>();

        if (wheelTransform == null)
            wheelTransform = transform;
    }

    public void StartDriving()
    {
        isDriving = true;
    }

    public void StopDriving()
    {
        isDriving = false;
    }

    private void Update()
    {
        if (!isDriving || boat == null || moveAction == null)
        {
            SmoothWheelVisual(0f);
            return;
        }

        Vector2 input = moveAction.action.ReadValue<Vector2>();

        var reverse = input.y < 0f ? -1f : 1f;

        boat.SteerInput = -input.x * reverse;
        boat.ThrottleInput = input.y;

        SmoothWheelVisual(input.x * reverse);
    }
    /// <summary>
    /// Smoothly rotates the wheel mesh toward a target angle based on steer input,
    /// so it doesn't snap instantly like a real wheel wouldn't.
    /// </summary>
    private void SmoothWheelVisual(float steerInput)
    {
        float targetAngle = steerInput * maxWheelAngle;

        currentWheelAngle = Mathf.SmoothDamp(
            currentWheelAngle,
            targetAngle,
            ref wheelAngleVelocity,
            1f / rotationSmoothSpeed
        );

        if (wheelTransform != null)
        {
            Vector3 euler = wheelTransform.localEulerAngles;
            wheelTransform.localRotation = Quaternion.Euler(euler.x, currentWheelAngle, euler.z);
        }
    }
}
