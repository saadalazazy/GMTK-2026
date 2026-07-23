using UnityEngine;

public class WaterWaveManager : StaticInstance<WaterWaveManager>
{
    [Header("Wave Controls")]
    public float amplitude = 1f;
    public float length = 2f;
    public float speed = 1f;
    public float offset = 0f;

    protected override void Awake()
    {
        base.Awake();
    }

    private void Update()
    {
        offset += Time.deltaTime * speed;
    }

    public float GetWaveHeight(float _x)
    {
        return amplitude * Mathf.Sin(_x / length + offset);
    }
}