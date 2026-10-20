using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class MesinEditor : MonoBehaviour
{
    public static MesinEditor instance;

    [Header("Panel Edit")]
    public GameObject panelEdit;
    public Button tombolHapus;
    public Button tombolBatal;
    public Button tombolPutar;
    public Button tombolPindah;
    public Button tombolBatalPindah;

    [Header("Panel Konfirmasi Hapus")]
    public GameObject panelKonfirmasi;
    public TextMeshProUGUI teksKonfirmasi;
    public Button tombolYa;
    public Button tombolTidak;

    [Header("Referensi Lain")]
    public LayerMask layerBendaGrid;
    public float yOffsetMesin = 0f;

    private GameObject targetMesin;
    private Vector3 posisiAwal;
    private Quaternion rotasiAwal;
    private int rotasiState = 0;
    private bool modePindah = false;

    private GameObject bayanganPindah;
    private Material materialCache;
    private Color warnaNormal;

    private float batasMin = -3f;
    private float batasMax = 3f;

    private Vector3[] offsets = new Vector3[]
    {
        Vector3.zero,
        new Vector3(0, 0, 0),
        new Vector3(0, 0, 0),
        new Vector3(0, 0, 0)
    };
    private int[] rotations = new int[] { 0, 90, 180, 270 };

    void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);

        panelEdit.SetActive(false);
        panelKonfirmasi.SetActive(false);
    }

    void Start()
    {
        tombolHapus.onClick.AddListener(BukaKonfirmasiHapus);
        tombolBatal.onClick.AddListener(() => {
            if (modePindah)
                BatalkanPindah();
            else
                KeluarModeEdit();
        });
        tombolPutar.onClick.AddListener(PutarMesin);
        tombolPindah.onClick.AddListener(AktifkanModePindah);
        tombolYa.onClick.AddListener(HapusMesin);
        tombolTidak.onClick.AddListener(() => panelKonfirmasi.SetActive(false));

        if (tombolBatalPindah != null)
            tombolBatalPindah.onClick.AddListener(BatalkanPindah);
    }

    void Update()
    {
        if (!modePindah || targetMesin == null || bayanganPindah == null) return;

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        // MENGGUNAKAN LOGIKA PLACEMENT MANAGER:
        // ~layerBendaGrid akan membuat raycast MENEMBUS mesin/lahan dan pasti mengenai lantai.
        // Mesin tidak akan pernah melompat ke atas mesin lain lagi.
        if (Physics.Raycast(ray, out hit, Mathf.Infinity, ~layerBendaGrid))
        {
            float x = Mathf.Floor(hit.point.x);
            float z = Mathf.Floor(hit.point.z);

            // Tetap kunci agar tidak keluar batas
            x = Mathf.Clamp(x, batasMin, batasMax);
            z = Mathf.Clamp(z, batasMin, batasMax);

            // Gunakan hit.point.y persis seperti PlacementManager
            Vector3 posGrid = new Vector3(x, yOffsetMesin, z);

            // Deteksi tabrakan dengan ukuran standar box
            Collider[] tabrakan = Physics.OverlapBox(posGrid, new Vector3(0.45f, 0.45f, 0.45f), Quaternion.identity, layerBendaGrid);

            // Karena collider mesin asli sudah kita matikan saat klik "Pindah",
            // Jika Length == 0, berarti area tersebut benar-benar kosong!
            bool valid = (tabrakan.Length == 0);

            bayanganPindah.transform.position = posGrid;

            // Visual Hijau / Merah
            Renderer[] rends = bayanganPindah.GetComponentsInChildren<Renderer>();
            Color warna = valid ? new Color(0, 1, 0, 0.5f) : new Color(1, 0, 0, 0.5f);
            foreach (Renderer r in rends)
            {
                if (r.material.HasProperty("_Color"))
                    r.material.color = warna;
            }

            // Konfirmasi Pindah
            if (Input.GetMouseButtonDown(0) && valid)
            {
                targetMesin.transform.position = posGrid;
                posisiAwal = posGrid;

                // Nyalakan kembali collider mesin
                foreach (Collider c in targetMesin.GetComponentsInChildren<Collider>())
                    c.enabled = true;

                HapusBayangan();
                modePindah = false;
                panelEdit.SetActive(true);
                ResetWarna();
                if (tombolBatalPindah != null) tombolBatalPindah.gameObject.SetActive(false);
            }
        }
    }

    // ==================== MODE PINDAH ====================

    void AktifkanModePindah()
    {
        if (targetMesin == null) return;
        if (modePindah) return;

        modePindah = true;
        posisiAwal = targetMesin.transform.position;
        rotasiAwal = targetMesin.transform.rotation;

        // MATIKAN SEMENTARA collider mesin asli agar tidak terdeteksi oleh sistem tabrakan
        foreach (Collider c in targetMesin.GetComponentsInChildren<Collider>())
            c.enabled = false;

        // Buat bayangan (clone mesin, matikan collider)
        bayanganPindah = Instantiate(targetMesin, targetMesin.transform.position, targetMesin.transform.rotation);
        foreach (Collider c in bayanganPindah.GetComponentsInChildren<Collider>())
            c.enabled = false;

        panelEdit.SetActive(false);
        TampilkanTombolBatalPindah();
    }

    void BatalkanPindah()
    {
        if (targetMesin != null)
        {
            targetMesin.transform.position = posisiAwal;
            targetMesin.transform.rotation = rotasiAwal;

            // NYALAKAN KEMBALI collider mesin aslinya
            foreach (Collider c in targetMesin.GetComponentsInChildren<Collider>())
                c.enabled = true;
        }
        HapusBayangan();
        modePindah = false;
        panelEdit.SetActive(true);
        ResetWarna();
        SembunyikanTombolBatalPindah();
    }

    void HapusBayangan()
    {
        if (bayanganPindah != null)
        {
            Destroy(bayanganPindah);
            bayanganPindah = null;
        }
    }

    // ==================== UI TOMBOL BATAL PINDAH ====================

    void TampilkanTombolBatalPindah()
    {
        if (tombolBatalPindah != null)
            tombolBatalPindah.gameObject.SetActive(true);
    }

    void SembunyikanTombolBatalPindah()
    {
        if (tombolBatalPindah != null)
            tombolBatalPindah.gameObject.SetActive(false);
    }

    // ==================== FUNGSI EDIT LAINNYA ====================

    public void BukaEdit(GameObject mesin)
    {
        targetMesin = mesin;
        posisiAwal = mesin.transform.position;
        rotasiAwal = mesin.transform.rotation;
        rotasiState = 0;

        Renderer rend = targetMesin.GetComponentInChildren<Renderer>();
        if (rend != null)
        {
            materialCache = rend.material;
            warnaNormal = materialCache.color;
        }
        panelEdit.SetActive(true);
        SembunyikanTombolBatalPindah();
    }

    void PutarMesin()
    {
        if (targetMesin == null) return;
        rotasiState = (rotasiState + 1) % 4;
        Vector3 offset = offsets[rotasiState];
        int rotasiY = rotations[rotasiState];
        Vector3 posBaru = posisiAwal + offset;
        if (CekValidasiPosisi(posBaru))
        {
            targetMesin.transform.position = posBaru;
            targetMesin.transform.rotation = rotasiAwal * Quaternion.Euler(0, rotasiY, 0);
            posisiAwal = posBaru;
            ResetWarna();
        }
        else
        {
            SetWarnaMerah();
            Debug.Log("Rotasi tidak valid, tabrakan!");
        }
    }

    bool CekValidasiPosisi(Vector3 pos)
    {
        if (pos.x < batasMin || pos.x > batasMax || pos.z < batasMin || pos.z > batasMax)
            return false;

        Collider[] colliders = Physics.OverlapBox(pos, new Vector3(0.45f, 0.45f, 0.45f), Quaternion.identity, layerBendaGrid);
        foreach (Collider col in colliders)
        {
            // Diperkuat agar child collider juga terabaikan
            if (col.gameObject == targetMesin || col.transform.IsChildOf(targetMesin.transform)) continue;
            return false;
        }
        return true;
    }

    void ResetWarna()
    {
        Renderer rend = targetMesin?.GetComponentInChildren<Renderer>();
        if (rend != null && materialCache != null)
            rend.material.color = warnaNormal;
    }

    void SetWarnaMerah()
    {
        Renderer rend = targetMesin.GetComponentInChildren<Renderer>();
        if (rend != null)
            rend.material.color = Color.red;
    }

    void BukaKonfirmasiHapus()
    {
        panelKonfirmasi.SetActive(true);
        teksKonfirmasi.text = $"Apakah anda yakin ingin menghapus {targetMesin.name}? Anda akan mendapat 50% harga beli.";
    }

    void HapusMesin()
    {
        if (targetMesin == null) return;

        int hargaBeli = 0;
        MesinPabrik mp = targetMesin.GetComponent<MesinPabrik>();
        MesinPemahat mpm = targetMesin.GetComponent<MesinPemahat>();
        if (mp != null) hargaBeli = mp.hargaBeli;
        else if (mpm != null) hargaBeli = mpm.hargaBeli;

        int refund = Mathf.RoundToInt(hargaBeli * 0.5f);
        if (EconomyManager.instance != null) EconomyManager.instance.TambahKoin(refund);

        Destroy(targetMesin);
        panelKonfirmasi.SetActive(false);
        KeluarModeEdit();
    }

    void KeluarModeEdit()
    {
        if (modePindah)
            BatalkanPindah();
        targetMesin = null;
        rotasiState = 0;
        panelEdit.SetActive(false);
        panelKonfirmasi.SetActive(false);
        materialCache = null;
        HapusBayangan();
        SembunyikanTombolBatalPindah();
    }
}