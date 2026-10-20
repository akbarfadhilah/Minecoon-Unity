using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;
using System.Collections.Generic;

public class PekerjaMesin : MonoBehaviour
{
    public enum StatusPekerja { Idle, MenujuMesin, Bekerja, Pulang }

    [Header("===== IDENTITAS =====")]
    public string namaPekerja = "Operator Mesin";
    public Sprite fotoPekerja;

    [Header("Statistik Pekerja")]
    public float durasiPotongKayu = 5f;
    public float durasiPapanHalus = 5f;
    public int biayaSewa = 100;
    public float durasiLamaBekerja = 120f;

    [Header("Status Saat Ini")]
    public StatusPekerja status = StatusPekerja.Idle;
    public bool isAktif = false;
    public float sisaWaktuKerja = 0f;
    public Vector3 posisiAwal;

    [Header("UI Info Pekerja")]
    public TextMeshProUGUI teksInfoPekerja;
    public TextMeshProUGUI teksInfoBiaya;
    public TextMeshProUGUI teksInfoLamaKontrak;
    public Image gambarPekerja;

    private NavMeshAgent agen;
    private Animator anim;
    private MesinPabrik targetMesin;
    private Renderer[] semuaRenderer;

    [Header("Pengaturan Pergerakan")]
    public float kecepatanJalan = 1.5f;
    public float kecepatanPutar = 360f;
    public float toleransiSudut = 5f;
    public float kecepatanAnimasi = 0.7f;

    [Header("Pengaturan Idle Dinamis")]
    public Transform[] titikKursi;
    public float durasiAktivitas = 5f;
    private float timerAktivitas = 0f;

    public enum AktivitasIdle { Milih, JalanRandom, Pemanasan, MenujuKursi, SedangDuduk, SedangBerdiri }
    public AktivitasIdle statusIdle = AktivitasIdle.Milih;
    private Transform kursiTujuan;
    private int urutanAktivitas = 0;
    private int lastChoice = -1;
    private int repeatCount = 0;

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

        if (teksInfoBiaya != null) teksInfoBiaya.text = $"Rp. {biayaSewa}";
        if (teksInfoLamaKontrak != null) teksInfoLamaKontrak.text = $"{durasiLamaBekerja}s";
        if (teksInfoPekerja != null) teksInfoPekerja.text = namaPekerja;
        if (gambarPekerja != null && fotoPekerja != null)
        {
            gambarPekerja.sprite = fotoPekerja;
            gambarPekerja.enabled = true;
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

    void Update()
    {
        // Logika kerja
        if (status == StatusPekerja.MenujuMesin)
        {
            if (agen.isActiveAndEnabled && agen.isOnNavMesh)
            {
                if (!agen.pathPending && agen.remainingDistance <= agen.stoppingDistance)
                    MulaiBekerjaDalamMesin();
            }
        }
        else if (status == StatusPekerja.Bekerja)
        {
            sisaWaktuKerja -= Time.deltaTime;
            if (sisaWaktuKerja <= 0) SelesaiBekerja();
        }
        else if (status == StatusPekerja.Idle)
        {
            // Idle dinamis
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
                if (!agen.pathPending && agen.remainingDistance <= agen.stoppingDistance)
                {
                    urutanAktivitas++;
                    if (urutanAktivitas > 2) urutanAktivitas = 0;
                    statusIdle = AktivitasIdle.Milih;
                }
            }
            else if (statusIdle == AktivitasIdle.MenujuKursi)
            {
                if (!agen.pathPending && agen.remainingDistance <= agen.stoppingDistance)
                {
                    if (agen.isActiveAndEnabled && agen.isOnNavMesh)
                    {
                        agen.isStopped = true;
                        agen.ResetPath();
                        agen.velocity = Vector3.zero;
                    }
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
        }

        AturPergerakanDanRotasi();

        // Update UI
        if (isAktif)
        {
            if (teksInfoPekerja != null) teksInfoPekerja.text = $"{namaPekerja}";
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

    public void SewaPekerja()
    {
        if (isAktif) return;

        if (EconomyManager.instance.koin >= biayaSewa)
        {
            MesinPabrik mesinTersedia = CariMesinKosong();
            if (mesinTersedia != null)
            {
                EconomyManager.instance.koin -= biayaSewa;
                targetMesin = mesinTersedia;
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
            else Debug.Log("Tidak ada mesin potong kayu yang kosong!");
        }
        else Debug.Log("Koin tidak cukup untuk sewa pekerja ini!");
    }

    MesinPabrik CariMesinKosong()
    {
        MesinPabrik[] semuaMesin = FindObjectsOfType<MesinPabrik>();
        foreach (MesinPabrik mesin in semuaMesin)
        {
            if (mesin.jenisMesin == MesinPabrik.Jenis.PotongKayu && mesin.jumlahPekerjaAktif < mesin.maxPekerja)
                return mesin;
        }
        return null;
    }

    void MulaiBekerjaDalamMesin()
    {
        status = StatusPekerja.Bekerja;
        foreach (Renderer r in semuaRenderer) r.enabled = false;
        agen.enabled = false;
        targetMesin.TambahPekerjaMesin(this);
    }

    void SelesaiBekerja()
    {
        foreach (Renderer r in semuaRenderer) r.enabled = true;
        agen.enabled = true;
        agen.Warp(transform.position);
        if (targetMesin != null)
        {
            targetMesin.KurangiPekerja();
            targetMesin = null;
        }
        status = StatusPekerja.Idle;
        isAktif = false;
        statusIdle = AktivitasIdle.Milih;
    }

    IEnumerator TungguAnimasiBerdiri()
    {
        yield return new WaitForSeconds(5f);
        statusIdle = AktivitasIdle.Milih;
    }
}