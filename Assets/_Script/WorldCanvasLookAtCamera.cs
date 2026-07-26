using UnityEngine;

public class WorldCanvasLookAtCamera : MonoBehaviour
{
    private Camera playerCamera;

    private void Start()
    {
        playerCamera = Camera.main;
    }

    private void LateUpdate()
    {
        if (playerCamera == null)
            return;

        transform.LookAt(
            transform.position + playerCamera.transform.forward,
            playerCamera.transform.up);
        transform.Rotate(0f, 180f, 0f);
    }
}
