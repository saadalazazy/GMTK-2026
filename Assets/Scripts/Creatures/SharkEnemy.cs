using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Splines;
using Unity.Mathematics;

public class SharkEnemy : MonoBehaviour
{
    public enum SharkState { Idle, Orbit, Chase, Attack, Flee, Dead }

    [Header("References")]
    [SerializeField] private Transform boat;
    [SerializeField] private Animator animator;
    [SerializeField] private SplineContainer orbitSpline;

    [Header("Idle / Tracking")]
    [SerializeField] private float idleObserveTime = 4f;
    [SerializeField] private float idleSpeedThreshold = 0.5f;
    [SerializeField] private float teleportMinDist = 25f;
    [SerializeField] private float teleportMaxDist = 45f;
    [SerializeField] private float teleportDepth = -5f;

    [Header("Orbit")]
    [SerializeField] private float orbitSpeed = 0.08f;
    [SerializeField] private float orbitLungeRange = 6f;
    [SerializeField] private float orbitPatienceTime = 8f;
    [SerializeField] private float orbitChaseDelay = 1f;

    [Header("Chase")]
    [SerializeField] private Transform chaseTarget;
    [SerializeField] private float chaseSpeed = 18f;
    [SerializeField] private float chaseWaveAmplitude = 3f;
    [SerializeField] private float chaseWaveFrequency = 2f;
    [SerializeField] private float chaseLungeDistance = 5f;
    [SerializeField] private float chaseLungeHoldTime = 1.5f;
    [SerializeField] private float chaseDuration = 5f;

    [Header("Attack")]
    [SerializeField] private float attackDuration = 1f;
    [SerializeField] private float attackSpeed = 30f;
    [SerializeField] private float attackLungeHeight = 3f;

    [Header("Flee")]
    [SerializeField] private float fleeSpeed = 30f;
    [SerializeField] private float fleeDuration = 4f;

    [Header("Health")]
    [SerializeField] private bool useHealth = false;
    [SerializeField] private float maxHealth = 100f;

    [Header("Damage")]
    [SerializeField] private float damageAmount = 25f;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip damageSound;

    [Header("Events")]
    public UnityEvent OnAttack;
    public UnityEvent OnDeath;

    SharkState currentState;
    float health;
    float stateTimer;
    float splineProgress;
    float orbitPatienceTimer;
    float orbitChaseDelayTimer;
    float chaseLungeTimer;
    bool wasScared;
    Vector3 lastBoatPosition;
    Vector3 attackTarget;
    bool attackHit;

    public SharkState CurrentState => currentState;

    void Start()
    {
        if (boat == null)
        {
            var player = FindFirstObjectByType<PlayerCore>();
            if (player != null) boat = player.transform;
        }

        health = maxHealth;
        lastBoatPosition = boat != null ? boat.position : Vector3.zero;
        TeleportNearBoat();
        StartIdle();
    }

    void Update()
    {
        if (boat == null) return;

        switch (currentState)
        {
            case SharkState.Idle:
                UpdateIdle();
                break;
            case SharkState.Orbit:
                UpdateOrbit();
                break;
            case SharkState.Chase:
                UpdateChase();
                break;
            case SharkState.Attack:
                UpdateAttack();
                break;
            case SharkState.Flee:
                UpdateFlee();
                break;
            case SharkState.Dead:
                break;
        }
    }

    // --- Spline ---
    Vector3 EvaluateSpline(float t)
    {
        SplineUtility.Evaluate(orbitSpline.Spline, t, out float3 position, out _, out _);
        return orbitSpline.transform.TransformPoint(new Vector3(position.x, position.y, position.z));
    }

    // --- Teleport ---
    void TeleportNearBoat()
    {
        float angle = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
        float dist = UnityEngine.Random.Range(teleportMinDist, teleportMaxDist);
        Vector3 pos = boat.position + new Vector3(Mathf.Sin(angle) * dist, teleportDepth, Mathf.Cos(angle) * dist);
        transform.position = pos;
    }

    // --- Idle ---
    void StartIdle()
    {
        stateTimer = 0f;
        lastBoatPosition = boat.position;
        TransitionTo(SharkState.Idle);
    }

    void UpdateIdle()
    {
        stateTimer += Time.deltaTime;

        if (stateTimer >= idleObserveTime)
        {
            float boatSpeed = (boat.position - lastBoatPosition).magnitude / stateTimer;
            TeleportNearBoat();

            if (boatSpeed < idleSpeedThreshold)
                StartOrbit();
            else
                StartChase();
        }
    }

    // --- Orbit ---
    void StartOrbit()
    {
        stateTimer = 0f;
        splineProgress = 0f;
        orbitPatienceTimer = orbitPatienceTime;
        orbitChaseDelayTimer = 0f;
        TransitionTo(SharkState.Orbit);
    }

    void UpdateOrbit()
    {
        stateTimer += Time.deltaTime;

        splineProgress += orbitSpeed * Time.deltaTime;
        if (splineProgress > 1f) splineProgress -= 1f;

        Vector3 targetPos = EvaluateSpline(splineProgress);
        Vector3 moveDir = targetPos - transform.position;
        if (moveDir.magnitude > 0.01f)
        {
            transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * 5f);
            FaceDirection(moveDir.normalized);
        }

        float boatSpeed = (boat.position - lastBoatPosition).magnitude / Mathf.Max(Time.deltaTime, 0.001f);
        lastBoatPosition = boat.position;

        if (boatSpeed > idleSpeedThreshold)
        {
            orbitChaseDelayTimer += Time.deltaTime;
            if (orbitChaseDelayTimer >= orbitChaseDelay)
            {
                StartChase();
                return;
            }
        }

        orbitPatienceTimer -= Time.deltaTime;

        if (orbitPatienceTimer <= 0f)
        {
            StartAttack();
        }
    }

    // --- Chase ---
    void StartChase()
    {
        stateTimer = 0f;
        chaseLungeTimer = 0f;
        animator.SetBool("IsJumping", true);
        TransitionTo(SharkState.Chase);
    }

    void UpdateChase()
    {
        stateTimer += Time.deltaTime;

        if (chaseTarget == null)
        {
            StartIdle();
            return;
        }

        Vector3 targetPos = chaseTarget.position;

        float wave = Mathf.Sin(Time.time * chaseWaveFrequency) * chaseWaveAmplitude;
        Vector3 waveOffset = transform.right * wave;
        Vector3 moveTarget = targetPos + waveOffset;

        Vector3 dir = moveTarget - transform.position;
        float dist = dir.magnitude;

        if (dist > 1f)
        {
            transform.position += dir.normalized * chaseSpeed * Time.deltaTime;
            FaceDirection(dir.normalized);
        }

        if (dist < chaseLungeDistance)
        {
            chaseLungeTimer += Time.deltaTime;
            if (chaseLungeTimer >= chaseLungeHoldTime)
            {
                StartAttack();
                return;
            }
        }
        else
        {
            chaseLungeTimer = 0f;
        }

        if (stateTimer >= chaseDuration)
        {
            ScareAway();
        }
    }

    // --- Attack ---
    void StartAttack()
    {
        stateTimer = 0f;
        attackHit = false;

        Vector3 dir = boat.position - transform.position;
        if (Physics.Raycast(transform.position, dir.normalized, out RaycastHit hit, dir.magnitude + 10f))
            attackTarget = hit.point;
        else
            attackTarget = boat.position;

        animator.CrossFade("Jump", 0.1f);
        TransitionTo(SharkState.Attack);
    }

    void UpdateAttack()
    {
        stateTimer += Time.deltaTime;
        float t = stateTimer / attackDuration;

        Vector3 startPos = transform.position;
        Vector3 flatDir = attackTarget - transform.position;
        flatDir.y = 0f;

        Vector3 flatPos = Vector3.MoveTowards(startPos, new Vector3(attackTarget.x, startPos.y, attackTarget.z), attackSpeed * Time.deltaTime);
        float arc = Mathf.Sin(t * Mathf.PI) * attackLungeHeight;
        float yPos = Mathf.Lerp(startPos.y, attackTarget.y, t) + arc;

        transform.position = new Vector3(flatPos.x, yPos, flatPos.z);

        if (flatDir.sqrMagnitude > 0.01f)
            FaceDirection(flatDir.normalized);

        if (!attackHit && Vector3.Distance(transform.position, attackTarget) < 2f)
        {
            attackHit = true;
            OnAttack?.Invoke();
            ScareAway();
        }
        else if (t >= 1f)
        {
            ScareAway();
        }
    }

    // --- Flee ---
    void UpdateFlee()
    {
        stateTimer += Time.deltaTime;

        Vector3 awayDir = transform.position - boat.position;
        awayDir.y = 0f;
        awayDir.Normalize();

        Vector3 pos = transform.position + awayDir * fleeSpeed * Time.deltaTime;
        pos.y = Mathf.Lerp(pos.y, teleportDepth, Time.deltaTime * 3f);
        transform.position = pos;
        FaceDirection(awayDir);

        if (stateTimer >= fleeDuration)
        {
            animator.SetBool("IsFleeing", false);
            TeleportNearBoat();
            StartIdle();
        }
    }

    // --- Damage ---
    public void TakeDamage(float amount)
    {
        if (currentState == SharkState.Dead) return;

        if (damageSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(damageSound);
        }

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
        orbitPatienceTimer = orbitPatienceTime;
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
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.001f) return;

        Quaternion targetRot = Quaternion.LookRotation(dir, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, 10f * Time.deltaTime);
    }

    void OnTriggerEnter(Collider other)
    {
        if (currentState == SharkState.Chase && stateTimer > 0.3f)
        {
            if (other.CompareTag("Player") || other.transform.root == boat)
            {
                StartAttack();
            }
        }
    }

    void OnGUI()
    {
        if (!Debug.isDebugBuild) return;

        float dist = 0f;
        if (chaseTarget != null)
            dist = Vector3.Distance(transform.position, chaseTarget.position);

        float boatSpeed = 0f;
        if (boat != null)
            boatSpeed = (boat.position - lastBoatPosition).magnitude / Mathf.Max(stateTimer, 0.01f);

        GUILayout.BeginArea(new Rect(10, 10, 280, 260));
        GUILayout.Label($"State: {currentState}");
        GUILayout.Label($"Health: {health:F0} / {maxHealth}");
        GUILayout.Label($"Boat speed: {boatSpeed:F2}");
        GUILayout.Label($"Orbit patience: {orbitPatienceTimer:F1} / {orbitPatienceTime}");
        GUILayout.Label($"Chase delay: {orbitChaseDelayTimer:F1} / {orbitChaseDelay}");
        GUILayout.Label($"Distance to target: {dist:F1}");
        GUILayout.Label($"Chase lunge: {chaseLungeTimer:F1} / {chaseLungeHoldTime}");
        GUILayout.Label($"Attack: {stateTimer:F1} / {attackDuration}");
        GUILayout.Label($"State timer: {stateTimer:F1}");
        GUILayout.EndArea();
    }
}
