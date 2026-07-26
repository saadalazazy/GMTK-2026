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
    [SerializeField] private float repositionMinDist = 25f;
    [SerializeField] private float repositionMaxDist = 45f;
    [SerializeField] private float repositionDepth = -5f;
    [SerializeField] private float repositionSpeed = 40f;
    [SerializeField] private float repositionArrivalDist = 2f;

    [Header("Orbit")]
    [SerializeField] private float orbitSpeed = 0.08f;
    [SerializeField] private float orbitLungeRange = 6f;
    [SerializeField] private float orbitPatienceTime = 8f;
    [SerializeField] private float orbitChaseDelay = 1f;
    [SerializeField] private float orbitApproachSpeed = 10f;
    [SerializeField] private float orbitApproachArrivalDist = 2f;

    [Header("Chase")]
    [SerializeField] private Transform chaseTarget;
    [SerializeField] private float chaseSpeed = 18f;
    [SerializeField] private float chaseWaveAmplitude = 3f;
    [SerializeField] private float chaseWaveFrequency = 2f;
    [SerializeField] private float chaseOffsetRadius = 8f;
    [SerializeField] private float chaseCircleRadius = 6f;
    [SerializeField] private float chaseCircleSpeed = 2f;
    [SerializeField] private float chaseCircleDuration = 3f;
    [SerializeField] private float chaseDuration = 5f;

    [Header("Attack")]
    [SerializeField] private Transform attackPoint;
    [SerializeField] private float attackRandomOffsetX = 3f;
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
    [SerializeField] private AudioClip hitSound;
    [SerializeField] private AudioClip attackSound;
    [SerializeField] private AudioClip[] roarSounds;
    [SerializeField] private float roarChance = 0.3f;
    [SerializeField] private float roarCooldown = 4f;


    [Header("Events")]
    public UnityEvent OnAttack;
    public UnityEvent OnDeath;

    SharkState currentState;
    float health;
    BoatHealth boatHealth;
    float stateTimer;
    float splineProgress;
    float orbitPatienceTimer;
    float orbitChaseDelayTimer;
    bool wasScared;
    Vector3 lastBoatPosition;
    Vector3 attackTarget;
    Vector3 repositionTarget;
    Vector3 chasePoint;
    float chaseCircleTimer;
    bool chaseCircling;
    bool attackHit;
    float roarTimer;
    bool approachingAttack;

    public SharkState CurrentState => currentState;

    void Start()
    {
        if (boat == null)
        {
            var player = FindFirstObjectByType<PlayerCore>();
            if (player != null) boat = player.transform;
        }

        boatHealth = FindFirstObjectByType<BoatHealth>();
        health = maxHealth;
        lastBoatPosition = boat.position;
        SetRepositionTarget();
        transform.position = repositionTarget;
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

    void SetRepositionTarget()
    {
        float angle = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
        float dist = UnityEngine.Random.Range(repositionMinDist, repositionMaxDist);
        repositionTarget = boat.position + new Vector3(Mathf.Sin(angle) * dist, repositionDepth, Mathf.Cos(angle) * dist);
    }

    // --- Idle ---
    void StartIdle()
    {
        stateTimer = 0f;
        lastBoatPosition = boat.position;
        SetRepositionTarget();
        TransitionTo(SharkState.Idle);
    }

    void UpdateIdle()
    {
        Vector3 toTarget = repositionTarget - transform.position;
        float distToTarget = toTarget.magnitude;

        if (distToTarget > repositionArrivalDist)
        {
            transform.position += toTarget.normalized * repositionSpeed * Time.deltaTime;
            transform.position = new Vector3(transform.position.x, repositionDepth, transform.position.z);
            FaceDirection(toTarget.normalized);
            return;
        }

        stateTimer += Time.deltaTime;

        if (stateTimer >= idleObserveTime)
        {
            float boatSpeed = (boat.position - lastBoatPosition).magnitude / stateTimer;

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
        roarTimer -= Time.deltaTime;

        if (approachingAttack)
        {
            Vector3 knot0Pos = EvaluateSpline(0f);
            Vector3 toKnot = knot0Pos - transform.position;

            if (toKnot.magnitude > orbitApproachArrivalDist)
            {
                transform.position += toKnot.normalized * orbitApproachSpeed * Time.deltaTime;
                FaceDirection(toKnot.normalized);
            }
            else
            {
                approachingAttack = false;
                StartAttack();
            }
            return;
        }

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
            approachingAttack = true;
        }

        PlayRoar();
    }

    // --- Chase ---
    void StartChase()
    {
        stateTimer = 0f;
        chaseCircleTimer = 0f;
        chaseCircling = false;
        if (chaseTarget == null) chaseTarget = boat;

        float angle = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
        chasePoint = chaseTarget.position + new Vector3(Mathf.Cos(angle) * chaseOffsetRadius, 0f, Mathf.Sin(angle) * chaseOffsetRadius);
        chasePoint.y = transform.position.y;

        animator.SetBool("IsJumping", true);
        TransitionTo(SharkState.Chase);
    }

    void UpdateChase()
    {
        stateTimer += Time.deltaTime;
        roarTimer -= Time.deltaTime;

        if (chaseTarget == null)
        {
            StartIdle();
            return;
        }

        if (!chaseCircling)
        {
            Vector3 dir = chasePoint - transform.position;
            float dist = dir.magnitude;

            float wave = Mathf.Sin(Time.time * chaseWaveFrequency) * chaseWaveAmplitude;
            Vector3 waveOffset = transform.right * wave;

            if (dist > 1f)
            {
                transform.position += (dir + waveOffset).normalized * chaseSpeed * Time.deltaTime;
                FaceDirection(dir.normalized);
            }

            if (dist < 2f)
            {
                chaseCircling = true;
                chaseCircleTimer = 0f;
            }
        }
        else
        {
            Vector3 toBoat = chaseTarget.position - transform.position;
            toBoat.y = 0f;
            Vector3 circleCenter = chaseTarget.position;

            Vector3 toCenter = circleCenter - transform.position;
            toCenter.y = 0f;

            if (toCenter.magnitude > chaseCircleRadius + 2f)
            {
                transform.position += toCenter.normalized * chaseSpeed * Time.deltaTime;
            }
            else
            {
                Vector3 tangent = Vector3.Cross(Vector3.up, toCenter.normalized);
                transform.position += tangent * chaseCircleSpeed * Time.deltaTime;
            }

            FaceDirection((circleCenter - transform.position).normalized);
            chaseCircleTimer += Time.deltaTime;

            if (chaseCircleTimer >= chaseCircleDuration)
            {
                StartAttack();
                return;
            }
        }

        if (stateTimer >= chaseDuration)
        {
            ScareAway();
        }

        PlayRoar();
    }

    // --- Attack ---
    void StartAttack()
    {
        stateTimer = 0f;
        attackHit = false;

        if (attackPoint != null)
        {
            float randomX = Random.Range(-attackRandomOffsetX, attackRandomOffsetX);
            attackTarget = attackPoint.position + new Vector3(randomX, 0f, 0f);
        }
        else
        {
            attackTarget = boat.position;
        }

        if (attackSound != null && audioSource != null)
            audioSource.PlayOneShot(attackSound);

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

        if (!attackHit)
        {
            attackHit = true;
            OnAttack?.Invoke();
            if (boatHealth != null) boatHealth.TakeSharkDamage();
            ScareAway();
        }
        else if (t >= 1f)
        {
            if (!attackHit)
            {
                attackHit = true;
                OnAttack?.Invoke();
                if (boatHealth != null) boatHealth.TakeSharkDamage();
            }
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
        pos.y = Mathf.Lerp(pos.y, repositionDepth, Time.deltaTime * 3f);
        transform.position = pos;
        FaceDirection(awayDir);

        if (stateTimer >= fleeDuration)
        {
            animator.SetBool("IsFleeing", false);
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
            audioSource.PlayOneShot(hitSound);
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
    void PlayRoar()
    {
        if (roarSounds == null || roarSounds.Length == 0 || audioSource == null) return;
        if (roarTimer > 0f) return;
        if (UnityEngine.Random.value > roarChance) return;

        AudioClip clip = roarSounds[UnityEngine.Random.Range(0, roarSounds.Length)];
        audioSource.PlayOneShot(clip);
        roarTimer = roarCooldown;
    }

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


}
