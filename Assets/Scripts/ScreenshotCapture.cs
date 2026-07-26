using UnityEngine;
using UnityEngine.InputSystem;

public class ScreenshotCapture : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private int superSize = 2;
    [SerializeField] private string folder = "Screenshots";

    private InputAction screenshotAction;

    void Start()
    {
        var gameplay = FindFirstObjectByType<PlayerCore>();
        if (gameplay == null) return;

        var actions = gameplay.actions;
        var map = actions.FindActionMap("Player");
        screenshotAction = map.FindAction("Interact");
    }

    void Update()
    {
        if (screenshotAction != null && screenshotAction.WasPressedThisFrame())
        {
            TakeScreenshot();
        }
    }

    void TakeScreenshot()
    {
        string path = System.IO.Path.Combine(Application.persistentDataPath, folder);
        System.IO.Directory.CreateDirectory(path);

        string filename = $"screenshot_{System.DateTime.Now:yyyyMMdd_HHmmss}.png";
        string fullPath = System.IO.Path.Combine(path, filename);

        ScreenCapture.CaptureScreenshot(fullPath, superSize);
        Debug.Log($"Screenshot saved: {fullPath}");
    }
}
