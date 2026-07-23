using UnityEngine;

public abstract class BaseHeldItem : MonoBehaviour, IHeld
{
    [SerializeField] private Vector3 heldPosition;
    [SerializeField] private Quaternion heldRotation;
    [SerializeField] private Vector3 heldScale;


    public void Start()
    {
        gameObject.TryGetComponent(out BaseInteractable interactable);
        if (interactable != null)
        {
            interactable.onInteract.AddListener((player) => Interact(player));
        }
    }

    public void Interact(GameObject player)
    {
        player.GetComponent<InteractionManager>().HoldItem(this);
    }

    public virtual void OnItemDrop(GameObject player)
    {

    }


    public virtual void OnItemPickup(GameObject player)
    {
        transform.localPosition = heldPosition;
        transform.localRotation = heldRotation;
        transform.localScale = heldScale;
    }

    public virtual void OnItemUse(GameObject player)
    {
        print("Using item: " + gameObject.name);
    }

    public virtual void OnItemRelease(GameObject player)
    {

    }


}
