using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerCore : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 6f;
    public float sprintSpeed = 10f;
    public float gravity = -9.81f;
    public float jumpHeight = 1.5f;

    [Header("Look")]
    public float mouseSensitivity = 0.2f;
    public float maxLookAngle = 80f;
    public Transform cameraAnchor;
    public Transform cameraBob;

    [Header("Head Bob")]
    public float bobFrequency = 8f;
    public float bobAmplitude = 0.05f;

    [Header("Footsteps")]
    public AudioClip[] footstepClips;
    public float footstepVolume = 0.5f;

    [Header("Input Actions")]
    public InputActionAsset actions;

    CharacterController controller;
    AudioSource audioSource;
    InputAction moveAction;
    InputAction lookAction;
    InputAction jumpAction;
    InputAction sprintAction;

    Vector3 velocity;
    float xRotation;
    float bobTimer;
    bool wasBobBottom;
    bool isSprinting;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        audioSource = gameObject.AddComponent<AudioSource>();
        Cursor.lockState = CursorLockMode.Locked;

        var gameplay = actions.FindActionMap("Player");
        moveAction = gameplay.FindAction("Move");
        lookAction = gameplay.FindAction("Look");
        jumpAction = gameplay.FindAction("Jump");
        sprintAction = gameplay.FindAction("Sprint");

        actions.FindActionMap("Player").Enable();
    }

    void Update()
    {
        HandleMovement();
        HandleLook();
    }

    void HandleMovement()
    {
        if (controller.isGrounded && velocity.y < 0)
            velocity.y = -2f;
        else if (!controller.isGrounded)
            velocity.y += gravity * Time.deltaTime;

        Vector2 input = moveAction.ReadValue<Vector2>();
        Vector3 move = transform.right * input.x + transform.forward * input.y;

        isSprinting = sprintAction.IsPressed() && input.magnitude > 0.1f;
        float currentSpeed = isSprinting ? sprintSpeed : moveSpeed;

        if (input.magnitude > 0.1f && controller.isGrounded)
        {
            float bobSpeedMultiplier = isSprinting ? 1.4f : 1f;
            bobTimer += Time.deltaTime * bobFrequency * bobSpeedMultiplier;
        }
        else
            bobTimer = 0f;

        if (cameraBob)
        {
            float target = 0f;
            if (input.magnitude > 0.1f && controller.isGrounded)
                target = Mathf.Sin(bobTimer) * bobAmplitude;
            cameraBob.localPosition = Vector3.Lerp(cameraBob.localPosition, new Vector3(0f, target, 0f), Time.deltaTime * bobFrequency);
        }

        bool isBobBottom = Mathf.Sin(bobTimer) < -0.9f;
        if (isBobBottom && !wasBobBottom && input.magnitude > 0.1f && controller.isGrounded)
            PlayFootstep();
        wasBobBottom = isBobBottom;

        if (jumpAction.WasPressedThisFrame() && controller.isGrounded)
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);

        controller.Move((move * currentSpeed + velocity) * Time.deltaTime);
    }

    void HandleLook()
    {
        Vector2 input = lookAction.ReadValue<Vector2>();

        float mx = input.x * mouseSensitivity;
        float my = input.y * mouseSensitivity;

        xRotation -= my;
        xRotation = Mathf.Clamp(xRotation, -maxLookAngle, maxLookAngle);

        if (cameraAnchor)
            cameraAnchor.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

        transform.Rotate(Vector3.up * mx);
    }

    void PlayFootstep()
    {
        if (footstepClips.Length == 0) return;
        audioSource.pitch = Random.Range(0.9f, 1.1f);
        audioSource.PlayOneShot(footstepClips[Random.Range(0, footstepClips.Length)], footstepVolume);
    }
}
