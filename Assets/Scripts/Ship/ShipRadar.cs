using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class ShipRadar : MonoBehaviour
{
    [Header("Radar Settings")]
    [SerializeField] private float radarRange = 500f; // Max distance in world units
    [SerializeField] private float radarScreenRadius = 150f; // UI radius in pixels
    [SerializeField] private LayerMask detectableLayers;

    [Header("UI References")]
    [SerializeField] private RectTransform radarSweepLine;
    [SerializeField] private RectTransform blipContainer;
    [SerializeField] private GameObject blipPrefab;
    [SerializeField] private float sweepSpeed = 120f; // Degrees per second

    private List<RadarBlip> activeBlips = new List<RadarBlip>();
    private float currentSweepAngle = 0f;

    private class RadarBlip
    {
        public Transform worldTarget;
        public RectTransform uiIcon;
    }

    private void Update()
    {
        RotateSweepLine();
        ScanSurroundings();
        UpdateBlipPositions();
    }

    private void RotateSweepLine()
    {
        if (radarSweepLine == null) return;
        currentSweepAngle -= sweepSpeed * Time.deltaTime;
        currentSweepAngle %= 360f;
        radarSweepLine.localRotation = Quaternion.Euler(0f, 0f, currentSweepAngle);
    }

    private void ScanSurroundings()
    {
        // Detect targets in sphere around boat
        Collider[] targets = Physics.OverlapSphere(transform.position, radarRange, detectableLayers);

        // Remove stale/destroyed targets
        activeBlips.RemoveAll(b => b.worldTarget == null);

        foreach (var col in targets)
        {
            if (col.transform == this.transform) continue;

            // Register new targets if not already tracked
            if (!activeBlips.Exists(b => b.worldTarget == col.transform))
            {
                GameObject newIcon = Instantiate(blipPrefab, blipContainer);
                activeBlips.Add(new RadarBlip { worldTarget = col.transform, uiIcon = newIcon.GetComponent<RectTransform>() });
            }
        }
    }

    private void UpdateBlipPositions()
    {
        foreach (var blip in activeBlips)
        {
            // Convert world vector to relative boat coordinates
            Vector3 relativePos = transform.InverseTransformPoint(blip.worldTarget.position);

            // Map 3D position (X, Z) to 2D radar plane (X, Y)
            Vector2 radarPos = new Vector2(relativePos.x, relativePos.z);

            // Scale to radar UI size
            float normalizedDistance = radarPos.magnitude / radarRange;

            if (normalizedDistance <= 1.0f)
            {
                blip.uiIcon.gameObject.SetActive(true);
                blip.uiIcon.anchorMin = new Vector2(0.5f, 0.5f);
                blip.uiIcon.anchorMax = new Vector2(0.5f, 0.5f);
                blip.uiIcon.anchoredPosition = radarPos.normalized * (normalizedDistance * radarScreenRadius);
            }
            else
            {
                blip.uiIcon.gameObject.SetActive(false); // Out of range
            }
        }
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, radarRange);
    }


}
