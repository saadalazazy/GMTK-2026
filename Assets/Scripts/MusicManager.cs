using UnityEngine;

public class MusicManager : MonoBehaviour
{
    [Header("Music")]
    public AudioClip musicClip;
    [Range(0f, 1f)]
    public float volume = 0.5f;

    private AudioSource audioSource;

    void Start()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.clip = musicClip;
        audioSource.loop = true;
        audioSource.volume = volume;
        audioSource.playOnAwake = false;
        audioSource.Play();
    }

    public void SetVolume(float newVolume)
    {
        volume = Mathf.Clamp01(newVolume);
        audioSource.volume = volume;
    }
}
