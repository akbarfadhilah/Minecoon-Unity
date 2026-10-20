using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

public class PekerjaMebel : MonoBehaviour
{
    private Animator anim;
    public enum StatusPekerja { Idle, MenujuMesin, Bekerja, Pulang }

    [Header("===== IDENTITAS =====")]
    public string namaPekerja = "Tukang Mebel";
    public Sprite fotoPekerja;

    [Header("Statistik Dasar")]
    public int biayaSewa = 200;
    public float durasiLamaBekerja = 150f;

    [Header("Kecepatan Kerja")]
    public float durasiKursi = 8f;
    public float durasiMeja = 12f;

    [Header("Status")]
    public StatusPekerja status = StatusPekerja.Idle;
    public bool isAktif = false;
    public float sisaWaktuKerja = 0f;

    [Header("UI Info Pekerja")]
    public TextMeshProUGUI teksInfoPekerja;
    public TextMeshProUGUI teksInfoBiaya;
    public TextMeshProUGUI teksInfoLamaKontrak;
    public Image gambarPekerja;

    // ===== IDLE DINAMIS =====
    [Header("Pengaturan Pergerakan (Stop & Turn)")]
    public float kecepatanJalan = 1.5f;
    public float kecepatanPutar = 360f;
    public float toleransiSudut = 5f;
    public float kecepatanAnimasi = 0.7f;

    [Header("Pengaturan Idle Dinamis")]
    public Transform[] titikKursi;
    public float durasiAktivitas = 5f;

    public enum AktivitasIdle { Milih, JalanRandom, Pemanasan, MenujuKursi, SedangDuduk, SedangBerdiri }
    public AktivitasIdle statusIdle = AktivitasIdle.Milih;
    private float timerAktivitas = 0f;
    private Transform kursiTujuan;
    private int urutanAktivitas = 0;
    private int lastChoice = -1;
    private int repeatCount = 0;

    private NavMeshAgent agen;
    private Vector3 posisiAwal;
    private MesinPabrik targetMesin;
    private Renderer[] semuaRenderer;

    void Start()
    {
        status = StatusPekerja.Idle;
        targetMesin = null;
        isAktif = false;

        agen = GetComponent<NavMeshAgent>();
        anim = GetComponent<Animator>();
        posisiAwal = transform.position;
        semuaRenderer = GetComponentsInChildren<Renderer>();

        if (agen != null)
            agen.updateRotation = false;

        // ===== SET DEFAULT UI =====
        if (teksInfoBiaya != null) teksInfoBiaya.text = $"Rp. {biayaSewa}";
        if (teksInfoLamaKontrak != null) teksInfoLamaKontrak.text = $"{durasiLamaBekerja}s";
        if (teksInfoPekerja != null) teksInfoPekerja.text = namaPekerja;
        if (gambarPekerja != null && fotoPekerja != null)
        {
            gambarPekerja.sprite = fotoPekerja;
            gambarPekerja.enabled = true;
        }
    }

    void Update()
    {
        // === LOGIKA KERJA (jika aktif) ===
        if (isAktif)
        {
            if (status == StatusPekerja.MenujuMesin)
            {
                if (agen.isActiveAndEnabled && agen.isOnNavMesh)
                {
                    if (!agen.pathPending && agen.remainingDistance <= agen.stoppingDistance)
                        MulaiMasukMesin();
                }
            }
            else if (status == StatusPekerja.Bekerja)
            {
                sisaWaktuKerja -= Time.deltaTime;
                if (sisaWaktuKerja <= 0) SelesaiKontrak();
            }
            else if (status == StatusPekerja.Pulang)
            {
                if (agen.isActiveAndEnabled && agen.isOnNavMesh)
                {
                    if (!agen.pathPending && agen.remainingDistance <= agen.stoppingDistance)
                    {
                        status = StatusPekerja.Idle;
                        isAktif = false;
                        statusIdle = AktivitasIdle.Milih;
                    }
                }
            }
        }

        // === LOGIKA IDLE DINAMIS ===
        if (status == StatusPekerja.Idle)
        {
            if (statusIdle == AktivitasIdle.Milih)
            {
                int pilihan = Random.Range(0, 3);
                if (pilihan == lastChoice && repeatCount >= 2)
                    pilihan = (pilihan + 1) % 3;

                if (pilihan == lastChoice) repeatCount++;
                else { lastChoice = pilihan; repeatCount = 1; }

                if (pilihan == 0)
                {
                    Vector3 titikRandom = DapatkanTitikRandom(transform.position, 8f);
                    if (agen.isActiveAndEnabled && agen.isOnNavMesh) { agen.isStopped = false; agen.SetDestination(titikRandom); }
                    statusIdle = AktivitasIdle.JalanRandom;
                }
                else if (pilihan == 1)
                {
                    if (agen.isActiveAndEnabled && agen.isOnNavMesh) { agen.isStopped = true; agen.ResetPath(); agen.velocity = Vector3.zero; }
                    if (anim != null) anim.SetBool("isPemanasan", true);
                    timerAktivitas = durasiAktivitas;
                    statusIdle = AktivitasIdle.Pemanasan;
                }
                else if (pilihan == 2)
                {
                    if (titikKursi.Length > 0)
                    {
                        kursiTujuan = titikKursi[Random.Range(0, titikKursi.Length)];
                        if (agen.isActiveAndEnabled && agen.isOnNavMesh) { agen.isStopped = false; agen.SetDestination(kursiTujuan.position); }
                        statusIdle = AktivitasIdle.MenujuKursi;
                    }
                    else statusIdle = AktivitasIdle.Milih;
                }
            }
            else if (statusIdle == AktivitasIdle.JalanRandom)
            {
                if (agen.isActiveAndEnabled && agen.isOnNavMesh && !agen.pathPending && agen.remainingDistance <= agen.stoppingDistance)
                {
                    urutanAktivitas++;
                    if (urutanAktivitas > 2) urutanAktivitas = 0;
                    statusIdle = AktivitasIdle.Milih;
                }
            }
            else if (statusIdle == AktivitasIdle.MenujuKursi)
            {
                if (agen.isActiveAndEnabled && agen.isOnNavMesh && !agen.pathPending && agen.remainingDistance <= agen.stoppingDistance)
                {
                    agen.isStopped = true;
                    agen.ResetPath();
                    agen.velocity = Vector3.zero;
                    transform.position = kursiTujuan.position;
                    transform.rotation = kursiTujuan.rotation;
                    if (anim != null) anim.SetBool("isDuduk", true);
                    timerAktivitas = durasiAktivitas;
                    statusIdle = AktivitasIdle.SedangDuduk;
                }
            }
            else if (statusIdle == AktivitasIdle.SedangDuduk)
            {
                timerAktivitas -= Time.deltaTime;
                if (timerAktivitas <= 0)
                {
                    if (anim != null) anim.SetBool("isDuduk", false);
                    StartCoroutine(TungguAnimasiBerdiri());
                    statusIdle = AktivitasIdle.SedangBerdiri;
                    urutanAktivitas++;
                    if (urutanAktivitas > 2) urutanAktivitas = 0;
                }
            }
            else if (statusIdle == AktivitasIdle.Pemanasan)
            {
                timerAktivitas -= Time.deltaTime;
                if (timerAktivitas <= 0)
                {
                    if (anim != null) anim.SetBool("isPemanasan", false);
                    statusIdle = AktivitasIdle.Milih;
                    urutanAktivitas++;
                    if (urutanAktivitas > 2) urutanAktivitas = 0;
                }
            }
            else if (statusIdle == AktivitasIdle.SedangBerdiri)
            {
                // Tunggu coroutine selesai
            }
        }

        // === PANGGIL ATUR PERGERAKAN ===
        AturPergerakanDanRotasi();

        // === UPDATE UI ===
        if (isAktif)
        {
            if (teksInfoPekerja != null)
                teksInfoPekerja.text = $"{namaPekerja}";
            if (gambarPekerja != null && fotoPekerja != null)
            {
                gambarPekerja.sprite = fotoPekerja;
                gambarPekerja.enabled = true;
            }
            if (teksInfoBiaya != null) teksInfoBiaya.text = $"{sisaWaktuKerja.ToString("F0")}s";
            if (teksInfoLamaKontrak != null) teksInfoLamaKontrak.text = "BEKERJA";
        }
        else
        {
            // ===== PERBAIKAN: tampilkan nama dan foto saat idle =====
            if (teksInfoPekerja != null) teksInfoPekerja.text = namaPekerja;
            if (gambarPekerja != null)
            {
                if (fotoPekerja != null)
                {
                    gambarPekerja.sprite = fotoPekerja;
                    gambarPekerja.enabled = true;
                }
                else
                {
                    gambarPekerja.enabled = false;
                }
            }
            if (teksInfoBiaya != null) teksInfoBiaya.text = $"Rp. {biayaSewa}";
            if (teksInfoLamaKontrak != null) teksInfoLamaKontrak.text = $"{durasiLamaBekerja}s";   
        }
    }

    void AturPergerakanDanRotasi()
    {
        if (agen.isActiveAndEnabled && agen.isOnNavMesh && agen.hasPath)
        {
            Vector3 arahTujuan = agen.steeringTarget - transform.position;
            arahTujuan.y = 0;
            if (arahTujuan.sqrMagnitude > 0.01f)
            {
                Quaternion rotasiTujuan = Quaternion.LookRotation(arahTujuan);
                float sudut = Quaternion.Angle(transform.rotation, rotasiTujuan);
                if (sudut > toleransiSudut)
                {
                    agen.isStopped = true;
                    transform.rotation = Quaternion.RotateTowards(transform.rotation, rotasiTujuan, kecepatanPutar * Time.deltaTime);
                    if (anim != null) anim.SetBool("isWalking", false);
                }
                else
                {
                    agen.isStopped = false;
                    agen.speed = kecepatanJalan;
                    if (anim != null) { anim.SetBool("isWalking", true); anim.speed = kecepatanAnimasi; }
                }
            }
        }
        else
        {
            if (anim != null) { anim.SetBool("isWalking", false); anim.speed = 1f; }
        }
    }

    public Vector3 DapatkanTitikRandom(Vector3 pusat, float radiusArea)
    {
        Vector3 arahRandom = Random.insideUnitSphere * radiusArea;
        arahRandom += pusat;
        NavMeshHit hit;
        if (NavMesh.SamplePosition(arahRandom, out hit, radiusArea, NavMesh.AllAreas))
            return hit.position;
        return pusat;
    }

    public void MulaiKerja()
    {
        if (isAktif) return;

        MesinPabrik mesinMebel = CariMesinMebel();
        if (mesinMebel == null)
        {
            Debug.Log("PEKERJA MEBEL BINGUNG: Tidak ada pabrik mebel yang kosong / belum dibangun!");
            return;
        }

        if (EconomyManager.instance.koin >= biayaSewa)
        {
            EconomyManager.instance.koin -= biayaSewa;
            targetMesin = mesinMebel;
            isAktif = true;
            sisaWaktuKerja = durasiLamaBekerja;
            status = StatusPekerja.MenujuMesin;

            if (anim != null)
            {
                anim.SetBool("isPemanasan", false);
                anim.SetBool("isDuduk", false);
                anim.SetBool("isWalking", false);
                anim.Play("HumanF@Idle01", 0, 0f);
            }
            StopAllCoroutines();
            statusIdle = AktivitasIdle.Milih;

            agen.enabled = true;
            agen.Warp(transform.position);
            if (agen.isOnNavMesh)
                agen.SetDestination(targetMesin.transform.position);
            else
                Debug.LogError($"[ERROR] {gameObject.name} melayang atau lantai belum di-Bake!");
        }
        else
        {
            Debug.Log("DUIT KURANG: Koin tidak cukup untuk sewa Pekerja Mebel!");
        }
    }

    MesinPabrik CariMesinMebel()
    {
        MesinPabrik[] semua = FindObjectsByType<MesinPabrik>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (MesinPabrik m in semua)
            if (m.jenisMesin == MesinPabrik.Jenis.Mebel && m.jumlahPekerjaAktif < m.maxPekerja)
                return m;
        return null;
    }

    void MulaiMasukMesin()
    {
        status = StatusPekerja.Bekerja;
        foreach (Renderer r in semuaRenderer) r.enabled = false;
        agen.enabled = false;
        targetMesin.TambahPekerjaMebel(this);
    }

    void SelesaiKontrak()
    {
        status = StatusPekerja.Pulang;
        targetMesin.KurangiPekerjaMebel(this);
        foreach (Renderer r in semuaRenderer) r.enabled = true;
        agen.enabled = true;
        agen.Warp(transform.position);
        if (agen.isOnNavMesh)
            agen.SetDestination(posisiAwal);
        else
            Debug.LogError($"[ERROR] {gameObject.name} tidak bisa pulang karena NavMesh error!");
    }

    IEnumerator TungguAnimasiBerdiri()
    {
        yield return new WaitForSeconds(5f);
        statusIdle = AktivitasIdle.Milih;
    }
}