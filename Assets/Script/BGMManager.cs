using UnityEngine;
using System.Collections;

public class BGMManager : MonoBehaviour
{
    public static BGMManager instance;

    [Header("Audio Sources (akan dibuat otomatis jika kosong)")]
    public AudioSource bgmSource1;
    public AudioSource bgmSource2;

    [Header("Default BGM")]
    public AudioClip bgmUtama;      // suara alam / main menu

    private AudioSource activeSource;
    private AudioSource inactiveSource;
    private Coroutine fadeCoroutine;

    private float currentVolume = 0.5f; // volume default

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Buat dua audio source jika belum di-assign
        if (bgmSource1 == null) bgmSource1 = gameObject.AddComponent<AudioSource>();
        if (bgmSource2 == null) bgmSource2 = gameObject.AddComponent<AudioSource>();

        bgmSource1.loop = true;
        bgmSource2.loop = true;
        bgmSource1.volume = 0;
        bgmSource2.volume = 0;
        bgmSource1.playOnAwake = false;
        bgmSource2.playOnAwake = false;

        activeSource = bgmSource1;
        inactiveSource = bgmSource2;
        PlayBGM(bgmUtama, 0.5f);
    }

    /// <summary>
    /// Ganti BGM dengan fade durasi tertentu
    /// </summary>
    public void ChangeBGM(AudioClip newClip, float fadeDuration = 1f)
    {
        if (newClip == null) return;
        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        fadeCoroutine = StartCoroutine(FadeToNewClip(newClip, fadeDuration));
    }

    public void SetVolume(float vol)
    {
        currentVolume = Mathf.Clamp01(vol);
        // Jika sedang aktif, terapkan langsung
        if (activeSource != null && activeSource.isPlaying)
            activeSource.volume = currentVolume;
    }

    IEnumerator FadeToNewClip(AudioClip newClip, float duration)
    {
        // Fade out active source
        float elapsed = 0;
        float startVol = activeSource.volume;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            activeSource.volume = Mathf.Lerp(startVol, 0, t);
            yield return null;
        }
        activeSource.volume = 0;
        activeSource.Stop();

        // Tukar active/inactive
        AudioSource temp = activeSource;
        activeSource = inactiveSource;
        inactiveSource = temp;

        // Set clip baru ke active source
        activeSource.clip = newClip;
        activeSource.Play();

        // Fade in active source
        elapsed = 0;
        float targetVol = currentVolume;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            activeSource.volume = Mathf.Lerp(0, targetVol, t);
            yield return null;
        }
        activeSource.volume = targetVol;
        fadeCoroutine = null;
    }

    /// <summary>
    /// Langsung play tanpa fade
    /// </summary>
    public void PlayBGM(AudioClip clip, float volume = 0.5f)
    {
        if (clip == null) return;
        activeSource.Stop();
        activeSource.clip = clip;
        activeSource.volume = volume;
        activeSource.Play();
        inactiveSource.Stop();
    }
}