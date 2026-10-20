using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;
using System.Collections.Generic;

public class UIPanelManagerPmht : MonoBehaviour
{
    public static UIPanelManagerPmht instance;

    [Header("UI Panel Pemahat")]
    public GameObject panelPemahat;
    public TextMeshProUGUI teksNamaMesin;
    public TextMeshProUGUI teksInfoResep;
    public TMP_InputField fieldJumlah;
    public TextMeshProUGUI teksTimerAktif;

    [Header("Tombol Produk")]
    public GameObject tombolAlatMakan;
    public GameObject tombolTelenan;
    public GameObject tombolPiring;
    public GameObject tombolUlekan;
    public GameObject tombolPatungKayu;
    public GameObject tombolAlatMasak;
    public GameObject tombolWadahPerhiasan;

    [Header("Info Pekerja Aktif")]
    public Image gambarPekerjaAktif;
    public TextMeshProUGUI teksNamaPekerjaAktif;
    public TextMeshProUGUI teksSisaWaktuPekerja;

    [Header("Default Placeholder")]
    public Sprite defaultFotoPekerja;

    [Header("Tombol Edit")]
    public Button tombolEdit;

    [Header("Antrean")]
    public Transform kontenAntrean;
    public GameObject prefabItemAntrean;
    public Button tombolBatalAntrean;

    private MesinPemahat mesinYangSedangDibuka;

    void Awake()
    {
        instance = this;
        if (panelPemahat != null) panelPemahat.SetActive(false);

        tombolEdit.onClick.AddListener(() => {
            if (mesinYangSedangDibuka != null && MesinEditor.instance != null)
                MesinEditor.instance.BukaEdit(mesinYangSedangDibuka.gameObject);
        });

        if (tombolBatalAntrean != null)
            tombolBatalAntrean.onClick.AddListener(BatalkanAntrean);
    }

    void Update()
    {
        if (panelPemahat.activeSelf && mesinYangSedangDibuka != null)
        {
            if (mesinYangSedangDibuka.sedangProduksi)
                teksTimerAktif.text = $"Sisa Waktu: {mesinYangSedangDibuka.sisaWaktu:F1}s\nAntrean: {mesinYangSedangDibuka.antreanBarang.Count}";
            else
                teksTimerAktif.text = "Mesin Nganggur";

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
        foreach (Transform child in kontenAntrean) Destroy(child.gameObject);
        if (mesinYangSedangDibuka == null) return;

        var antrean = mesinYangSedangDibuka.antreanBarang;
        if (antrean == null || antrean.Count == 0) return;

        Dictionary<string, int> counts = new Dictionary<string, int>();
        foreach (string item in antrean)
        {
            if (counts.ContainsKey(item)) counts[item]++;
            else counts[item] = 1;
        }

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
        MesinPemahat mesin = mesinYangSedangDibuka;
        bool adaPekerja = false;

        if (mesin.pekerjaPemahatAktif != null)
        {
            PekerjaPemahat p = mesin.pekerjaPemahatAktif;
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

    public void BukaPanelMesin(MesinPemahat mesin)
    {
        mesinYangSedangDibuka = mesin;
        panelPemahat.SetActive(true);
        UpdateAntrean();
        fieldJumlah.text = "0";
        UpdateInfoResep();

        // Aktifkan semua tombol produk
        tombolAlatMakan.SetActive(true);
        tombolTelenan.SetActive(true);
        tombolPiring.SetActive(true);
        tombolUlekan.SetActive(true);
        tombolPatungKayu.SetActive(true);
        tombolAlatMasak.SetActive(true);
        tombolWadahPerhiasan.SetActive(true);
    }

    private int AmbilJumlah()
    {
        if (fieldJumlah != null && int.TryParse(fieldJumlah.text, out int hasil))
            return Mathf.Max(0, hasil);
        return 0;
    }

    public void TombolPlus() => fieldJumlah.text = (AmbilJumlah() + 1).ToString();
    public void TombolMin()
    {
        int cur = AmbilJumlah();
        if (cur > 0) fieldJumlah.text = (cur - 1).ToString();
    }

    // Masing-masing tombol panggil fungsi ini
    public void TombolPesan(string produk)
    {
        int jumlah = AmbilJumlah();
        if (mesinYangSedangDibuka != null && jumlah > 0)
        {
            mesinYangSedangDibuka.TambahKeAntrean(produk, jumlah);
            fieldJumlah.text = "0";
            UpdateAntrean();
        }
    }

    // Wrapper untuk setiap tombol (hubungkan ke Inspector)
    public void TombolAlatMakan() => TombolPesan("Alat makan");
    public void TombolTelenan() => TombolPesan("Telenan");
    public void TombolPiring() => TombolPesan("Piring");
    public void TombolUlekan() => TombolPesan("Ulekan");
    public void TombolPatungKayu() => TombolPesan("Patung Kayu");
    public void TombolAlatMasak() => TombolPesan("Alat masak");
    public void TombolWadahPerhiasan() => TombolPesan("Wadah perhiasan");

    private void UpdateInfoResep()
    {
        teksNamaMesin.text = mesinYangSedangDibuka.namaMesin;
        teksInfoResep.text =
            "Alat makan: 1 Papan + 10 Listrik\n" +
            "Telenan: 1 Papan + 5 Listrik\n" +
            "Piring: 1 Papan + 10 Listrik\n" +
            "Ulekan: 1 Gelondong + 20 Listrik\n" +
            "Patung Kayu: 1 Gelondong + 1 Papan + 30 Listrik\n" +
            "Alat masak: 2 Papan + 15 Listrik\n" +
            "Wadah perhiasan: 3 Papan + 20 Listrik";
    }

    public void TutupPanel()
    {
        panelPemahat.SetActive(false);
        mesinYangSedangDibuka = null;
    }
}