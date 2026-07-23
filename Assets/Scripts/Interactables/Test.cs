using UnityEngine;
using UnityEngine.Events;

public class Test : MonoBehaviour, IInteractable
{
    [SerializeField] private float scaleDuration = 0.2f;

    public UnityEvent onInteract;

    public void Interact(GameObject player)
    {
        onInteract.Invoke();
    }

    public void OnItemEnter(GameObject player)
    {
    }

    public void OnItemExit(GameObject player)
    {
    }
}
