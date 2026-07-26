using UnityEngine;
public class Ladder : MonoBehaviour
{
    [SerializeField] private Transform topPoint;
    [SerializeField] private Transform bottomPoint;
    [SerializeField] private Vector3 playerOffset = new Vector3(0f, 0.5f);

    public Vector3 GetPositionAtProgress(float t)
    {
        return Vector3.Lerp(bottomPoint.position, topPoint.position, t);
    }

    public Vector3 GetClimbDirection()
    {
        return (topPoint.position - bottomPoint.position).normalized;
    }

    public Vector3 GetFacingDirection()
    {
        return -transform.forward;
    }

    public Vector3 GetOffsetPosition(float t)
    {
        Vector3 pos = GetPositionAtProgress(t);
        return pos + playerOffset;
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

        Gizmos.color = Color.cyan;
        Vector3 mid = GetPositionAtProgress(0.5f);
        Gizmos.DrawRay(mid, GetFacingDirection() * 1f);
    }
}
