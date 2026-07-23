using UnityEngine;
using UnityEngine.InputSystem;

public class InteractionManager : MonoBehaviour
{
    public float interactRange = 10f;
    public InputActionAsset actions;

    InputAction interactAction;
    IInteractable currentTarget;
    Camera mainCamera;

    void Start()
    {
        mainCamera = Camera.main;

        var gameplay = actions.FindActionMap("Player");
        interactAction = gameplay.FindAction("Interact");
    }

    void Update()
    {
        CheckForInteractable();

        if (interactAction.WasPressedThisFrame() && currentTarget != null)
            currentTarget.Interact(gameObject);
    }

    void CheckForInteractable()
    {
        IInteractable detected = null;

        if (Physics.Raycast(mainCamera.transform.position, mainCamera.transform.forward, out RaycastHit hit, interactRange))
            detected = hit.collider.GetComponent<IInteractable>();

        if (detected != currentTarget)
        {
            if (currentTarget != null)
                currentTarget.OnItemExit(gameObject);

            currentTarget = detected;

            if (currentTarget != null)
                currentTarget.OnItemEnter(gameObject);
        }
    }
}
