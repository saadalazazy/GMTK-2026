using UnityEngine;
using UnityEngine.InputSystem;

public class HarpoonController : MonoBehaviour
{
    [SerializeField] private Transform harpoonAnchor;
    [SerializeField] private GameObject harpoonArrowPrefab;

    [SerializeField] private Transform loadedArrow;


    public InputActionAsset actions;

    InputAction shootAction;
    InputAction lookAction;

    Camera mainCamera;

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
        // Rotate the harpoon anchor based on the look input
        Vector2 lookInput = lookAction.ReadValue<Vector2>();
        float rotationSpeed = 5f; // Adjust rotation speed as needed

        harpoonAnchor.Rotate(Vector3.up, lookInput.x * rotationSpeed * Time.deltaTime);
    }

    void ShootHarpoon()
    {
        if (loadedArrow != null)
        {
            // Instantiate the harpoon arrow prefab at the loaded arrow's position and rotation
            GameObject arrow = Instantiate(harpoonArrowPrefab, loadedArrow.position, loadedArrow.rotation);
            // Add force to the arrow to shoot it forward
            Rigidbody rb = arrow.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.AddForce(loadedArrow.forward * 1000f); // Adjust force as needed
            }

            // Destroy the loaded arrow after shooting
            Destroy(loadedArrow.gameObject);
            loadedArrow = null;
        }
    }
}
