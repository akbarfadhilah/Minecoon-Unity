using UnityEngine;
using TMPro;

public class SellItemRow : MonoBehaviour
{
    [Header("Pengaturan Item")]
    public string namaItem;

    [Header("Referensi UI")]
    public TextMeshProUGUI teksNama;
    public TextMeshProUGUI teksStok;
    public TextMeshProUGUI teksHarga;

    private int hargaSatuan = 0;

    void Start()
    {
        // Validasi referensi UI
        if (teksNama == null) Debug.LogError("teksNama belum di-assign untuk item " + namaItem);
        if (teksStok == null) Debug.LogError("teksStok belum di-assign untuk item " + namaItem);
        if (teksHarga == null) Debug.LogError("teksHarga belum di-assign untuk item " + namaItem);

        teksNama.text = namaItem;

        // Ambil harga dari EconomyManager
        hargaSatuan = EconomyManager.instance.AmbilHargaJual(namaItem);
        if (teksHarga != null)
        {
            // Format angka dengan pemisah ribuan pakai titik
            string hargaFormatted = hargaSatuan.ToString("#,0").Replace(",", ".");
            teksHarga.text = $"Harga: Rp. {hargaFormatted}";
        }

        UpdateStok();
    }

    void Update()
    {
        UpdateStok();
    }

    void UpdateStok()
    {
        if (teksStok != null && EconomyManager.instance != null)
        {
            int stok = EconomyManager.instance.AmbilStok(namaItem);
            teksStok.text = $"{stok}";
        }
    }
}