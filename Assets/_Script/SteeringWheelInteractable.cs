using UnityEngine;
using UnityEngine.InputSystem;
using DG.Tweening;

[RequireComponent(typeof(BaseInteractable))]
public class SteeringWheelInteractable : MonoBehaviour
{
    [SerializeField] private WaterBoat boat;
    [SerializeField] private InputActionReference moveAction;

    [Header("Wheel Visual Rotation")]
    [SerializeField] private Transform wheelTransform;
    [SerializeField] private Transform forwardHandle;
    [SerializeField] private Transform backwardHandle;
    [SerializeField] private float maxWheelAngle = 90f;
    [SerializeField] private float wheelTurnDuration = 0.18f;
    [SerializeField] private float handleTurnDuration = 0.15f;

    private bool isDriving;

    private Quaternion wheelStartRotation;
    private Quaternion forwardHandleStartRotation;
    private Quaternion backwardHandleStartRotation;

    private Tween wheelTween;
    private Tween forwardHandleTween;
    private Tween backwardHandleTween;

    private float lastSteerInput;
    private float lastThrottleInput;

    private void Awake()
    {
        if (wheelTransform == null)
            wheelTransform = transform;

        wheelStartRotation = wheelTransform.localRotation;

        if (forwardHandle != null)
            forwardHandleStartRotation = forwardHandle.localRotation;

        if (backwardHandle != null)
            backwardHandleStartRotation = backwardHandle.localRotation;
    }

    public void StartDriving()
    {
        isDriving = true;
    }

    public void StopDriving()
    {
        isDriving = false;

        if (boat != null)
        {
            boat.SteerInput = 0f;
            boat.ThrottleInput = 0f;
        }

        AnimateWheel(0f);
        AnimateHandles(0f);
    }

    private void Update()
    {
        if (!isDriving || boat == null || moveAction == null)
            return;

        Vector2 input = moveAction.action.ReadValue<Vector2>();

        boat.SteerInput = -input.x;
        boat.ThrottleInput = input.y;

        // Only start a new tween when input changes noticeably.
        if (Mathf.Abs(input.x - lastSteerInput) > 0.01f)
        {
            AnimateWheel(input.x);
            lastSteerInput = input.x;
        }

        if (Mathf.Abs(input.y - lastThrottleInput) > 0.01f)
        {
            AnimateHandles(input.y);
            lastThrottleInput = input.y;
        }
    }

    private void AnimateWheel(float steerInput)
    {
        if (wheelTransform == null)
            return;

        wheelTween?.Kill();

        float targetAngle = steerInput * maxWheelAngle;

        Quaternion targetRotation =
            wheelStartRotation * Quaternion.AngleAxis(-targetAngle, Vector3.right);

        wheelTween = wheelTransform
            .DOLocalRotateQuaternion(targetRotation, wheelTurnDuration)
            .SetEase(Ease.OutCubic);
    }

    private void AnimateHandles(float throttleInput)
    {
        const float handleAngle = 45f;

        if (forwardHandle != null)
        {
            forwardHandleTween?.Kill();

            float forwardAngle = Mathf.Max(0f, throttleInput) * handleAngle;
            Quaternion target = forwardHandleStartRotation *
                                Quaternion.AngleAxis(forwardAngle, Vector3.up);

            forwardHandleTween = forwardHandle
                .DOLocalRotateQuaternion(target, handleTurnDuration)
                .SetEase(Ease.OutBack);
        }

        if (backwardHandle != null)
        {
            backwardHandleTween?.Kill();

            float backwardAngle = Mathf.Min(0f, throttleInput) * handleAngle;
            Quaternion target = backwardHandleStartRotation *
                                Quaternion.AngleAxis(backwardAngle, Vector3.up);

            backwardHandleTween = backwardHandle
                .DOLocalRotateQuaternion(target, handleTurnDuration)
                .SetEase(Ease.OutBack);
        }
    }

    private void OnDestroy()
    {
        wheelTween?.Kill();
        forwardHandleTween?.Kill();
        backwardHandleTween?.Kill();
    }
}