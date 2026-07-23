using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Animator))]
public class SharkEnemy : MonoBehaviour
{
    public enum SharkState { Approach, Circle, Lunge, Flee, Dead }

    [Header("References")]
    [SerializeField] private Transform boat;
    [SerializeField] private Animator animator;

    [Header("Orbit")]
    [SerializeField] private float orbitRadius = 30f;
    [SerializeField] private float orbitSpeed = 20f;
    [SerializeField] private float orbitHeight = -2f;

    [Header("Approach")]
    [SerializeField] private float approachSpeed = 15f;
    [SerializeField] private float approachHeight = -3f;

    [Header("Lunge")]
    [SerializeField] private float lungeInterval = 8f;
    [SerializeField] private float lungeSpeed = 25f;
    [SerializeField] private float lungeArcHeight = 8f;
    [SerializeField] private float lungeDuration = 1.2f;

    [Header("Flee")]
    [SerializeField] private float fleeSpeed = 30f;
    [SerializeField] private float fleeDuration = 4f;

    [Header("Health")]
    [SerializeField] private bool useHealth = false;
    [SerializeField] private float maxHealth = 100f;

    [Header("Damage")]
    [SerializeField] private float damageAmount = 25f;

    [Header("Events")]
    public UnityEvent OnAttack;
    public UnityEvent OnDeath;

    SharkState currentState;
    float health;
    float lungeTimer;
    float stateTimer;
    float orbitAngle;
    Vector3 orbitCenter;
    Vector3 lungeStartPos;
    Vector3 lungeDirection;
    bool wasScared;

    public SharkState CurrentState => currentState;

    void Start()
    {
        if (animator == null) animator = GetComponent<Animator>();
        if (boat == null)
        {
            var player = FindFirstObjectByType<PlayerCore>();
            if (player != null) boat = player.transform;
        }

        health = maxHealth;
        currentState = SharkState.Approach;
        stateTimer = 0f;

        Vector3 toBoat = boat.position - transform.position;
        toBoat.y = 0f;
        orbitAngle = Mathf.Atan2(toBoat.x, toBoat.z) * Mathf.Rad2Deg;
        orbitCenter = new Vector3(boat.position.x, orbitHeight, boat.position.z);
    }

    void Update()
    {
        if (boat == null) return;

        switch (currentState)
        {
            case SharkState.Approach:
                UpdateApproach();
                break;
            case SharkState.Circle:
                UpdateCircle();
                break;
            case SharkState.Lunge:
                UpdateLunge();
                break;
            case SharkState.Flee:
                UpdateFlee();
                break;
            case SharkState.Dead:
                break;
        }
    }

    // --- Approach ---
    void UpdateApproach()
    {
        orbitCenter = new Vector3(boat.position.x, orbitHeight, boat.position.z);
        Vector3 targetPos = orbitCenter + (transform.position - orbitCenter).normalized * orbitRadius;
        targetPos.y = orbitHeight;

        Vector3 dir = (targetPos - transform.position).normalized;
        transform.position += dir * approachSpeed * Time.deltaTime;
        FaceDirection(dir);

        float dist = Vector3.Distance(
            new Vector3(transform.position.x, 0, transform.position.z),
            new Vector3(targetPos.x, 0, targetPos.z)
        );

        if (dist < 2f)
        {
            orbitCenter = new Vector3(boat.position.x, orbitHeight, boat.position.z);
            Vector3 toBoat = boat.position - transform.position;
            orbitAngle = Mathf.Atan2(toBoat.x, toBoat.z) * Mathf.Rad2Deg;
            lungeTimer = lungeInterval;
            TransitionTo(SharkState.Circle);
        }
    }

    // --- Circle ---
    void UpdateCircle()
    {
        orbitCenter = new Vector3(boat.position.x, orbitHeight, boat.position.z);
        orbitAngle += orbitSpeed * Time.deltaTime;

        float rad = orbitAngle * Mathf.Deg2Rad;
        Vector3 offset = new Vector3(Mathf.Sin(rad), 0f, Mathf.Cos(rad)) * orbitRadius;
        Vector3 targetPos = orbitCenter + offset;

        transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * 5f);

        Vector3 dir = (targetPos - transform.position);
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.01f)
            FaceDirection(dir.normalized);

        lungeTimer -= Time.deltaTime;
        if (lungeTimer <= 0f)
        {
            StartLunge();
        }
    }

    // --- Lunge ---
    void StartLunge()
    {
        lungeStartPos = transform.position;
        Vector3 boatDir = (boat.position - transform.position);
        boatDir.y = 0f;
        lungeDirection = boatDir.normalized;
        stateTimer = 0f;

        animator.SetBool("IsJumping", true);
        TransitionTo(SharkState.Lunge);
    }

    void UpdateLunge()
    {
        stateTimer += Time.deltaTime;
        float t = stateTimer / lungeDuration;

        Vector3 flatPos = Vector3.Lerp(lungeStartPos, lungeStartPos + lungeDirection * orbitRadius * 1.2f, t);
        float arc = Mathf.Sin(t * Mathf.PI) * lungeArcHeight;
        float yPos = Mathf.Lerp(lungeStartPos.y, orbitHeight, t) + arc;

        transform.position = new Vector3(flatPos.x, yPos, flatPos.z);

        Vector3 dir = lungeDirection;
        if (t < 0.5f)
        {
            Vector3 upDir = Vector3.Lerp(lungeDirection, Vector3.up, t * 2f);
            FaceDirection(upDir.normalized);
        }
        else
        {
            Vector3 downDir = Vector3.Lerp(Vector3.up, lungeDirection, (t - 0.5f) * 2f);
            FaceDirection(downDir.normalized);
        }

        if (t >= 1f)
        {
            animator.SetBool("IsJumping", false);
            lungeTimer = lungeInterval;
            TransitionTo(SharkState.Circle);
        }
    }

    // --- Flee ---
    void UpdateFlee()
    {
        stateTimer += Time.deltaTime;

        Vector3 awayDir = (transform.position - boat.position);
        awayDir.y = 0f;
        awayDir.Normalize();

        transform.position += awayDir * fleeSpeed * Time.deltaTime;
        FaceDirection(awayDir);

        float dist = Vector3.Distance(
            new Vector3(transform.position.x, 0, transform.position.z),
            new Vector3(boat.position.x, 0, boat.position.z)
        );

        if (stateTimer >= fleeDuration || dist > 200f)
        {
            Destroy(gameObject);
        }
    }

    // --- Damage ---
    public void TakeDamage(float amount)
    {
        if (currentState == SharkState.Dead) return;

        if (!useHealth)
        {
            ScareAway();
            return;
        }

        health -= amount;

        if (health <= 0f)
        {
            health = 0f;
            TransitionTo(SharkState.Dead);
            OnDeath?.Invoke();
            Destroy(gameObject, 2f);
        }
        else
        {
            ScareAway();
        }
    }

    public void ScareAway()
    {
        if (currentState == SharkState.Flee || currentState == SharkState.Dead) return;

        wasScared = true;
        animator.SetBool("IsJumping", false);
        animator.SetBool("IsFleeing", true);
        stateTimer = 0f;
        TransitionTo(SharkState.Flee);
    }

    // --- Helpers ---
    void TransitionTo(SharkState newState)
    {
        currentState = newState;
    }

    void FaceDirection(Vector3 dir)
    {
        if (dir.sqrMagnitude < 0.001f) return;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.001f) return;

        Quaternion targetRot = Quaternion.LookRotation(dir, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, 10f * Time.deltaTime);
    }

    void OnTriggerEnter(Collider other)
    {
        if (currentState == SharkState.Lunge && stateTimer > lungeDuration * 0.3f)
        {
            if (other.CompareTag("Player") || other.transform.root == boat)
            {
                OnAttack?.Invoke();
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        if (boat != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(new Vector3(boat.position.x, orbitHeight, boat.position.z), orbitRadius);
        }
    }
}
