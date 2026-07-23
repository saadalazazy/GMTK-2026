using DG.Tweening;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

public class HarpoonController : MonoBehaviour
{
    [SerializeField] private Transform harpoonAnchor;
    [SerializeField] private GameObject harpoonArrowPrefab;

    [SerializeField] private Transform loadedArrow;
    [SerializeField] private Transform harpoonBodyAnchor;
    [SerializeField] private Transform harpoonArrowAnchor;


    [SerializeField] private float shootingForce = 3000f;
    [SerializeField] private float reloadDelay = 2f;
    [SerializeField] private AudioClip shootSound;

    [SerializeField] private AudioSource audioSource;
    [SerializeField] private CinemachineCamera camera;

    [SerializeField] private Vector3 pos;

    public InputActionAsset actions;

    InputAction shootAction;
    InputAction lookAction;

    Camera mainCamera;
    float zRotation;


    void Start()
    {
        mainCamera = Camera.main;

        var gameplay = actions.FindActionMap("Player");
        shootAction = gameplay.FindAction("Attack");
        lookAction = gameplay.FindAction("Look");
    }

    void Update()
    {
        HandleHarpoon();
        if (shootAction.WasPressedThisFrame())
        {
            ShootHarpoon();
        }
    }

    void HandleHarpoon()
    {
        Vector2 lookInput = lookAction.ReadValue<Vector2>();

        // Increased from 5f to 150f because degrees are now calculated per second
        float rotationSpeed = 10f;

        // Apply Time.deltaTime to make rotation frame-rate independent
        harpoonAnchor.Rotate(Vector3.up, lookInput.x * rotationSpeed * Time.deltaTime);
        float y = harpoonAnchor.localEulerAngles.y;
        if (y > 180f) y -= 360f;
        y = Mathf.Clamp(y, -70f, 70f);
        harpoonAnchor.localEulerAngles = new Vector3(
            harpoonAnchor.localEulerAngles.x,
            y,
            harpoonAnchor.localEulerAngles.z
        );


        zRotation -= lookInput.y * rotationSpeed * Time.deltaTime;
        zRotation = Mathf.Clamp(zRotation, -45f, 30f);

        harpoonBodyAnchor.localRotation = Quaternion.Euler(0f, 0f, zRotation);

    }

    void ShootHarpoon()
    {
        if (loadedArrow != null)
        {
            loadedArrow.parent = null; // Detach the arrow from the harpoon
            loadedArrow.localScale = Vector3.one; // Reset scale if needed

            // Add force to the arrow to shoot it forward
            Rigidbody rb = loadedArrow.GetComponent<Rigidbody>();
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.isKinematic = false;
            if (rb != null)
            {
                rb.AddForce(mainCamera.transform.forward * shootingForce); // Adjust force as needed
            }

            camera.transform.DOShakeRotation(0.2f, 1f, 2, 40);

            audioSource.PlayOneShot(shootSound);
            Destroy(loadedArrow.gameObject, 5f); // Destroy the arrow after 5 seconds to clean up

            loadedArrow = null;


            StartCoroutine(ReloadAfterDelay());
        }
    }

    System.Collections.IEnumerator ReloadAfterDelay()
    {
        yield return new WaitForSeconds(reloadDelay);
        Reload();
    }

    void Reload()
    {
        loadedArrow = Instantiate(harpoonArrowPrefab, harpoonArrowAnchor).transform;
        loadedArrow.SetLocalPositionAndRotation(pos, Quaternion.identity);
    }
}
