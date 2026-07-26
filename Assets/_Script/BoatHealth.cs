using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class BoatHealth : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] private float maxHealth = 100f;

    [Header("Damage")]
    [SerializeField] private float obstacleDamage = 10f;
    [SerializeField] private float sharkDamage = 25f;
    [SerializeField] private float damageCooldown = 0.5f;

    [Header("Health Bar")]
    [SerializeField] private Slider healthSlider;

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
        UpdateHealthBar();
    }

    public void TakeDamage(float amount)
    {
        if (IsDead) return;
        if (Time.time - lastDamageTime < damageCooldown) return;

        lastDamageTime = Time.time;
        currentHealth = Mathf.Max(0f, currentHealth - amount);
        UpdateHealthBar();
        OnDamageTaken?.Invoke(currentHealth);

        if (currentHealth <= 0f)
        {
            OnDeath?.Invoke();
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }

    void UpdateHealthBar()
    {
        if (healthSlider == null) return;
        healthSlider.value = 1f - (currentHealth / maxHealth);
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
