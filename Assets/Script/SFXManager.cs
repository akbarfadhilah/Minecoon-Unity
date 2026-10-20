using UnityEngine;

public class SFXManager : MonoBehaviour
{
    public static SFXManager instance;
    private AudioSource audioSource;

    private float currentVolume = 0.5f;

    void Awake()
    {
        instance = this;
        audioSource = GetComponent<AudioSource>();
    }

    public void PlaySound(AudioClip clip)
    {
        if (clip != null && audioSource != null)
            audioSource.PlayOneShot(clip, currentVolume);
    }

    public void SetVolume(float vol)
    {
        currentVolume = Mathf.Clamp01(vol);
    }
}