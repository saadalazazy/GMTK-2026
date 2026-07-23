using UnityEngine;

public class FireExtinguisher : BaseHeldItem
{
    [SerializeField] private float range = 10f;
    [SerializeField] private float extinguishAmount = 25f;
    [SerializeField] private AudioClip spraySound;

    Camera mainCamera;
    AudioSource audioSource;

    void Start()
    {
        base.Start();
        mainCamera = Camera.main;
        audioSource = GetComponent<AudioSource>();
        audioSource.clip = spraySound;
        audioSource.loop = true;
    }

    public override void OnItemUse(GameObject player)
    {
        if (!audioSource.isPlaying)
            audioSource.Play();

        Ray ray = new Ray(mainCamera.transform.position, mainCamera.transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, range))
        {
            Fire fire = hit.collider.GetComponent<Fire>();
            if (fire != null)
                fire.Extinguish(extinguishAmount * Time.deltaTime);
        }
    }

    public override void OnItemRelease(GameObject player)
    {
        audioSource.Stop();
    }
}
