using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerCore : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 6f;
    public float gravity = -9.81f;
    public float jumpHeight = 1.5f;

    [Header("Look")]
    public float mouseSensitivity = 0.2f;
    public float maxLookAngle = 80f;
    public Transform cameraAnchor;

    [Header("Head Bob")]
    public float bobFrequency = 8f;
    public float bobAmplitude = 0.05f;

    [Header("Input Actions")]
    public InputActionAsset actions;

    CharacterController controller;
    InputAction moveAction;
    InputAction lookAction;
    InputAction jumpAction;

    Vector3 velocity;
    float xRotation;
    float bobTimer;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked;

        var gameplay = actions.FindActionMap("Player");
        moveAction = gameplay.FindAction("Move");
        lookAction = gameplay.FindAction("Look");
        jumpAction = gameplay.FindAction("Jump");

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

        if (input.magnitude > 0.1f && controller.isGrounded)
            bobTimer += Time.deltaTime * bobFrequency;
        else
            bobTimer = 0f;

        if (cameraAnchor)
        {
            float bobOffset = Mathf.Sin(bobTimer) * bobAmplitude * Mathf.Min(input.magnitude, 1f);
            cameraAnchor.localPosition = new Vector3(0f, bobOffset, 0f);
        }

        if (jumpAction.WasPressedThisFrame() && controller.isGrounded)
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);

        controller.Move((move * moveSpeed + velocity) * Time.deltaTime);
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
}
