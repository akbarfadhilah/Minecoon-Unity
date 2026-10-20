using UnityEngine;
using System.Collections.Generic;
using UnityEngine.EventSystems;

public class MesinPemahat : MonoBehaviour
{
    [Header("Sistem Pekerja")]
    public int maxPekerja = 2;
    public int jumlahPekerjaAktif = 0;
    public PekerjaPemahat pekerjaPemahatAktif;

    [Header("Pengaturan Pabrik")]
    public string namaMesin = "Mesin Pemahat";
    public int hargaBeli = 200;

    [Header("Status Produksi")]
    public bool sedangProduksi = false;
    public float sisaWaktu = 0f;
    public List<string> antreanBarang = new List<string>();
    private string barangSedangDiproses = "";

    void OnMouseDown()
    {
        if (EventSystem.current.IsPointerOverGameObject()) return;
        if (UIPanelManagerPmht.instance != null)
            UIPanelManagerPmht.instance.BukaPanelMesin(this);
    }

    void Update()
    {
        if (jumlahPekerjaAktif <= 0) return;

        if (sedangProduksi)
        {
            sisaWaktu -= Time.deltaTime;
            if (sisaWaktu <= 0) SelesaiSatuProduksi();
        }
        else if (antreanBarang.Count > 0)
        {
            MulaiProsesBerikutnya();
        }
    }

    public void TambahKeAntrean(string namaBarang, int jumlah)
    {
        // Hitung kebutuhan bahan & listrik
        int butuhListrik = 0;
        int butuhPapan = 0;
        int butuhGelondongan = 0;

        switch (namaBarang)
        {
            case "Alat makan": butuhListrik = 10; butuhPapan = 1; break;
            case "Telenan": butuhListrik = 5; butuhPapan = 1; break;
            case "Piring": butuhListrik = 10; butuhPapan = 1; break;
            case "Ulekan": butuhListrik = 20; butuhGelondongan = 1; break;
            case "Patung Kayu": butuhListrik = 30; butuhGelondongan = 1; butuhPapan = 1; break;
            case "Alat masak": butuhListrik = 15; butuhPapan = 2; break;
            case "Wadah perhiasan": butuhListrik = 20; butuhPapan = 3; break;
            default: Debug.LogError("Barang tidak dikenal: " + namaBarang); return;
        }

        butuhListrik *= jumlah;
        butuhPapan *= jumlah;
        butuhGelondongan *= jumlah;

        bool listrikCukup = EconomyManager.instance.listrik >= butuhListrik;
        bool papanCukup = EconomyManager.instance.papanHalus >= butuhPapan;
        bool gelondongCukup = EconomyManager.instance.kayuGelondongan >= butuhGelondongan;

        if (listrikCukup && papanCukup && gelondongCukup)
        {
            // Kurangi资源
            EconomyManager.instance.listrik -= butuhListrik;
            EconomyManager.instance.papanHalus -= butuhPapan;
            EconomyManager.instance.kayuGelondongan -= butuhGelondongan;

            for (int i = 0; i < jumlah; i++) antreanBarang.Add(namaBarang);
            Debug.Log($"<color=green>Sukses! {jumlah} {namaBarang} masuk antrean pemahat.</color>");
        }
        else
        {
            string alasan = !listrikCukup ? "Listrik" : (!papanCukup ? "Papan Halus" : "Kayu Gelondongan");
            Debug.Log($"<color=red>GAGAL! Butuh {alasan} untuk {namaBarang}.</color>");
        }
    }

    void MulaiProsesBerikutnya()
    {
        if (antreanBarang.Count == 0) return;
        sedangProduksi = true;
        barangSedangDiproses = antreanBarang[0];

        // Durasi dari pekerja aktif
        if (pekerjaPemahatAktif != null)
        {
            switch (barangSedangDiproses)
            {
                case "Alat makan": sisaWaktu = pekerjaPemahatAktif.durasiAlatMakan; break;
                case "Telenan": sisaWaktu = pekerjaPemahatAktif.durasiTelenan; break;
                case "Piring": sisaWaktu = pekerjaPemahatAktif.durasiPiring; break;
                case "Ulekan": sisaWaktu = pekerjaPemahatAktif.durasiUlekan; break;
                case "Patung Kayu": sisaWaktu = pekerjaPemahatAktif.durasiPatungKayu; break;
                case "Alat masak": sisaWaktu = pekerjaPemahatAktif.durasiAlatMasak; break;
                case "Wadah perhiasan": sisaWaktu = pekerjaPemahatAktif.durasiWadahPerhiasan; break;
                default: sisaWaktu = 5f; break;
            }
        }
        else sisaWaktu = 5f;
    }

    void SelesaiSatuProduksi()
    {
        sedangProduksi = false;
        // Tambah stok sesuai barang
        switch (barangSedangDiproses)
        {
            case "Alat makan":
                EconomyManager.instance.alatMakan++;
                EconomyManager.instance.TambahXP(3);
                break;
            case "Telenan":
                EconomyManager.instance.telenan++;
                EconomyManager.instance.TambahXP(3);
                break;
            case "Piring":
                EconomyManager.instance.piring++;
                EconomyManager.instance.TambahXP(3);
                break;
            case "Ulekan":
                EconomyManager.instance.ulekan++;
                EconomyManager.instance.TambahXP(3);
                break;
            case "Patung Kayu":
                EconomyManager.instance.patungKayu++;
                EconomyManager.instance.TambahXP(3);
                break;
            case "Alat masak":
                EconomyManager.instance.alatMasak++;
                EconomyManager.instance.TambahXP(3);
                break;
            case "Wadah perhiasan":
                EconomyManager.instance.wadahPerhiasan++;
                EconomyManager.instance.TambahXP(3);
                break;
        }

        if (antreanBarang.Count > 0)
            antreanBarang.RemoveAt(0);

        if (UIPanelManagerPmht.instance != null && UIPanelManagerPmht.instance.panelPemahat.activeSelf)
            UIPanelManagerPmht.instance.UpdateAntrean();
    }

    public void TambahPekerja(PekerjaPemahat pekerja)
    {
        jumlahPekerjaAktif++;
        pekerjaPemahatAktif = pekerja;
    }
    public void KurangiPekerja()
    {
        if (jumlahPekerjaAktif > 0) jumlahPekerjaAktif--;
        if (jumlahPekerjaAktif == 0) pekerjaPemahatAktif = null;
    }

    public void BatalkanAntrean()
    {
        if (sedangProduksi)
        {
            Debug.Log("Tidak bisa membatalkan antrean karena mesin sedang berproduksi!");
            return;
        }

        if (antreanBarang.Count == 0) return;

        int totalListrik = 0;
        int totalPapan = 0;
        int totalGelondongan = 0;

        foreach (string barang in antreanBarang)
        {
            switch (barang)
            {
                case "Alat makan": totalListrik += 10; totalPapan += 1; break;
                case "Telenan": totalListrik += 5; totalPapan += 1; break;
                case "Piring": totalListrik += 10; totalPapan += 1; break;
                case "Ulekan": totalListrik += 20; totalGelondongan += 1; break;
                case "Patung Kayu": totalListrik += 30; totalGelondongan += 1; totalPapan += 1; break;
                case "Alat masak": totalListrik += 15; totalPapan += 2; break;
                case "Wadah perhiasan": totalListrik += 20; totalPapan += 3; break;
            }
        }

        EconomyManager.instance.listrik += totalListrik;
        EconomyManager.instance.papanHalus += totalPapan;
        EconomyManager.instance.kayuGelondongan += totalGelondongan;

        antreanBarang.Clear();

        if (UIPanelManagerPmht.instance != null && UIPanelManagerPmht.instance.panelPemahat.activeSelf)
            UIPanelManagerPmht.instance.UpdateAntrean();
    }
}