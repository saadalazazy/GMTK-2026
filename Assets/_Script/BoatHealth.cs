using UnityEngine;
using UnityEngine.Events;

public class BoatHealth : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] private float maxHealth = 100f;

    [Header("Damage")]
    [SerializeField] private float obstacleDamage = 10f;
    [SerializeField] private float sharkDamage = 25f;
    [SerializeField] private float damageCooldown = 0.5f;

    [Header("Dissolve")]
    [SerializeField] private Renderer boatRenderer;
    [SerializeField] private int dissolveMaterialIndex = 1;
    [SerializeField] private float dissolveFull = 0.828f;
    [SerializeField] private float dissolveEmpty = 0.786f;

    [Header("Events")]
    public UnityEvent<float> OnDamageTaken;
    public UnityEvent OnDeath;

    private float currentHealth;
    private float lastDamageTime;
    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;
    public bool IsDead => currentHealth <= 0f;

    void Awake()
    {
        currentHealth = maxHealth;
        UpdateDissolve();
    }

    public void TakeDamage(float amount)
    {
        if (IsDead) return;
        if (Time.time - lastDamageTime < damageCooldown) return;

        lastDamageTime = Time.time;
        currentHealth = Mathf.Max(0f, currentHealth - amount);
        UpdateDissolve();
        OnDamageTaken?.Invoke(currentHealth);

        if (currentHealth <= 0f)
            OnDeath?.Invoke();
    }

    void UpdateDissolve()
    {
        if (boatRenderer == null) return;
        float t = currentHealth / maxHealth;
        float value = Mathf.Lerp(dissolveEmpty, dissolveFull, t);
        Material[] materials = boatRenderer.materials;
        if (dissolveMaterialIndex < materials.Length)
        {
            materials[dissolveMaterialIndex].SetFloat("_Disolve", value);
        }
    }

    public void TakeObstacleDamage()
    {
        TakeDamage(obstacleDamage);
    }

    public void TakeSharkDamage()
    {
        TakeDamage(sharkDamage);
    }
}
