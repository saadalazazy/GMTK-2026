using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class CanvasAutoFade : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private float fadeDuration = 0.2f;
    [SerializeField] private float visibleTime = 10f;
    [SerializeField] private TextMeshProUGUI text;

    [Header("Input")]
    [SerializeField] private InputActionReference helpAction;

    private Tween fadeTween;
    private Tween autoHideTimer;
    private InputAction helpInput;
    private bool isShowing;
    private bool enabledInputHere;

    private void Awake()
    {
        if (canvasGroup != null)
            canvasGroup.alpha = 0f;

        isShowing = false;
    }

    private void OnEnable()
    {
        if (helpAction == null)
            return;

        helpInput = helpAction.action;
        helpInput.performed += OnHelpPressed;

        // Enables it only if another script has not already enabled it.
        if (!helpInput.enabled)
        {
            helpInput.Enable();
            enabledInputHere = true;
        }
    }

    private void OnDisable()
    {
        if (helpInput != null)
        {
            helpInput.performed -= OnHelpPressed;

            if (enabledInputHere)
                helpInput.Disable();
        }

        fadeTween?.Kill();
        autoHideTimer?.Kill();
    }

    private void OnHelpPressed(InputAction.CallbackContext context)
    {
        Toggle();
    }

    public void Toggle()
    {
        if (isShowing)
            Hide();
        else
            Show();
    }

    public void Show()
    {
        if (canvasGroup == null)
            return;

        fadeTween?.Kill();
        autoHideTimer?.Kill();

        isShowing = true;
        fadeTween = canvasGroup.DOFade(1f, fadeDuration);

        autoHideTimer = DOVirtual.DelayedCall(visibleTime, Hide);

        text.text = "i to hide help";
    }

    public void Hide()
    {
        if (canvasGroup == null)
            return;

        fadeTween?.Kill();
        autoHideTimer?.Kill();

        isShowing = false;
        fadeTween = canvasGroup.DOFade(0f, fadeDuration);
        text.text = "i to help";

    }
}