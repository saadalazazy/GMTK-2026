using UnityEngine;

public class Lighthouse : MonoBehaviour
{
    [Header("Position")]
    public float zOffset = 20f;

    [Header("Flash")]
    public Renderer lighthouseRenderer;
    public float flashMinInterval = 3f;
    public float flashMaxInterval = 6f;
    public float flashDuration = 0.8f;
    public Color flashColor = new Color(1f, 0.9f, 0.3f);
    public float emissionIntensity = 2f;

    [Header("Fade")]
    public float fadeDuration = 30f;
    public Color fadeColor = new Color(0.5f, 0f, 0f);

    [SerializeField] private Transform target;

    private float lockedX;
    private float lockedY;
    private bool hasArrived;
    private float nextFlashTime;
    private float flashTimer;
    private bool isFlashing;
    private float fadeTimer;
    private bool hasFaded;
    private MaterialPropertyBlock propBlock;

    void Start()
    {
        lockedX = transform.position.x;
        lockedY = transform.position.y;
        propBlock = new MaterialPropertyBlock();
        nextFlashTime = Random.Range(flashMinInterval, flashMaxInterval);
        fadeTimer = 0f;
    }

    void Update()
    {
        HandleMovement();
        HandleFlash();
    }

    void HandleMovement()
    {
        if (hasArrived) return;

        float targetZ = target.position.z + zOffset;

        transform.position = new Vector3(lockedX, lockedY, targetZ);
    }

    public void Arrive()
    {
        hasArrived = true;
    }

    void HandleFlash()
    {
        if (lighthouseRenderer == null) return;

        if (hasFaded) return;

        fadeTimer += Time.deltaTime;
        float fadeT = Mathf.Clamp01(fadeTimer / fadeDuration);
        Color currentColor = Color.Lerp(flashColor, fadeColor, fadeT);
        float currentIntensity = Mathf.Lerp(emissionIntensity, 0f, fadeT);

        if (fadeT >= 1f)
        {
            hasFaded = true;
            lighthouseRenderer.GetPropertyBlock(propBlock);
            propBlock.SetColor("_EmissionColor", Color.black);
            lighthouseRenderer.SetPropertyBlock(propBlock);
            return;
        }

        float emission = 0f;

        if (isFlashing)
        {
            flashTimer -= Time.deltaTime;
            float t = 1f - (flashTimer / flashDuration);
            emission = Mathf.Sin(t * Mathf.PI) * currentIntensity;

            if (flashTimer <= 0f)
            {
                isFlashing = false;
                nextFlashTime = Random.Range(flashMinInterval, flashMaxInterval);
            }
        }
        else
        {
            nextFlashTime -= Time.deltaTime;
            if (nextFlashTime <= 0f)
            {
                isFlashing = true;
                flashTimer = flashDuration;
            }
        }

        lighthouseRenderer.GetPropertyBlock(propBlock);
        propBlock.SetColor("_EmissionColor", currentColor * emission);
        lighthouseRenderer.SetPropertyBlock(propBlock);
    }
}
