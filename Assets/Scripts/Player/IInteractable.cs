using UnityEngine;

public interface IInteractable
{
    void OnItemEnter(GameObject player);
    void Interact(GameObject player);
    void OnItemExit(GameObject player);
}

public interface IHeld
{
    void OnItemPickup(GameObject player);
    void OnItemUse(GameObject player);
    void OnItemRelease(GameObject player);
    void OnItemDrop(GameObject player);
}
