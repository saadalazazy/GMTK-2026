using UnityEngine;

public class Ladder : MonoBehaviour
{
    [SerializeField] private Transform topPoint;
    [SerializeField] private Transform bottomPoint;

    public Vector3 GetPositionAtProgress(float t)
    {
        return Vector3.Lerp(bottomPoint.position, topPoint.position, t);
    }

    public Vector3 GetDirection()
    {
        return (topPoint.position - bottomPoint.position).normalized;
    }

    public float GetLength()
    {
        return Vector3.Distance(bottomPoint.position, topPoint.position);
    }

    void OnDrawGizmos()
    {
        if (topPoint == null || bottomPoint == null) return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(bottomPoint.position, topPoint.position);
        Gizmos.DrawSphere(topPoint.position, 0.2f);
        Gizmos.DrawSphere(bottomPoint.position, 0.2f);
    }
}
