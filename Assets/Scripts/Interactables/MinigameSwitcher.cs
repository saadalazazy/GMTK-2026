using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class MinigameSwitcher : MonoBehaviour
{
    [SerializeField] private CinemachineCamera targetCamera;

    public UnityEvent onMinigameStart;
    public UnityEvent onMinigameEnd;

    public InputActionAsset actions;

    private bool isInMinigame = false;
    InputAction interactAction;


    void Start()
    {
        var gameplay = actions.FindActionMap("Player");
        interactAction = gameplay.FindAction("Interact");
    }

    public void Update()
    {
        if (isInMinigame && interactAction.WasPressedThisFrame())
        {
            ToggleMinigame();
        }
    }

    public void ToggleMinigame()
    {
        isInMinigame = !isInMinigame;

        if (isInMinigame)
        {
            // Find player core and disable it
            PlayerCore playerCore = FindFirstObjectByType<PlayerCore>();
            if (playerCore != null)
            {
                playerCore.enabled = false;
            }

            playerCore.transform.GetComponent<InteractionManager>().enabled = false;

            targetCamera.Priority = 20;
        }
        else
        {
            // Find player core and enable it
            PlayerCore playerCore = FindFirstObjectByType<PlayerCore>();
            if (playerCore != null)
            {
                playerCore.enabled = true;
            }
            playerCore.transform.GetComponent<InteractionManager>().enabled = true;


            targetCamera.Priority = -1;
        }

        if (isInMinigame)
            onMinigameStart.Invoke();
        else
            onMinigameEnd.Invoke();
    }
}
