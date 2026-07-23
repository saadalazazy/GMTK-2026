using UnityEngine;

public interface IInteractable
{
    void OnItemEnter(GameObject player);
    void Interact(GameObject player);
    void OnItemExit(GameObject player);
}
