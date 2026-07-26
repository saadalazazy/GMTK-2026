using UnityEngine;

public class RuntimeParent : MonoBehaviour
{
    [SerializeField] private Transform child;
    [SerializeField] private Transform parent;
    [SerializeField] private bool matchPosition = true;
    [SerializeField] private bool matchRotation = true;

    void Start()
    {
        if (child == null || parent == null) return;
        child.SetParent(parent, matchPosition && matchRotation);
        if (matchPosition) child.localPosition = Vector3.zero;
        if (matchRotation) child.localRotation = Quaternion.identity;
    }
}
