using UnityEngine;
using System.Collections;

public class IntroCanvasManager : MonoBehaviour
{
    public CanvasGroup panelIntro;
    public float durasiVideo = 5f; // Ganti sesuai panjang durasi video intro abang
    public float durasiFade = 1f;  // Waktu transisi memudar (1 detik)

    void Start()
    {
        // 1. Pastikan layar intro terlihat pekat menutupi menu di awal (alpha 1)
        panelIntro.alpha = 1f;

        // 2. Mulai hitung mundur
        StartCoroutine(TungguDanFadeOut());
    }

    IEnumerator TungguDanFadeOut()
    {
        // 3. Diam dan biarkan video berputar selama 5 detik
        yield return new WaitForSeconds(durasiVideo);

        // 4. Buat panel intro perlahan memudar (menjadi transparan / alpha 0)
        LeanTween.alphaCanvas(panelIntro, 0f, durasiFade).setOnComplete(() =>
        {
            // 5. Setelah benar-benar memudar, kita nonaktifkan panelnya 
            // agar tombol-tombol menu di belakangnya bisa diklik oleh player.
            panelIntro.gameObject.SetActive(false);
        });
    }
}