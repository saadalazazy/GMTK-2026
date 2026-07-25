using System.Collections.Generic;
using UnityEngine;

public class ShipRadar : MonoBehaviour
{
    [Header("Radar Settings")]
    [SerializeField] private float radarRange = 500f;
    [SerializeField] private float radarScreenRadius = 150f;
    [SerializeField] private LayerMask detectableLayers;

    [Header("UI References")]
    [SerializeField] private RectTransform radarSweepLine;
    [SerializeField] private RectTransform blipContainer;
    [SerializeField] private float sweepSpeed = 120f;

    [Header("Blip Prefabs")]
    [SerializeField] private GameObject defaultBlipPrefab;
    [SerializeField] private LayerPrefab[] layerPrefabs;

    [Header("Blip Fade")]
    [SerializeField] private float blipFadeOutSpeed = 0.5f;
    [SerializeField] private float sweepDetectAngle = 4f;

    private List<RadarBlip> activeBlips = new List<RadarBlip>();
    private float currentSweepAngle = 0f;

    [System.Serializable]
    private struct LayerPrefab
    {
        public LayerMask layer;
        public GameObject prefab;
    }

    private class RadarBlip
    {
        public Transform worldTarget;
        public RectTransform uiIcon;
        public CanvasGroup canvasGroup;
    }

    private void Update()
    {
        RotateSweepLine();
        ScanSurroundings();
        UpdateBlips();
    }

    private void RotateSweepLine()
    {
        if (radarSweepLine == null) return;
        currentSweepAngle -= sweepSpeed * Time.deltaTime;
        currentSweepAngle %= 360f;
        if (currentSweepAngle < 0f) currentSweepAngle += 360f;
        radarSweepLine.localRotation = Quaternion.Euler(0f, 0f, currentSweepAngle);
    }

    private GameObject GetBlipPrefab(GameObject target)
    {
        int targetLayer = 1 << target.layer;
        foreach (var entry in layerPrefabs)
        {
            if ((entry.layer.value & targetLayer) != 0 && entry.prefab != null)
                return entry.prefab;
        }
        return defaultBlipPrefab;
    }

    private void ScanSurroundings()
    {
        Collider[] targets = Physics.OverlapSphere(transform.position, radarRange, detectableLayers);
        activeBlips.RemoveAll(b => b.worldTarget == null);

        foreach (var col in targets)
        {
            if (col.transform == this.transform) continue;
            if (activeBlips.Exists(b => b.worldTarget == col.transform)) continue;

            GameObject prefab = GetBlipPrefab(col.gameObject);
            if (prefab == null) continue;

            GameObject newIcon = Instantiate(prefab, blipContainer);
            CanvasGroup cg = newIcon.GetComponent<CanvasGroup>();
            if (cg == null) cg = newIcon.AddComponent<CanvasGroup>();
            cg.alpha = 0f;

            activeBlips.Add(new RadarBlip
            {
                worldTarget = col.transform,
                uiIcon = newIcon.GetComponent<RectTransform>(),
                canvasGroup = cg
            });
        }
    }

    private void UpdateBlips()
    {
        foreach (var blip in activeBlips)
        {
            Vector3 relativePos = transform.InverseTransformPoint(blip.worldTarget.position);
            Vector2 radarPos = new Vector2(relativePos.x, relativePos.z);
            float normalizedDistance = radarPos.magnitude / radarRange;

            if (normalizedDistance > 1.0f)
            {
                blip.uiIcon.gameObject.SetActive(false);
                continue;
            }

            blip.uiIcon.gameObject.SetActive(true);
            blip.uiIcon.anchorMin = new Vector2(0.5f, 0.5f);
            blip.uiIcon.anchorMax = new Vector2(0.5f, 0.5f);

            float blipAngle = Mathf.Atan2(radarPos.x, radarPos.y) * Mathf.Rad2Deg;
            float sweepAngle = (360f - currentSweepAngle) % 360f;
            float angleDiff = Mathf.Abs(Mathf.DeltaAngle(sweepAngle, blipAngle));

            if (angleDiff <= sweepDetectAngle)
            {
                blip.uiIcon.anchoredPosition = radarPos.normalized * (normalizedDistance * radarScreenRadius);
                blip.canvasGroup.alpha = 1f;
            }
            else
            {
                blip.canvasGroup.alpha = Mathf.MoveTowards(blip.canvasGroup.alpha, 0f, blipFadeOutSpeed * Time.deltaTime);
            }
        }
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, radarRange);
    }
}
