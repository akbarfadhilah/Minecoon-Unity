using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;
using System.Collections.Generic;

public class UIPanelManager : MonoBehaviour
{
    public static UIPanelManager instance;

    [Header("UI Panel Pabrik")]
    public GameObject panelMesin;
    public TextMeshProUGUI teksNamaMesin;
    public TextMeshProUGUI teksInfoResep;
    public TMP_InputField fieldJumlah; // Input Field
    public TextMeshProUGUI teksTimerAktif;

    [Header("Tombol Pesan")]
    public GameObject tombolPesanKursi;
    public GameObject tombolPesanMeja;
    public GameObject tombolPesanPotong;
    public GameObject tombolPesanPapanHalus;

    [Header("Info Pekerja Aktif")]
    public Image gambarPekerjaAktif;
    public TextMeshProUGUI teksNamaPekerjaAktif;
    public TextMeshProUGUI teksSisaWaktuPekerja;

    [Header("Default Placeholder")]
    public Sprite defaultFotoPekerja;

    [Header("Tombol Edit")]
    public Button tombolEdit;

    [Header("Antrean")]
    public Transform kontenAntrean;          // parent untuk daftar antrean
    public GameObject prefabItemAntrean;     // prefab ItemAntreanUI
    public Button tombolBatalAntrean;        // tombol batal antrean

    private MesinPabrik mesinYangSedangDibuka;

    void Awake()
    {
        instance = this;
        if (panelMesin != null) panelMesin.SetActive(false);

        // Listener tombol edit (hapus semua OnClick di Inspector!)
        if (tombolEdit != null)
        {
            tombolEdit.onClick.RemoveAllListeners(); // Hapus listener lama (termasuk dari Inspector)
            tombolEdit.onClick.AddListener(() => {
                if (mesinYangSedangDibuka != null && MesinEditor.instance != null)
                    MesinEditor.instance.BukaEdit(mesinYangSedangDibuka.gameObject);
            });
        }

        if (tombolBatalAntrean != null)
            tombolBatalAntrean.onClick.AddListener(BatalkanAntrean);
    }

    void Update()
    {
        if (panelMesin.activeSelf && mesinYangSedangDibuka != null)
        {
            if (mesinYangSedangDibuka.sedangProduksi)
                teksTimerAktif.text = "Sisa Waktu: " + mesinYangSedangDibuka.sisaWaktu.ToString("F1") + "s\nAntrean: " + mesinYangSedangDibuka.antreanBarang.Count;
            else
                teksTimerAktif.text = "Mesin Nganggur";

            // Tambahan: notifikasi jika tidak ada pekerja
            if (mesinYangSedangDibuka.jumlahPekerjaAktif == 0)
                teksTimerAktif.text += "\n<color=red>Tidak ada pekerja!</color>";

            UpdateInfoPekerja();
        }
    }

    void BatalkanAntrean()
    {
        if (mesinYangSedangDibuka != null)
        {
            mesinYangSedangDibuka.BatalkanAntrean();
            UpdateAntrean();
        }
    }

    public void UpdateAntrean()
    {
        if (kontenAntrean == null) return;

        // Hapus semua anak
        foreach (Transform child in kontenAntrean)
            Destroy(child.gameObject);

        if (mesinYangSedangDibuka == null) return;

        var antrean = mesinYangSedangDibuka.antreanBarang;
        if (antrean == null || antrean.Count == 0) return;

        // Hitung jumlah per jenis barang
        Dictionary<string, int> counts = new Dictionary<string, int>();
        foreach (string item in antrean)
        {
            if (counts.ContainsKey(item)) counts[item]++;
            else counts[item] = 1;
        }

        // Tampilkan setiap jenis barang
        foreach (var kvp in counts)
        {
            GameObject go = Instantiate(prefabItemAntrean, kontenAntrean);
            ItemAntreanUI ui = go.GetComponent<ItemAntreanUI>();
            if (ui != null)
            {
                Sprite sprite = EconomyManager.instance.AmbilSpriteBarang(kvp.Key);
                ui.SetData(sprite, kvp.Value, kvp.Key);
            }
        }
    }

    void UpdateInfoPekerja()
    {
        MesinPabrik mesin = mesinYangSedangDibuka;
        bool adaPekerja = false;

        if (mesin.pekerjaMesinAktif != null)
        {
            PekerjaMesin p = mesin.pekerjaMesinAktif;
            if (gambarPekerjaAktif != null && p.fotoPekerja != null)
            {
                gambarPekerjaAktif.sprite = p.fotoPekerja;
                gambarPekerjaAktif.enabled = true;
            }
            if (teksNamaPekerjaAktif != null) teksNamaPekerjaAktif.text = p.namaPekerja;
            if (teksSisaWaktuPekerja != null) teksSisaWaktuPekerja.text = $"Sisa: {p.sisaWaktuKerja.ToString("F0")}s";
            adaPekerja = true;
        }
        else if (mesin.pekerjaMebelAktif != null)
        {
            PekerjaMebel p = mesin.pekerjaMebelAktif;
            if (gambarPekerjaAktif != null && p.fotoPekerja != null)
            {
                gambarPekerjaAktif.sprite = p.fotoPekerja;
                gambarPekerjaAktif.enabled = true;
            }
            if (teksNamaPekerjaAktif != null) teksNamaPekerjaAktif.text = p.namaPekerja;
            if (teksSisaWaktuPekerja != null) teksSisaWaktuPekerja.text = $"Sisa: {p.sisaWaktuKerja.ToString("F0")}s";
            adaPekerja = true;
        }

        // Jika tidak ada pekerja, tampilkan default
        if (!adaPekerja)
        {
            if (gambarPekerjaAktif != null)
            {
                gambarPekerjaAktif.sprite = defaultFotoPekerja;
                gambarPekerjaAktif.enabled = true;
            }
            if (teksNamaPekerjaAktif != null) teksNamaPekerjaAktif.text = "Belum ada Pekerja";
            if (teksSisaWaktuPekerja != null) teksSisaWaktuPekerja.text = "0s";
        }
    }

    public void BukaPanelMesin(MesinPabrik mesin)
    {
        mesinYangSedangDibuka = mesin;
        panelMesin.SetActive(true);
        fieldJumlah.text = "0"; // Selalu reset ke 0 saat dibuka
        UpdateInfoResep();

        if (mesin.jenisMesin == MesinPabrik.Jenis.PotongKayu)
        {
            if (tombolPesanKursi != null) tombolPesanKursi.SetActive(false);
            if (tombolPesanMeja != null) tombolPesanMeja.SetActive(false);
            if (tombolPesanPotong != null) tombolPesanPotong.SetActive(true);
            if (tombolPesanPapanHalus != null) tombolPesanPapanHalus.SetActive(true);
        }
        else if (mesin.jenisMesin == MesinPabrik.Jenis.Mebel)
        {
            if (tombolPesanKursi != null) tombolPesanKursi.SetActive(true);
            if (tombolPesanMeja != null) tombolPesanMeja.SetActive(true);
            if (tombolPesanPotong != null) tombolPesanPotong.SetActive(false);
            if (tombolPesanPapanHalus != null) tombolPesanPapanHalus.SetActive(false);
        }
    }

    // --- LOGIKA INPUT BACA LANGSUNG ---
    private int AmbilAngkaDariField()
    {
        // Langsung baca teks dari UI, paling aman dari bug!
        if (fieldJumlah != null && int.TryParse(fieldJumlah.text, out int hasil))
        {
            return Mathf.Max(0, hasil);
        }
        return 0;
    }

    public void TombolPlus()
    {
        int current = AmbilAngkaDariField();
        fieldJumlah.text = (current + 1).ToString();
    }

    public void TombolMin()
    {
        int current = AmbilAngkaDariField();
        if (current > 0) fieldJumlah.text = (current - 1).ToString();
    }

    // --- TOMBOL PESAN ---
    public void TombolPesanKursi()
    {
        int jumlah = AmbilAngkaDariField();
        if (mesinYangSedangDibuka != null && jumlah > 0)
        {
            mesinYangSedangDibuka.TambahKeAntrean("Kursi", jumlah);
            fieldJumlah.text = "0"; // Reset setelah ditekan
            UpdateAntrean();
        }
    }

    public void TombolPesanMeja()
    {
        int jumlah = AmbilAngkaDariField();
        if (mesinYangSedangDibuka != null && jumlah > 0)
        {
            mesinYangSedangDibuka.TambahKeAntrean("Meja", jumlah);
            fieldJumlah.text = "0";
            UpdateAntrean();
        }
    }

    public void TombolPesanPotong()
    {
        int jumlah = AmbilAngkaDariField();
        if (mesinYangSedangDibuka != null && jumlah > 0)
        {
            mesinYangSedangDibuka.TambahKeAntrean("Gelondongan", jumlah);
            fieldJumlah.text = "0";
            UpdateAntrean();
        }
    }

    public void TombolPesanPapanHalus()
    {
        int jumlah = AmbilAngkaDariField();
        if (mesinYangSedangDibuka != null && jumlah > 0)
        {
            int butuhListrik = 12 * jumlah;
            int butuhKayu = 1 * jumlah;
            if (EconomyManager.instance.listrik < butuhListrik)
            {
                Debug.LogWarning("Listrik tidak cukup!");
                // Tampilkan pesan ke UI popup jika ada
                return;
            }
            if (EconomyManager.instance.kayuMentah < butuhKayu)
            {
                Debug.LogWarning($"Kayu mentah tidak cukup! Butuh {butuhKayu}, tersedia {EconomyManager.instance.kayuMentah}");
                return;
            }
            mesinYangSedangDibuka.TambahKeAntrean("Papan Halus", jumlah);
            fieldJumlah.text = "0";
            UpdateAntrean();
        }
    }

    private void UpdateInfoResep()
    {
        teksNamaMesin.text = mesinYangSedangDibuka.namaMesin;
        if (mesinYangSedangDibuka.jenisMesin == MesinPabrik.Jenis.PotongKayu)
            teksInfoResep.text = "PAKET PAPAN: 1 Kayu -> 4 Papan Halus (12 Listrik)\n" +
                                "PAKET GELONDONG: 1 Kayu -> 2 Gelondongan (10 Listrik)";
        else
            teksInfoResep.text = "Kursi (1 Gelondongan, 15 Listrik)\nMeja (2 Gelondongan, 25 Listrik)";
    }
}