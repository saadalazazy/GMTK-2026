using UnityEngine;
using UnityEngine.Events;

public class BaseInteractable : MonoBehaviour, IInteractable
{
    [SerializeField] private float scaleDuration = 0.2f;
    [SerializeField] private Outline outline;

    public UnityEvent<GameObject> onInteract;

    public void Interact(GameObject player)
    {
        onInteract.Invoke(player);
        OnItemExit(player);
    }

    public void OnItemEnter(GameObject player)
    {
        if (outline != null)
            outline.enabled = true;
    }

    public void OnItemExit(GameObject player)
    {
        if (outline != null)
            outline.enabled = false;
    }
}
