using UnityEngine;

public class FireExtinguisher : BaseHeldItem
{
    [SerializeField] private float range = 10f;
    [SerializeField] private float extinguishAmount = 25f;
    [SerializeField] private AudioClip spraySound;
    [SerializeField] private ParticleSystem sprayParticles;

    private Camera mainCamera;
    private AudioSource audioSource;

    void Start()
    {
        base.Start();

        mainCamera = Camera.main;
        audioSource = GetComponent<AudioSource>();

        audioSource.clip = spraySound;
        audioSource.loop = true;

        sprayParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    public override void OnItemUse(GameObject player)
    {
        if (!audioSource.isPlaying)
            audioSource.Play();

        if (!sprayParticles.isPlaying)
            sprayParticles.Play();

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

        sprayParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    private void OnDisable()
    {
        if (audioSource != null)
            audioSource.Stop();

        if (sprayParticles != null)
            sprayParticles.Stop(true , ParticleSystemStopBehavior.StopEmittingAndClear);
    }
}