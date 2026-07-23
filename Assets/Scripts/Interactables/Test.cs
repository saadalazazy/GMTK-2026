using DG.Tweening;
using UnityEngine;

public class Test : MonoBehaviour, IInteractable
{
    [SerializeField] private Transform interactCanvas;
    [SerializeField] private float scaleDuration = 0.2f;

    public void Interact(GameObject player)
    {
        Destroy(gameObject);
    }

    public void OnItemEnter(GameObject player)
    {
        interactCanvas.DOScale(Vector3.one * 0.01f, scaleDuration).SetEase(Ease.OutBack);
    }

    public void OnItemExit(GameObject player)
    {
        interactCanvas.DOScale(Vector3.zero, scaleDuration).SetEase(Ease.InBack);
    }
}
