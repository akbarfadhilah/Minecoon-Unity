using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

public class PekerjaPetaniPohonJati : MonoBehaviour
{
    public enum StatusPekerja { Idle, MenujuLahan, Menanam, MenujuPohon, Menebang, Pulang }

    [Header("===== IDENTITAS =====")]
    public string namaPekerja = "Petani Jati";
    public Sprite fotoPekerja;

    [Header("Sistem Kontrak")]
    public bool isAktif = false;
    public float sisaWaktuKerja = 0f;
    public Vector3 posisiAwal;

    [Header("Statistik")]
    public float durasiMenanam = 2f;
    public float durasiMenebang = 3f;
    public int biayaSewa = 100;
    public float lamaKontrak = 120f;

    [Header("Pengaturan Animasi")]
    public float panjangAsliAnimasiNanam = 2.5f;
    public float panjangAsliAnimasiNebang = 2.0f;
    public GameObject prefabPohon;

    [Header("Status")]
    public StatusPekerja status = StatusPekerja.Idle;

    [Header("UI Info Pekerja")]
    public TextMeshProUGUI teksInfoPekerja;
    public TextMeshProUGUI teksInfoBiaya;
    public TextMeshProUGUI teksInfoLamaKontrak;
    public Image gambarPekerja;

    // ===== IDLE DINAMIS =====
    [Header("Pengaturan Pergerakan")]
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
    private Animator anim;
    private Transform targetLahan;
    private GameObject targetPohon;

    void Start()
    {
        status = StatusPekerja.Idle;
        isAktif = false;

        agen = GetComponent<NavMeshAgent>();
        anim = GetComponent<Animator>();
        posisiAwal = transform.position;

        if (agen != null)
        {
            agen.updateRotation = false;
            agen.stoppingDistance = 0.5f;
        }

        if (teksInfoBiaya != null) teksInfoBiaya.text = $"Rp. {biayaSewa}";
        if (teksInfoLamaKontrak != null) teksInfoLamaKontrak.text = $"{lamaKontrak}s";
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
            if (status != StatusPekerja.Idle && status != StatusPekerja.Pulang)
            {
                sisaWaktuKerja -= Time.deltaTime;
                if (sisaWaktuKerja <= 0) PulangKeBase();
            }

            if (status == StatusPekerja.MenujuLahan || status == StatusPekerja.MenujuPohon || status == StatusPekerja.Pulang)
            {
                HandleGerakanGrid();
            }

            if ((status == StatusPekerja.MenujuLahan || status == StatusPekerja.MenujuPohon) && targetLahan == null)
            {
                status = StatusPekerja.Idle;
            }

            if (status == StatusPekerja.Idle)
            {
                if (!CariPohonSiapPanen()) CariLahanKosong();
            }
            else if (status == StatusPekerja.MenujuLahan)
            {
                if (agen.isActiveAndEnabled && agen.isOnNavMesh && SudahSampai()) MulaiMenanam();
            }
            else if (status == StatusPekerja.MenujuPohon)
            {
                if (agen.isActiveAndEnabled && agen.isOnNavMesh && SudahSampai()) MulaiMenebang();
            }
            else if (status == StatusPekerja.Pulang)
            {
                if (agen.isActiveAndEnabled && agen.isOnNavMesh && SudahSampai())
                {
                    isAktif = false;
                    status = StatusPekerja.Idle;
                    statusIdle = AktivitasIdle.Milih;
                }
            }
        }

        // === LOGIKA IDLE DINAMIS (hanya saat tidak aktif dan status Idle) ===
        if (status == StatusPekerja.Idle && !isAktif)
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

            AturPergerakanDanRotasi();
        }

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
            if (teksInfoLamaKontrak != null) teksInfoLamaKontrak.text = $"{lamaKontrak}s";
        }
    }

    // ===== FUNGSI UNTUK IDLE DINAMIS =====
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

    // ===== LOGIKA KERJA PERTANIAN =====
    public void PanggilPekerja()
    {
        if (isAktif) return;

        if (EconomyManager.instance.KurangiKoin(biayaSewa))
        {
            isAktif = true;
            sisaWaktuKerja = lamaKontrak;
            status = StatusPekerja.Idle;

            if (anim != null)
            {
                anim.SetBool("isPemanasan", false);
                anim.SetBool("isDuduk", false);
                anim.SetBool("isWalking", false);
                anim.Play("HumanF@Idle01", 0, 0f);
            }
            StopAllCoroutines();
            statusIdle = AktivitasIdle.Milih;

            Debug.Log("Pekerja disewa!");
        }
        else
        {
            Debug.Log("Koin tidak cukup!");
        }
    }

    void HandleGerakanGrid()
    {
        if (agen == null || !agen.isActiveAndEnabled || !agen.isOnNavMesh) return;

        bool punyaTujuan = agen.hasPath && agen.remainingDistance > agen.stoppingDistance;

        if (punyaTujuan)
        {
            Vector3 arahTujuan = agen.steeringTarget - transform.position;
            arahTujuan.y = 0;

            if (arahTujuan.sqrMagnitude > 0.01f)
            {
                Quaternion rotasiTarget = Quaternion.LookRotation(arahTujuan);
                float sudut = Quaternion.Angle(transform.rotation, rotasiTarget);

                if (sudut > 10f)
                {
                    agen.speed = 0f;
                    anim.SetBool("isWalking", false);
                    transform.rotation = Quaternion.RotateTowards(transform.rotation, rotasiTarget, 400f * Time.deltaTime);
                }
                else
                {
                    agen.speed = 3.5f;
                    anim.SetBool("isWalking", true);
                    transform.rotation = Quaternion.RotateTowards(transform.rotation, rotasiTarget, 400f * Time.deltaTime);
                }
            }
        }
        else
        {
            anim.SetBool("isWalking", false);
            agen.speed = 3.5f;
        }
    }

    bool SudahSampai()
    {
        return !agen.pathPending && agen.remainingDistance <= agen.stoppingDistance;
    }

    void PulangKeBase()
    {
        status = StatusPekerja.Pulang;
        if (agen.isActiveAndEnabled && agen.isOnNavMesh)
            agen.SetDestination(posisiAwal);
        else
            Debug.LogError($"[ERROR] {gameObject.name} tidak bisa pulang!");
    }

    bool CariPohonSiapPanen()
    {
        GameObject[] semuaLahan = GameObject.FindGameObjectsWithTag("Lahan");
        foreach (GameObject lahan in semuaLahan)
        {
            PohonScript pohon = lahan.GetComponentInChildren<PohonScript>();
            if (pohon != null && pohon.siapPanen)
            {
                targetLahan = lahan.transform;
                targetPohon = pohon.gameObject;
                if (agen.isActiveAndEnabled && agen.isOnNavMesh)
                    agen.SetDestination(targetLahan.position);
                else
                    return false;
                status = StatusPekerja.MenujuPohon;
                return true;
            }
        }
        return false;
    }

    void CariLahanKosong()
    {
        GameObject[] semuaLahan = GameObject.FindGameObjectsWithTag("Lahan");
        foreach (GameObject lahan in semuaLahan)
        {
            if (lahan.GetComponentInChildren<PohonScript>() == null)
            {
                targetLahan = lahan.transform;
                if (agen.isActiveAndEnabled && agen.isOnNavMesh)
                    agen.SetDestination(targetLahan.position);
                else
                    return;
                status = StatusPekerja.MenujuLahan;
                return;
            }
        }
    }

    void MulaiMenanam()
    {
        if (targetLahan == null) { status = StatusPekerja.Idle; return; }
        status = StatusPekerja.Menanam;
        transform.LookAt(new Vector3(targetLahan.position.x, transform.position.y, targetLahan.position.z));
        float speedMultiplier = panjangAsliAnimasiNanam / durasiMenanam;
        anim.SetFloat("SpeedNanam", speedMultiplier);
        anim.SetBool("isNanam", true);
        StartCoroutine(ProsesNanam());
    }

    IEnumerator ProsesNanam()
    {
        yield return new WaitForSeconds(durasiMenanam);
        anim.SetBool("isNanam", false);
        if (targetLahan != null && targetLahan.GetComponentInChildren<PohonScript>() == null)
        {
            GameObject p = Instantiate(prefabPohon, targetLahan.position, Quaternion.identity);
            p.transform.SetParent(targetLahan);
        }
        status = StatusPekerja.Idle;
    }

    void MulaiMenebang()
    {
        if (targetLahan == null) { status = StatusPekerja.Idle; return; }
        status = StatusPekerja.Menebang;
        transform.LookAt(new Vector3(targetLahan.position.x, transform.position.y, targetLahan.position.z));
        float speedMultiplier = panjangAsliAnimasiNebang / durasiMenebang;
        anim.SetFloat("SpeedNebang", speedMultiplier);
        anim.SetBool("isNebang", true);
        StartCoroutine(ProsesTebang());
    }

    IEnumerator ProsesTebang()
    {
        yield return new WaitForSeconds(durasiMenebang);
        anim.SetBool("isNebang", false);
        if (targetPohon != null)
        {
            EconomyManager.instance.kayuMentah += Random.Range(2, 5);
            EconomyManager.instance.TambahXP(1);
            Destroy(targetPohon);
        }
        status = StatusPekerja.Idle;
    }

    IEnumerator TungguAnimasiBerdiri()
    {
        yield return new WaitForSeconds(5f);
        statusIdle = AktivitasIdle.Milih;
    }
}