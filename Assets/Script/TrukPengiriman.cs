using UnityEngine;
using System.Collections;
using TMPro;

public class TrukPengiriman : MonoBehaviour
{
    [Header("Titik Jalur")]
    public Transform titikParkir;
    public Transform titikTujuan;
    public Transform titikSpawn;

    [Header("Durasi Animasi (detik)")]
    public float durasiAnimasiPergi = 4f;
    public float durasiAnimasiPulang = 4f;

    [Header("UI Informasi")]
    public TextMeshProUGUI teksInfoPerjalanan;

    [Header("Info Truk")]
    public string namaTruk = "Truk";

    // === VARIABEL UNTUK STATUS & EFEK ===
    private bool isDelivering = false;
    private bool isReadyToCollect = false;
    private float waktuTersisa = 0f;
    private Coroutine korutinSaatIni;
    private Renderer[] childRenderers;
    private Color originalColor;
    private float totalEstimasi = 0f;

    // === VARIABEL UNTUK ANIMASI PULSE ===
    private Coroutine animasiPulseCoroutine;
    private Color warnaKuningDasar = new Color(1f, 1f, 0f, 1f);   // Kuning penuh
    private Color warnaKuningRedup = new Color(0.5f, 0.5f, 0f, 1f); // Kuning 50% intensitas

    private System.Action onDeliveryComplete;
    private System.Action onComplete;

    void Awake()
    {
        // Ambil semua renderer anak
        childRenderers = GetComponentsInChildren<Renderer>();
        if (childRenderers.Length > 0)
        {
            originalColor = childRenderers[0].material.color;
        }
    }

    void Start()
    {
        if (titikParkir != null) transform.position = titikParkir.position;
        if (teksInfoPerjalanan != null) teksInfoPerjalanan.gameObject.SetActive(false);
    }

    void Update()
    {
        if (isDelivering && teksInfoPerjalanan != null && waktuTersisa > 0)
        {
            int detik = Mathf.CeilToInt(waktuTersisa);
            teksInfoPerjalanan.text = $"{namaTruk} : {detik} detik tersisa";
        }
        else if (isDelivering && teksInfoPerjalanan != null && waktuTersisa <= 0)
        {
            teksInfoPerjalanan.gameObject.SetActive(false);
        }
    }

    // === PROPERTI PUBLIK ===
    public bool IsDelivering => isDelivering;
    public float RemainingTime => waktuTersisa;

    public void SetNamaTruk(string nama) => namaTruk = nama;

    // === METODE UNTUK MENGATUR EFEK SIAP PANEN (dengan animasi pulse) ===
    public void SetReadyToCollect(bool ready)
    {
        isReadyToCollect = ready;

        if (childRenderers == null || childRenderers.Length == 0) return;

        if (ready)
        {
            // Mulai animasi pulse jika belum berjalan
            if (animasiPulseCoroutine != null) StopCoroutine(animasiPulseCoroutine);
            animasiPulseCoroutine = StartCoroutine(AnimasiPulse());
        }
        else
        {
            // Hentikan animasi, kembalikan warna ke original
            if (animasiPulseCoroutine != null)
            {
                StopCoroutine(animasiPulseCoroutine);
                animasiPulseCoroutine = null;
            }
            foreach (var r in childRenderers)
            {
                r.material.color = originalColor;
            }
        }
    }

    // === COROUTINE ANIMASI PULSE (fade in/out terus-menerus) ===
    IEnumerator AnimasiPulse()
    {
        float kecepatan = 1.5f; // bisa diatur sesuai selera

        while (true)
        {
            // Gunakan sin untuk osilasi halus antara 0 dan 1
            float t = Mathf.Sin(Time.time * kecepatan) * 0.5f + 0.5f;
            // Interpolasi antara kuning redup (50%) dan kuning terang (100%)
            Color warnaSaatIni = Color.Lerp(warnaKuningRedup, warnaKuningDasar, t);

            foreach (var r in childRenderers)
            {
                r.material.color = warnaSaatIni;
            }

            yield return null; // update setiap frame
        }
    }

    // === KLIK PADA TRUK ===
    void OnMouseDown()
    {
        if (isReadyToCollect && LogisticsManager.instance != null)
        {
            LogisticsManager.instance.AmbilRewardDariTruk();
        }
    }

    // === MEMULAI PENGIRIMAN ===
    public void MulaiPengiriman(float estimasiTotal, System.Action onArriveCallback, System.Action onCompleteCallback)
    {
        if (isDelivering)
        {
            Debug.LogWarning($"Truk {namaTruk} sedang sibuk!");
            return;
        }

        if (titikSpawn == null) Debug.LogError("titikSpawn NULL di TrukPengiriman!");
        if (titikParkir == null) Debug.LogError("titikParkir NULL di TrukPengiriman!");
        if (titikTujuan == null) Debug.LogError("titikTujuan NULL di TrukPengiriman!");

        // Reset status reward (matikan efek kuning)
        SetReadyToCollect(false);
        isReadyToCollect = false;

        totalEstimasi = estimasiTotal;
        waktuTersisa = estimasiTotal;
        isDelivering = true;
        onDeliveryComplete = onArriveCallback;
        onComplete = onCompleteCallback;
        if (korutinSaatIni != null) StopCoroutine(korutinSaatIni);
        korutinSaatIni = StartCoroutine(JalankanPengiriman());
    }

    IEnumerator JalankanPengiriman()
    {
        if (teksInfoPerjalanan != null) teksInfoPerjalanan.gameObject.SetActive(true);

        // FASE 1: Pergi
        yield return StartCoroutine(GerakKeTitik(titikTujuan.position, durasiAnimasiPergi));
        Debug.Log($"Truk {namaTruk} sampai di tujuan.");
        onDeliveryComplete?.Invoke();

        // FASE 2: Hilang
        float waktuTengah = totalEstimasi - durasiAnimasiPergi - durasiAnimasiPulang;
        if (waktuTengah > 0)
        {
            Debug.Log($"Truk {namaTruk} menghilang selama {waktuTengah:F2} detik.");
            SetTrukVisible(false);
            yield return new WaitForSeconds(waktuTengah);
            SetTrukVisible(true);
            if (titikSpawn != null)
                transform.position = titikSpawn.position;
            else
                transform.position = titikParkir.position;
            Debug.Log($"Truk {namaTruk} muncul di spawn.");
        }
        else
        {
            Debug.Log($"Truk {namaTruk} langsung pindah ke spawn (estimasi terlalu kecil).");
            if (titikSpawn != null) transform.position = titikSpawn.position;
            else if (titikParkir != null) transform.position = titikParkir.position;
        }

        // FASE 3: Pulang
        Debug.Log($"Truk {namaTruk} mulai pulang dari spawn ke parkir.");
        yield return StartCoroutine(GerakKeTitik(titikParkir.position, durasiAnimasiPulang));
        Debug.Log($"Truk {namaTruk} kembali ke parkir.");

        // Selesai
        isDelivering = false;
        korutinSaatIni = null;
        if (teksInfoPerjalanan != null) teksInfoPerjalanan.gameObject.SetActive(false);

        // Panggil callback onComplete (di sinilah reward akan disiapkan)
        onComplete?.Invoke();
    }

    IEnumerator GerakKeTitik(Vector3 target, float durasi)
    {
        float t = 0;
        Vector3 startPos = transform.position;
        while (t < 1)
        {
            t += Time.deltaTime / durasi;
            transform.position = Vector3.Lerp(startPos, target, t);
            waktuTersisa -= Time.deltaTime;
            yield return null;
        }
        transform.position = target;
    }

    private void SetTrukVisible(bool visible)
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (var r in renderers) r.enabled = visible;
        Collider[] colliders = GetComponentsInChildren<Collider>();
        foreach (var c in colliders) c.enabled = visible;
    }

    public void ResetState()
    {
        if (korutinSaatIni != null) StopCoroutine(korutinSaatIni);
        isDelivering = false;
        SetReadyToCollect(false);
        if (teksInfoPerjalanan != null) teksInfoPerjalanan.gameObject.SetActive(false);
    }
}