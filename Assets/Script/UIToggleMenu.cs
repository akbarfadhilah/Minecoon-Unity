using UnityEngine;
using System.Collections;

public class UIToggleMenu : MonoBehaviour
{
    [Header("Referensi UI")]
    [Tooltip("Masukkan RectTransform dari Button atau Panel Bar yang mau digerakkan")]
    public RectTransform targetUI;

    [Header("Pengaturan Posisi Y")]
    public float posYBawah = -206f;
    public float posYAtas = -135f;

    [Header("Pengaturan Animasi")]
    public float durasiAnimasi = 0.4f;

    [Tooltip("Wajib gunakan Vector2.LerpUnclamped di kode agar kurva yang melebihi angka 1 bisa membal (bounce)")]
    public AnimationCurve kurvaGerak = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private bool sedangDiAtas = false;
    private bool sedangBergerak = false;

    void Start()
    {
        // Jika targetUI lupa diisi di Inspector, otomatis mengambil RectTransform milik object ini
        if (targetUI == null)
        {
            targetUI = GetComponent<RectTransform>();
        }

        // Set posisi awal ke posisi bawah (-206)
        if (targetUI != null)
        {
            Vector2 pos = targetUI.anchoredPosition;
            targetUI.anchoredPosition = new Vector2(pos.x, posYBawah);
        }
    }

    // --- FUNGSI UTAMA (Pasang fungsi ini di OnClick() tombol kamu) ---
    public void TogglePosisi()
    {
        // Cegah player melakukan spam klik saat animasi sedang berjalan
        if (sedangBergerak) return;

        // Tentukan target tujuan berdasarkan status posisi saat ini
        float targetY = sedangDiAtas ? posYBawah : posYAtas;

        // Mulai animasi pergerakan
        StartCoroutine(AnimasikanGerakan(targetY));

        // Balikkan status posisi
        sedangDiAtas = !sedangDiAtas;
    }

    IEnumerator AnimasikanGerakan(float targetY)
    {
        sedangBergerak = true;

        Vector2 posisiAwal = targetUI.anchoredPosition;
        Vector2 posisiTarget = new Vector2(posisiAwal.x, targetY);

        float elapsed = 0f;

        while (elapsed < durasiAnimasi)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / durasiAnimasi;

            // Mengambil nilai evaluasi dari kurva di Inspector
            float nilaiKurva = kurvaGerak.Evaluate(t);

            // MENGGUNAKAN LerpUnclamped: 
            // Agar jika kurva bernilai di atas 1.0 atau di bawah 0.0, UI-nya bisa bergerak melebihi target sebentar (efek membal/overshoot)
            targetUI.anchoredPosition = Vector2.LerpUnclamped(posisiAwal, posisiTarget, nilaiKurva);

            yield return null;
        }

        // Pastikan di frame terakhir posisinya benar-benar akurat di target
        targetUI.anchoredPosition = posisiTarget;

        sedangBergerak = false;
    }
}