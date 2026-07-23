using UnityEngine;
using UnityEngine.InputSystem;

public class InteractionManager : MonoBehaviour
{
    public float interactRange = 10f;
    public InputActionAsset actions;

    InputAction interactAction;
    InputAction useAction;
    IInteractable currentTarget;

    [SerializeField] private Transform heldItemAnchor;

    BaseHeldItem currentHeldItem;

    Camera mainCamera;

    void Start()
    {
        mainCamera = Camera.main;

        var gameplay = actions.FindActionMap("Player");
        interactAction = gameplay.FindAction("Interact");
        useAction = gameplay.FindAction("Attack");

    }

    void Update()
    {
        CheckForInteractable();

        if (interactAction.WasPressedThisFrame() && currentTarget != null)
            currentTarget.Interact(gameObject);

        if (useAction.IsPressed() && currentHeldItem != null)
            currentHeldItem.OnItemUse(gameObject);

        if (useAction.WasReleasedThisFrame() && currentHeldItem != null)
            currentHeldItem.OnItemRelease(gameObject);

    }

    public void HoldItem(BaseHeldItem item)
    {
        if (currentHeldItem != null)
            currentHeldItem.OnItemDrop(gameObject);

        currentHeldItem = item;

        currentHeldItem.transform.SetParent(heldItemAnchor);


        if (currentHeldItem != null)
            currentHeldItem.OnItemPickup(gameObject);

        print("Holding item: " + (currentHeldItem != null ? currentHeldItem.ToString() : "None"));
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
