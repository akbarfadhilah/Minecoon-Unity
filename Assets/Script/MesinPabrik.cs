using UnityEngine;
using TMPro;
using System.Collections.Generic;
using UnityEngine.EventSystems;

public class MesinPabrik : MonoBehaviour
{
    public enum Jenis { PotongKayu, Mebel }

    [Header("Sistem Pekerja")]
    public int maxPekerja = 2;
    public int jumlahPekerjaAktif = 0;
    public PekerjaMesin pekerjaMesinAktif;
    public PekerjaMebel pekerjaMebelAktif;

    [Header("Pengaturan Pabrik")]
    public Jenis jenisMesin;
    public string namaMesin = "Mesin Produksi"; 
    public int hargaBeli = 100;

    [Header("Status Produksi")]
    public bool sedangProduksi = false;
    public float sisaWaktu = 0f;
    public List<string> antreanBarang = new List<string>();
    private string barangSedangDiproses = "";

    void OnMouseDown()
    {
        if (EventSystem.current.IsPointerOverGameObject()) return;
        if (UIPanelManager.instance != null)
        {
            UIPanelManager.instance.BukaPanelMesin(this);
        }
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

    // --- FUNGSI ANTREAN YANG SUDAH DIBERSIHKAN ---
    public void TambahKeAntrean(string namaBarang, int jumlah)
    {
        // Izinkan Gelondongan dan Papan Halus untuk mesin PotongKayu
        if (jenisMesin == Jenis.PotongKayu && namaBarang != "Gelondongan" && namaBarang != "Papan Halus") return;
        if (jenisMesin == Jenis.Mebel && namaBarang == "Gelondongan") return;

        int butuhListrik = 0;
        int butuhBahanKayu = 0;
        string namaBahan = "";

        if (namaBarang == "Gelondongan")
        {
            butuhListrik = 10 * jumlah;
            butuhBahanKayu = 1 * jumlah;
            namaBahan = "Kayu Mentah";
        }
        else if (namaBarang == "Papan Halus")
        {
            butuhListrik = 12 * jumlah;
            butuhBahanKayu = 1 * jumlah;
            namaBahan = "Kayu Mentah";
        }
        else if (namaBarang == "Kursi")
        {
            butuhListrik = 15 * jumlah;
            butuhBahanKayu = 1 * jumlah;
            namaBahan = "Kayu Gelondongan";
        }
        else if (namaBarang == "Meja")
        {
            butuhListrik = 25 * jumlah;
            butuhBahanKayu = 2 * jumlah;
            namaBahan = "Kayu Gelondongan";
        }

        bool listrikCukup = EconomyManager.instance.listrik >= butuhListrik;
        bool bahanCukup = EconomyManager.instance.AmbilStok(namaBahan) >= butuhBahanKayu;

        if (listrikCukup && bahanCukup)
        {
            EconomyManager.instance.listrik -= butuhListrik;
            EconomyManager.instance.KurangiStok(namaBahan, butuhBahanKayu);

            for (int i = 0; i < jumlah; i++) antreanBarang.Add(namaBarang);

            Debug.Log($"<color=green>Sukses! Memasukkan {jumlah} {namaBarang} ke antrean.</color>");
        }
        else
        {
            string alasan = !listrikCukup ? "Listrik" : "Bahan Kayu";
            Debug.Log($"<color=red>GAGAL: Tidak cukup membuat {jumlah} {namaBarang} karena {alasan} kurang!</color>");
        }
    }

    void MulaiProsesBerikutnya()
    {
        if (antreanBarang.Count == 0) return;

        sedangProduksi = true;
        barangSedangDiproses = antreanBarang[0];

        if (barangSedangDiproses == "Gelondongan")
            sisaWaktu = (pekerjaMesinAktif != null) ? pekerjaMesinAktif.durasiPotongKayu : 5f;
        else if (barangSedangDiproses == "Papan Halus")
            sisaWaktu = (pekerjaMesinAktif != null) ? (pekerjaMesinAktif.durasiPapanHalus * 4) : 20f;
        else if (barangSedangDiproses == "Kursi")
            sisaWaktu = (pekerjaMebelAktif != null) ? pekerjaMebelAktif.durasiKursi : 8f;
        else if (barangSedangDiproses == "Meja")
            sisaWaktu = (pekerjaMebelAktif != null) ? pekerjaMebelAktif.durasiMeja : 12f;
    }

    void SelesaiSatuProduksi()
    {
        sedangProduksi = false;

        if (barangSedangDiproses == "Gelondongan")
        {
            EconomyManager.instance.kayuGelondongan += 2;
            EconomyManager.instance.TambahXP(2);
        }
        else if (barangSedangDiproses == "Papan Halus")
        {
            EconomyManager.instance.papanHalus += 4;
            EconomyManager.instance.TambahXP(2);
        }
        else if (barangSedangDiproses == "Kursi")
        {
            EconomyManager.instance.kursi += 1;
            EconomyManager.instance.TambahXP(5);
        }
        else if (barangSedangDiproses == "Meja")
        {
            EconomyManager.instance.meja += 1;
            EconomyManager.instance.TambahXP(5);
        }

        // Pengecekan sebelum RemoveAt
        if (antreanBarang.Count > 0)
            antreanBarang.RemoveAt(0);

        // Update UI antrean jika panel mesin terbuka
        if (UIPanelManager.instance != null && UIPanelManager.instance.panelMesin.activeSelf)
            UIPanelManager.instance.UpdateAntrean();
    }

    public void TambahPekerjaMesin(PekerjaMesin pekerja)
    {
        jumlahPekerjaAktif++;
        pekerjaMesinAktif = pekerja;
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
        int totalKayuMentah = 0;
        int totalGelondongan = 0;

        foreach (string barang in antreanBarang)
        {
            if (barang == "Gelondongan")
            {
                totalListrik += 10;
                totalKayuMentah += 1;
            }
            else if (barang == "Papan Halus")
            {
                totalListrik += 12;
                totalKayuMentah += 1;
            }
            else if (barang == "Kursi")
            {
                totalListrik += 15;
                totalGelondongan += 1;
            }
            else if (barang == "Meja")
            {
                totalListrik += 25;
                totalGelondongan += 2;
            }
        }

        EconomyManager.instance.listrik += totalListrik;
        EconomyManager.instance.kayuMentah += totalKayuMentah;
        EconomyManager.instance.kayuGelondongan += totalGelondongan;

        antreanBarang.Clear();

        if (UIPanelManager.instance != null && UIPanelManager.instance.panelMesin.activeSelf)
            UIPanelManager.instance.UpdateAntrean();
    }

    public void KurangiPekerja() { if (jumlahPekerjaAktif > 0) jumlahPekerjaAktif--; }
    public void TambahPekerjaMebel(PekerjaMebel script) { pekerjaMebelAktif = script; jumlahPekerjaAktif++; }
    public void KurangiPekerjaMebel(PekerjaMebel script) { if (jumlahPekerjaAktif > 0) jumlahPekerjaAktif--; if (pekerjaMebelAktif == script) pekerjaMebelAktif = null; }
}