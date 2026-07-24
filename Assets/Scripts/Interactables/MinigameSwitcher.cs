using System.Collections;
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
        isInMinigame = false;
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
        StartCoroutine(ToggleMinigameCoroutine());
    }

    private IEnumerator ToggleMinigameCoroutine()
    {
        isInMinigame = !isInMinigame;
        print("Minigame state changed: " + isInMinigame);

        yield return null;
        if (isInMinigame)
        {
            PlayerCore playerCore = FindFirstObjectByType<PlayerCore>();
            playerCore.enabled = false;
            playerCore.GetComponent<InteractionManager>().enabled = false;

            targetCamera.Priority = 20;
        }
        else
        {
            PlayerCore playerCore = FindFirstObjectByType<PlayerCore>();
            playerCore.enabled = true;
            playerCore.GetComponent<InteractionManager>().enabled = true;

            targetCamera.Priority = -1;
        }

        if (isInMinigame)
            onMinigameStart.Invoke();
        else
            onMinigameEnd.Invoke();

    }
}
