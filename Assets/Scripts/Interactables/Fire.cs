using UnityEngine;

public class Fire : MonoBehaviour
{
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float extinguishRate = 25f;

    ParticleSystem particles;
    float health;

    void Start()
    {
        particles = GetComponent<ParticleSystem>();
        health = maxHealth;
    }

    public void Extinguish(float amount)
    {
        health -= amount;

        float t = health / maxHealth;
        transform.localScale = Vector3.one * Mathf.Lerp(0.1f, 1f, t);

        var main = particles.main;
        main.startLifetime = Mathf.Lerp(0f, main.startLifetime.constantMax, t);

        if (health <= 0f)
            Destroy(gameObject);
    }
}
