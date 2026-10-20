using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using System.Collections;

public class IntroManager : MonoBehaviour
{
    [Header("Referensi UI")]
    public RawImage videoRawImage;
    public VideoPlayer videoPlayer;

    [Header("Pengaturan Fade")]
    public float fadeOutDuration = 1f;   // durasi fade out (detik)

    [Header("Scene Tujuan (Opsional)")]
    public string nextSceneName = "";    // kosongkan jika tidak pindah scene

    private bool isFadingOut = false;
    private bool isVideoFinished = false;

    void Start()
    {
        // Pastikan RawImage terlihat
        if (videoRawImage != null)
        {
            Color c = videoRawImage.color;
            c.a = 1f;
            videoRawImage.color = c;
        }

        // Subscribe event ketika video selesai
        if (videoPlayer != null)
        {
            videoPlayer.loopPointReached += OnVideoFinished;
            videoPlayer.Play();
        }
        else
        {
            Debug.LogError("VideoPlayer tidak ditemukan!");
        }
    }

    void Update()
    {
        // Opsional: jika player klik di mana saja, bisa langsung skip intro
        // if (Input.anyKeyDown && !isFadingOut)
        // {
        //     SkipIntro();
        // }
    }

    void OnVideoFinished(VideoPlayer vp)
    {
        isVideoFinished = true;
        StartCoroutine(FadeOutAndClose());
    }

    IEnumerator FadeOutAndClose()
    {
        if (isFadingOut) yield break;
        isFadingOut = true;

        float elapsed = 0f;
        Color color = videoRawImage.color;

        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsed / fadeOutDuration);
            color.a = alpha;
            videoRawImage.color = color;
            yield return null;
        }

        // Pastikan alpha 0
        color.a = 0f;
        videoRawImage.color = color;

        // Tutup intro atau pindah scene
        if (!string.IsNullOrEmpty(nextSceneName))
        {
            // Jika ada scene tujuan, load scene
            UnityEngine.SceneManagement.SceneManager.LoadScene(nextSceneName);
        }
        else
        {
            // Jika tidak, nonaktifkan objek intro
            gameObject.SetActive(false);
        }
    }

    // Fungsi untuk skip intro (jika diinginkan)
    public void SkipIntro()
    {
        if (!isFadingOut && !isVideoFinished)
        {
            // Langsung lompat ke akhir
            StopAllCoroutines();
            Color c = videoRawImage.color;
            c.a = 0f;
            videoRawImage.color = c;
            if (!string.IsNullOrEmpty(nextSceneName))
                UnityEngine.SceneManagement.SceneManager.LoadScene(nextSceneName);
            else
                gameObject.SetActive(false);
        }
    }
}