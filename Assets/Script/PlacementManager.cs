using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class PlacementManager : MonoBehaviour
{
    [Header("Sistem Penempatan")]
    public GameObject bendaYangMauDipasang;
    private GameObject bayanganAktif;
    public LayerMask layerBendaGrid;
    public float yOffsetMesin = 0f;
    private bool bisaPasang = true;

    [Header("Warna Indikator")]
    public Color warnaBisa = new Color(0, 1, 0, 0.5f);
    public Color warnaGakBisa = new Color(1, 0, 0, 0.5f);

    [Header("Pengaturan UI")]
    public GameObject panelUIUtama;
    public GameObject panelPilihMesin;
    public Button tombolBatalPlacement;

    [Header("UI Notifikasi & Info")]
    public TextMeshProUGUI teksInfoPlacement;
    public TextMeshProUGUI teksNotifikasi;

    // Batas area (grid)
    private float batasMin = -3f;
    private float batasMax = 3f;

    void Start()
    {
        bendaYangMauDipasang = null;
        if (teksInfoPlacement != null) teksInfoPlacement.gameObject.SetActive(false);
        if (teksNotifikasi != null) teksNotifikasi.gameObject.SetActive(false);

        if (tombolBatalPlacement != null)
            tombolBatalPlacement.onClick.AddListener(BatalPasang);
    }

    public void BukaMenuPabrik()
    {
        if (panelPilihMesin != null) panelPilihMesin.SetActive(true);
        if (panelUIUtama != null) panelUIUtama.SetActive(false);
    }

    public void TutupMenuPabrik()
    {
        if (panelPilihMesin != null) panelPilihMesin.SetActive(false);
        if (panelUIUtama != null) panelUIUtama.SetActive(true);
    }

    void SetWarnaBayangan(Color warna)
    {
        Renderer[] renderers = bayanganAktif.GetComponentsInChildren<Renderer>();
        foreach (Renderer r in renderers)
            r.material.color = warna;
    }

    void Update()
    {
        if (bendaYangMauDipasang == null || bayanganAktif == null) return;

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, Mathf.Infinity, ~layerBendaGrid))
        {
            float x = Mathf.Floor(hit.point.x);
            float z = Mathf.Floor(hit.point.z);
            Vector3 posisiBaru = new Vector3(x, hit.point.y + yOffsetMesin, z);

            // Cek batas area
            bool dalamBatas = (x >= batasMin && x <= batasMax && z >= batasMin && z <= batasMax);

            Collider[] tabrakan = Physics.OverlapBox(posisiBaru, new Vector3(0.45f, 0.45f, 0.45f), Quaternion.identity, layerBendaGrid);
            bool tidakTabrakan = (tabrakan.Length == 0);

            bisaPasang = dalamBatas && tidakTabrakan;

            // Update posisi bayangan (tetap bergerak meskipun di luar batas)
            bayanganAktif.transform.position = posisiBaru;
            SetWarnaBayangan(bisaPasang ? warnaBisa : warnaGakBisa);

            if (Input.GetMouseButtonDown(0) && bisaPasang)
            {
                PasangMesin(posisiBaru);
            }
        }
    }

    void PasangMesin(Vector3 posisi)
    {
        MesinPabrik mesinPabrik = bendaYangMauDipasang.GetComponent<MesinPabrik>();
        MesinPemahat mesinPemahat = bendaYangMauDipasang.GetComponent<MesinPemahat>();

        int harga = 0;
        string namaMesinForNotif = "Mesin";

        if (mesinPabrik != null)
        {
            harga = mesinPabrik.hargaBeli;
            namaMesinForNotif = mesinPabrik.namaMesin;
        }
        else if (mesinPemahat != null)
        {
            harga = mesinPemahat.hargaBeli;
            namaMesinForNotif = mesinPemahat.namaMesin;
        }
        else
        {
            Debug.LogError("Prefab mesin tidak memiliki komponen MesinPabrik atau MesinPemahat!");
            BatalPasang();
            return;
        }

        if (EconomyManager.instance.koin >= harga)
        {
            EconomyManager.instance.koin -= harga;
            GameObject baru = Instantiate(bendaYangMauDipasang, posisi, Quaternion.identity);
            baru.layer = LayerMask.NameToLayer("BendaGrid");

            TampilkanNotifikasi($"Mesin {namaMesinForNotif} berhasil ditambahkan", 2f);
            BatalPasang();
        }
        else
        {
            TampilkanNotifikasi("Duit cekak, Bos!", 2f);
            BatalPasang();
        }
    }

    public void PilihMesin(GameObject prefabBaru)
    {
        if (bayanganAktif != null) Destroy(bayanganAktif);
        bendaYangMauDipasang = prefabBaru;
        bayanganAktif = Instantiate(prefabBaru, transform.position, Quaternion.identity);

        foreach (Collider c in bayanganAktif.GetComponentsInChildren<Collider>()) c.enabled = false;

        if (panelPilihMesin != null) panelPilihMesin.SetActive(false);
        if (panelUIUtama != null) panelUIUtama.SetActive(false);

        if (teksInfoPlacement != null)
        {
            string namaMesin = "";
            MesinPabrik mp = prefabBaru.GetComponent<MesinPabrik>();
            MesinPemahat mpm = prefabBaru.GetComponent<MesinPemahat>();
            if (mp != null) namaMesin = mp.namaMesin;
            else if (mpm != null) namaMesin = mpm.namaMesin;
            else namaMesin = prefabBaru.name;

            teksInfoPlacement.text = $"Atur posisi {namaMesin} sesuka Anda. Tekan tombol Batal jika Anda berubah pikiran.";
            teksInfoPlacement.gameObject.SetActive(true);
        }

        if (tombolBatalPlacement != null) tombolBatalPlacement.gameObject.SetActive(true);
    }

    public void BatalPasang()
    {
        if (bayanganAktif != null) Destroy(bayanganAktif);
        bendaYangMauDipasang = null;
        if (panelUIUtama != null) panelUIUtama.SetActive(true);

        if (teksInfoPlacement != null) teksInfoPlacement.gameObject.SetActive(false);
        if (tombolBatalPlacement != null) tombolBatalPlacement.gameObject.SetActive(false);
    }

    void TampilkanNotifikasi(string pesan, float durasi)
    {
        if (teksNotifikasi != null)
        {
            teksNotifikasi.text = pesan;
            teksNotifikasi.gameObject.SetActive(true);
            CancelInvoke(nameof(HideNotifikasi));
            Invoke(nameof(HideNotifikasi), durasi);
        }
    }

    void HideNotifikasi()
    {
        if (teksNotifikasi != null) teksNotifikasi.gameObject.SetActive(false);
    }
}