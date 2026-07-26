using DG.Tweening;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerCore : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 6f;
    public float sprintSpeed = 10f;
    public float gravity = -9.81f;

    [Header("Look")]
    public float mouseSensitivity = 30f; // Adjusted for Time.deltaTime scaling
    public float maxLookAngle = 80f;
    public Transform cameraAnchor;
    public Transform cameraBob;

    [Header("Head Bob")]
    public float bobFrequency = 8f;
    public float bobVerticalAmplitude = 0.05f;
    public float bobHorizontalAmplitude = 0.03f;

    [Header("Hand Inertia")]
    public float handSpringStiffness = 150f;
    public float handDamping = 12f;
    public float handMass = 1f;

    [Header("Footsteps")]
    public AudioClip[] footstepClips;
    public float footstepVolume = 0.5f;

    [Header("Input Actions")]
    public InputActionAsset actions;

    [Header("Utils")]
    public Transform handMesh;

    [Header("Boat")]
    [SerializeField] private Rigidbody boatRigidbody;

    [Header("Ladder")]
    public float climbSpeed = 4f;

    [HideInInspector] public bool inputEnabled = true;


    private CharacterController controller;
    private AudioSource audioSource;
    private InputAction moveAction;
    private InputAction lookAction;
    private InputAction sprintAction;

    private Vector3 velocity;
    private float xRotation;
    private float bobTimer;
    private bool wasBobBottom;
    private bool isSprinting;
    private Vector2 handOffset;
    private Vector2 handSpringVelocity;
    private Vector3 bobVelocity;
    private bool isClimbing;
    private Ladder currentLadder;
    private Ladder nearbyLadder;
    private float climbProgress;
    private float climbCooldown;
    private bool climbFlipped;

    public TyperwriterText typerwriterText { get; private set; }

    [SerializeField] private CanvasGroup winPanel;

    void Start()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        Cursor.lockState = CursorLockMode.Locked;

        if (boatRigidbody == null)
        {
            var boat = FindFirstObjectByType<WaterFloat>();
            if (boat != null) boatRigidbody = boat.GetComponent<Rigidbody>();
        }

        var gameplay = actions.FindActionMap("Player");
        moveAction = gameplay.FindAction("Move");
        lookAction = gameplay.FindAction("Look");
        sprintAction = gameplay.FindAction("Sprint");

        gameplay.Enable();

        typerwriterText = FindFirstObjectByType<TyperwriterText>();
        typerwriterText.StartTyping("Move towards the lighthouse");
    }

    void OnDisable()
    {
        handMesh.gameObject.SetActive(false);
        velocity = Vector3.zero;
    }

    void OnEnable()
    {
        controller = GetComponent<CharacterController>();
        handMesh.gameObject.SetActive(true);

        controller.enabled = false;
        controller.enabled = true;
    }

    void Update()
    {
        HandleLook();
        HandleMovement();
    }

    public void Win()
    {
        winPanel.gameObject.SetActive(true);
        winPanel.DOFade(1f, 1f).SetEase(Ease.InOutQuad);
    }

    void HandleMovement()
    {
        Vector2 input = moveAction.ReadValue<Vector2>();

        if (climbCooldown > 0f)
            climbCooldown -= Time.deltaTime;

        if (isClimbing)
        {
            HandleClimbing(input);
            return;
        }

        // Gravity logic
        if (controller.isGrounded && velocity.y < 0)
            velocity.y = -2f;
        else if (!controller.isGrounded)
            velocity.y += gravity * Time.deltaTime;

        Vector3 move = transform.right * input.x + transform.forward * input.y;

        isSprinting = sprintAction.IsPressed() && input.magnitude > 0.1f;
        float currentSpeed = isSprinting ? sprintSpeed : moveSpeed;

        // Bob timer advancement
        if (input.magnitude > 0.1f && controller.isGrounded)
        {
            float bobSpeedMultiplier = isSprinting ? 1.4f : 1f;
            bobTimer += Time.deltaTime * bobFrequency * bobSpeedMultiplier;
        }

        // Camera/Hand Bob calculation
        if (cameraBob)
        {
            Vector3 targetBob = Vector3.zero;
            if (input.magnitude > 0.1f && controller.isGrounded)
            {
                float verticalBob = Mathf.Sin(bobTimer) * bobVerticalAmplitude;
                float horizontalBob = Mathf.Cos(bobTimer / 2f) * bobHorizontalAmplitude;
                targetBob = new Vector3(horizontalBob, verticalBob, 0f);
            }
            cameraBob.localPosition = Vector3.SmoothDamp(cameraBob.localPosition, targetBob, ref bobVelocity, 1f / bobFrequency);
        }

        // Footstep triggering at sinusoidal trough
        bool isBobBottom = Mathf.Sin(bobTimer) < -0.85f;
        if (isBobBottom && !wasBobBottom && input.magnitude > 0.1f && controller.isGrounded)
        {
            PlayFootstep();
        }
        wasBobBottom = isBobBottom;

        // Ladder detection
        if (input.magnitude > 0.1f)
        {
            TryStartClimb(input);
        }

        if (isClimbing) return;
        // Character Controller movement execution
        controller.Move((move * currentSpeed + velocity) * Time.deltaTime);
    }

    void TryStartClimb(Vector2 input)
    {
        if (climbCooldown > 0f || nearbyLadder == null) return;
        if (Mathf.Abs(input.y) < 0.1f) return;

        float distToBottom = Vector3.Distance(transform.position, nearbyLadder.GetPositionAtProgress(0f));
        float distToTop = Vector3.Distance(transform.position, nearbyLadder.GetPositionAtProgress(1f));
        bool fromTop = distToTop < distToBottom;

        StartClimbing(nearbyLadder, fromTop);
    }

    void OnTriggerEnter(Collider other)
    {
        if (isClimbing) return;
        Ladder ladder = other.GetComponentInParent<Ladder>();
        if (ladder != null) nearbyLadder = ladder;
    }

    void OnTriggerExit(Collider other)
    {
        Ladder ladder = other.GetComponentInParent<Ladder>();
        if (ladder != null && ladder == nearbyLadder) nearbyLadder = null;
    }

    void StartClimbing(Ladder ladder, bool fromTop)
    {
        isClimbing = true;
        currentLadder = ladder;
        climbProgress = fromTop ? 1f : 0f;
        climbFlipped = fromTop;
        velocity = Vector3.zero;
        controller.enabled = false;
    }

    void HandleClimbing(Vector2 input)
    {
        float climbInput = climbFlipped ? -input.y : input.y;
        float length = currentLadder.GetLength();
        float progressPerSecond = climbSpeed / length;

        climbProgress += climbInput * progressPerSecond * Time.deltaTime;
        climbProgress = Mathf.Clamp01(climbProgress);

        transform.position = currentLadder.GetOffsetPosition(climbProgress);
        controller.enabled = false;

        // Exit conditions
        if (climbProgress >= 1f || climbProgress <= 0f)
        {
            StopClimbing();
        }
    }

    void StopClimbing()
    {
        isClimbing = false;
        currentLadder = null;
        nearbyLadder = null;
        controller.enabled = true;
        velocity = Vector3.zero;
        climbCooldown = 0.6f;
    }

    void HandleLook()
    {
        Vector2 input = lookAction.ReadValue<Vector2>();

        // Multiply by Time.deltaTime for frame-rate independent camera movement
        float mx = input.x * mouseSensitivity * Time.deltaTime;
        float my = input.y * mouseSensitivity * Time.deltaTime;

        xRotation -= my;
        xRotation = Mathf.Clamp(xRotation, -maxLookAngle, maxLookAngle);

        if (cameraAnchor)
            cameraAnchor.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

        transform.Rotate(Vector3.up * mx);

        // Hand inertia spring-damper physics
        if (cameraBob)
        {
            Vector2 springForce = -handSpringStiffness * handOffset;
            Vector2 dampingForce = -handDamping * handSpringVelocity;

            // Normalize input force across frame durations
            Vector2 inputForce = input * (handSpringStiffness * 0.05f);
            Vector2 totalForce = springForce + dampingForce + inputForce;

            Vector2 acceleration = totalForce / handMass;
            handSpringVelocity += acceleration * Time.deltaTime;
            handOffset += handSpringVelocity * Time.deltaTime;

            cameraBob.localRotation = Quaternion.Euler(handOffset.y, handOffset.x, 0f);
        }
    }

    void PlayFootstep()
    {
        if (footstepClips == null || footstepClips.Length == 0) return;
        audioSource.pitch = UnityEngine.Random.Range(0.9f, 1.1f);
        audioSource.PlayOneShot(footstepClips[UnityEngine.Random.Range(0, footstepClips.Length)], footstepVolume);
    }
}
