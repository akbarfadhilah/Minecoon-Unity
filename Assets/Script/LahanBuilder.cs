using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class LahanBuilder : MonoBehaviour
{
    public static LahanBuilder instance;

    [Header("UI Notifikasi & Info")]
    public TextMeshProUGUI teksInfoPlacement; // teks info saat mode edit
    public TextMeshProUGUI teksNotifikasi;    // teks notifikasi (bisa gunakan teksNotifikasi yang sudah ada, atau pisah)

    [Header("Pengaturan Kamera")]
    public Camera mainCamera;
    public Vector3 posNormal, rotNormal;
    public Vector3 posTopDown, rotTopDown;
    [Tooltip("Durasi pergerakan kamera saat transisi mode (dalam detik)")]
    public float durasiTransisiKamera = 1.0f; // BARU: Mengatur kecepatan transisi gerakan kamera

    public LayerMask layerBendaGrid;

    [Header("UI & Prefab")]
    public GameObject panelEdit;
    public GameObject prefabLahanAsli;
    public GameObject prefabLahanDraft;
    public GameObject prefabPenanda; // Kursor yang mengikuti mouse

    private GameObject kursorPreview;
    private Renderer kursorRenderer;

    private bool modeEdit = false;
    private bool modeHapus = false;

    // DATA ASLI (Sudah dikonfirmasi)
    public List<Vector3> daftarPosisiAsli = new List<Vector3>();
    public List<GameObject> objekLahanAsliTerpasang = new List<GameObject>();

    // DATA DRAFT (Belum dikonfirmasi)
    private List<Vector3> tempPosisiLahan = new List<Vector3>();
    private List<GameObject> objekDraftTerpasang = new List<GameObject>();

    // Data variabel untuk menghitung jumlah perubahan
    private int jumlahBerhasilDitambah = 0;
    private int jumlahBerhasilDihapus = 0;

    void Awake()
    {
        instance = this;
    }

    void Start()
    {
        if (prefabPenanda != null)
        {
            kursorPreview = Instantiate(prefabPenanda);
            kursorRenderer = kursorPreview.GetComponent<Renderer>();
            kursorPreview.SetActive(false);
        }

        if (teksInfoPlacement != null) teksInfoPlacement.gameObject.SetActive(false);
    }

    // --- FUNGSI UNTUK MENAMPILKAN PESAN ---
    void TampilkanPesan(string pesan)
    {
        if (teksNotifikasi != null)
        {
            teksNotifikasi.text = pesan;
            teksNotifikasi.gameObject.SetActive(true);
            CancelInvoke("HapusPesan");
            Invoke("HapusPesan", 3f);
        }
    }
    void HapusPesan() { if (teksNotifikasi != null) teksNotifikasi.gameObject.SetActive(false); }

    // --- LOGIKA KONFIRMASI (FIX BUG POHON HILANG) ---
    public void KonfirmasiLahan()
    {
        int added = 0;
        foreach (Vector3 pos in tempPosisiLahan)
        {
            if (!daftarPosisiAsli.Contains(pos))
            {
                GameObject lahanBaru = Instantiate(prefabLahanAsli, pos, Quaternion.identity);
                lahanBaru.layer = LayerMask.NameToLayer("BendaGrid");

                daftarPosisiAsli.Add(pos);
                objekLahanAsliTerpasang.Add(lahanBaru);
                added++;
            }
        }
        if (added > 0)
        {
            TampilkanPesan($"{added} lahan berhasil ditambahkan");
        }
        BersihkanVisualDraft();
        KeluarModeEdit();
    }

    // --- LOGIKA HAPUS (FIX BUG VALIDASI POHON) ---
    void LogikaKlikHapus(Vector3 pos)
    {
        int index = daftarPosisiAsli.IndexOf(pos);

        if (index != -1)
        {
            GameObject objLahan = objekLahanAsliTerpasang[index];

            if (objLahan.GetComponentInChildren<PohonScript>() != null)
            {
                TampilkanPesan("Lahan terdapat pohon, tidak bisa dihapus!");
                return;
            }

            Destroy(objLahan);
            objekLahanAsliTerpasang.RemoveAt(index);
            daftarPosisiAsli.RemoveAt(index);

            jumlahBerhasilDihapus++;
            TampilkanPesan(jumlahBerhasilDihapus + " lahan berhasil dihapus");

            CancelInvoke("ResetCounterHapus");
            Invoke("ResetCounterHapus", 0.5f);
        }
    }

    void ResetCounterHapus()
    {
        jumlahBerhasilDihapus = 0;
    }

    void Update()
    {
        if (!modeEdit || kursorPreview == null) return;

        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit))
        {
            float x = Mathf.Floor(hit.point.x) + 0.5f;
            float z = Mathf.Floor(hit.point.z) + 0.5f;
            Vector3 posGrid = new Vector3(x, 0f, z);

            kursorPreview.transform.position = posGrid;

            bool adaBendaLain = Physics.CheckSphere(new Vector3(x, 0.5f, z), 0.45f, layerBendaGrid);
            bool sudahAdaLahan = CekApakahSudahAdaDraft(posGrid) || CekApakahSudahAdaLahanAsli(posGrid);
            bool tidakBisaPasang = sudahAdaLahan || adaBendaLain;

            kursorRenderer.material.color = tidakBisaPasang ? Color.red : Color.white;

            if (Input.GetMouseButtonDown(0))
            {
                if (modeHapus) HapusLahan(posGrid);
                else if (!tidakBisaPasang) TambahLahanDraft(posGrid);
            }
        }
    }

    // --- FUNGSI TOMBOL ---
    public void AktifkanModeEdit()
    {
        modeEdit = true;
        panelEdit.SetActive(true);
        if (kursorPreview != null) kursorPreview.SetActive(true);

        foreach (GameObject draft in objekDraftTerpasang)
        {
            if (draft != null) draft.SetActive(true);
        }

        // BARU: Hentikan animasi kamera sebelumnya jika ada untuk menghindari bentrok / bergetar
        LeanTween.cancel(mainCamera.gameObject);

        // BARU: Transisi mulus ke posisi Topdown menggunakan LeanTween
        LeanTween.move(mainCamera.gameObject, posTopDown, durasiTransisiKamera).setEase(LeanTweenType.easeInOutQuad);
        LeanTween.rotate(mainCamera.gameObject, rotTopDown, durasiTransisiKamera).setEase(LeanTweenType.easeInOutQuad);

        if (teksInfoPlacement != null)
        {
            teksInfoPlacement.text = "Atur posisi lahan sesuka Anda. Tekan tombol Batal jika Anda berubah pikiran.";
            teksInfoPlacement.gameObject.SetActive(true);
        }
    }

    public void TombolPilih() { modeHapus = false; }
    public void TombolHapus() { modeHapus = true; }

    public void TombolBatal()
    {
        foreach (GameObject draft in objekDraftTerpasang)
        {
            if (draft != null) draft.SetActive(false);
        }
        KeluarModeEdit();
    }

    public void TombolKonfirmasi()
    {
        foreach (Vector3 pos in tempPosisiLahan)
        {
            if (!daftarPosisiAsli.Contains(pos))
            {
                GameObject lahanBaru = Instantiate(prefabLahanAsli, pos, Quaternion.identity);
                lahanBaru.layer = LayerMask.NameToLayer("BendaGrid");

                daftarPosisiAsli.Add(pos);
                objekLahanAsliTerpasang.Add(lahanBaru);
                jumlahBerhasilDitambah++;
            }
        }

        if (jumlahBerhasilDitambah > 0)
        {
            TampilkanPesan(jumlahBerhasilDitambah + " lahan berhasil ditambahkan");
            jumlahBerhasilDitambah = 0;
        }

        BersihkanVisualDraft();
        KeluarModeEdit();
    }

    void KeluarModeEdit()
    {
        modeEdit = false;
        panelEdit.SetActive(false);
        if (kursorPreview != null) kursorPreview.SetActive(false);

        // BARU: Hentikan animasi kamera sebelumnya jika ada
        LeanTween.cancel(mainCamera.gameObject);

        // BARU: Transisi mulus kembali ke posisi Normal menggunakan LeanTween
        LeanTween.move(mainCamera.gameObject, posNormal, durasiTransisiKamera).setEase(LeanTweenType.easeInOutQuad);
        LeanTween.rotate(mainCamera.gameObject, rotNormal, durasiTransisiKamera).setEase(LeanTweenType.easeInOutQuad);

        if (teksInfoPlacement != null) teksInfoPlacement.gameObject.SetActive(false);
    }

    // --- LOGIKA GRID & DRAFT ---
    void TambahLahanDraft(Vector3 pos)
    {
        if (!CekApakahSudahAdaDraft(pos) && !CekApakahSudahAdaLahanAsli(pos))
        {
            tempPosisiLahan.Add(pos);
            GameObject visual = Instantiate(prefabLahanDraft, pos, Quaternion.identity);
            objekDraftTerpasang.Add(visual);
        }
    }

    void HapusLahan(Vector3 pos)
    {
        int indexDraft = tempPosisiLahan.IndexOf(pos);
        if (indexDraft != -1)
        {
            Destroy(objekDraftTerpasang[indexDraft]);
            objekDraftTerpasang.RemoveAt(indexDraft);
            tempPosisiLahan.RemoveAt(indexDraft);
            TampilkanPesan("Lahan batal dipasang (dihapus dari draft)");
            return;
        }

        int indexAsli = daftarPosisiAsli.IndexOf(pos);
        if (indexAsli != -1)
        {
            GameObject objLahan = objekLahanAsliTerpasang[indexAsli];

            if (objLahan.GetComponentInChildren<PohonScript>() != null)
            {
                TampilkanPesan("Lahan terdapat pohon, tidak bisa dihapus!");
                return;
            }

            Destroy(objLahan);
            objekLahanAsliTerpasang.RemoveAt(indexAsli);
            daftarPosisiAsli.RemoveAt(indexAsli);

            jumlahBerhasilDihapus++;
            TampilkanPesan(jumlahBerhasilDihapus + " lahan berhasil dihapus");

            CancelInvoke("ResetCounterHapus");
            Invoke("ResetCounterHapus", 0.5f);
        }
    }

    bool CekApakahSudahAdaDraft(Vector3 pos) { return tempPosisiLahan.Contains(pos); }
    bool CekApakahSudahAdaLahanAsli(Vector3 pos) { return daftarPosisiAsli.Contains(pos); }

    void BersihkanVisualDraft()
    {
        foreach (GameObject go in objekDraftTerpasang) Destroy(go);
        objekDraftTerpasang.Clear();
        tempPosisiLahan.Clear();
    }
}