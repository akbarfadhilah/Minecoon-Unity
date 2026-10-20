using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class BarangMuatanUI : MonoBehaviour
{
    public Image iconProduk;
    public TextMeshProUGUI teksNamaBarang;
    public TextMeshProUGUI teksSisaStok;
    public TMP_InputField inputJumlah;
    public Button tombolMinus;
    public Button tombolPlus;
    public TextMeshProUGUI teksJumlahDiangkut;

    private string namaItem;
    private LogisticsManager manager;

    public void Inisialisasi(string item, LogisticsManager mgr)
    {
        // Cek komponen null
        if (teksNamaBarang == null) Debug.LogError("teksNamaBarang tidak di-assign di prefab!");
        if (inputJumlah == null) Debug.LogError("inputJumlah tidak di-assign!");
        if (tombolPlus == null) Debug.LogError("tombolPlus tidak di-assign!");
        if (tombolMinus == null) Debug.LogError("tombolMinus tidak di-assign!");

        namaItem = item;
        manager = mgr;
        teksNamaBarang.text = item;
        inputJumlah.text = "0";

        if (iconProduk != null && EconomyManager.instance != null)
        {
            iconProduk.sprite = EconomyManager.instance.AmbilSpriteBarang(item);
        }

        // Hapus listener lama untuk menghindari duplikasi
        tombolPlus.onClick.RemoveAllListeners();
        tombolMinus.onClick.RemoveAllListeners();

        tombolPlus.onClick.AddListener(() => Tambah());
        tombolMinus.onClick.AddListener(() => Kurang());
    }

    void Tambah()
    {
        int current = int.Parse(inputJumlah.text);
        current++;
        inputJumlah.text = current.ToString();
        manager.TambahBarangKeMuatan(namaItem, 1);
    }

    void Kurang()
    {
        int current = int.Parse(inputJumlah.text);
        if (current > 0)
        {
            current--;
            inputJumlah.text = current.ToString();
            manager.KurangiBarangDariMuatan(namaItem, 1);
        }
    }

    public void UpdateTampilan(int sisaStok, int jumlahDiangkut)
    {
        if (teksSisaStok != null) teksSisaStok.text = $"Stok: {sisaStok}";
        if (teksJumlahDiangkut != null) teksJumlahDiangkut.text = $"Di truk: {jumlahDiangkut}";
    }

    public void ResetInputField()
    {
        if (inputJumlah != null) inputJumlah.text = "0";
    }
}
