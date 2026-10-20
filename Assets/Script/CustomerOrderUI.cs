using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;

public class CustomerOrderUI : MonoBehaviour
{
    public TextMeshProUGUI teksNamaPelanggan;
    public TextMeshProUGUI teksTotalHarga;
    public TextMeshProUGUI teksPesanPelanggan;
    public Button tombolJual;
    public Button tombolTunggu;
    public Button tombolGaPunya;
    public Image gambarPelanggan;

    [Header("Daftar Pesanan UI")]
    public GameObject prefabItemPesanan;
    public Transform kontenDaftarPesanan;

    private CustomerTruck truck;
    private List<CustomerSpawner.ItemPesanan> daftarPesanan;
    private CustomerData customerData;

    [Header("BGM")]
    public AudioClip bgmPelanggan;

    [Header("SFX")]
    public AudioClip sfxTersedia;
    public AudioClip sfxBarangKosong;
    public AudioClip sfxMenolak;

    void Awake()
    {
        tombolJual.onClick.AddListener(() => CekStokDanJual());
        tombolTunggu.onClick.AddListener(() =>
        {
            if (truck != null) truck.Tunggu();
            KembalikanBGM();
            Destroy(gameObject);
        });
        tombolGaPunya.onClick.AddListener(() => SkipPenjualan());
    }

    void KembalikanBGM()
    {
        if (BGMManager.instance != null && BGMManager.instance.bgmUtama != null)
            BGMManager.instance.ChangeBGM(BGMManager.instance.bgmUtama, 0.8f);
    }

    void PlaySFX(AudioClip clip)
    {
        if (SFXManager.instance != null && clip != null)
            SFXManager.instance.PlaySound(clip);
    }

    public void BukaPanel(CustomerTruck tr, string nama, List<CustomerSpawner.ItemPesanan> pesanan,
                          int totalHarga, Sprite foto, CustomerData data)
    {
        if (BGMManager.instance != null && bgmPelanggan != null)
            BGMManager.instance.ChangeBGM(bgmPelanggan, 0.8f);

        truck = tr;
        daftarPesanan = pesanan;
        customerData = data;

        teksNamaPelanggan.text = nama;
        teksPesanPelanggan.text = data.teksKedatangan;
        if (gambarPelanggan != null && foto != null) gambarPelanggan.sprite = foto;
        if (teksTotalHarga != null) teksTotalHarga.text = $"Pendapatan : Rp {totalHarga.ToString("N0", CultureInfo.CreateSpecificCulture("id-ID"))}";

        // Tampilkan daftar pesanan dengan cek stok
        if (kontenDaftarPesanan != null)
        {
            foreach (Transform child in kontenDaftarPesanan)
                Destroy(child.gameObject);

            if (prefabItemPesanan == null)
            {
                Debug.LogError("Prefab Item Pesanan tidak di-assign!");
                return;
            }

            foreach (var item in pesanan)
            {
                GameObject baru = Instantiate(prefabItemPesanan, kontenDaftarPesanan, false);
                ItemPesananUI ui = baru.GetComponent<ItemPesananUI>();
                if (ui != null)
                {
                    Sprite sprite = EconomyManager.instance.AmbilSpriteBarang(item.namaItem);
                    ui.SetData(sprite, $"{item.jumlah}x {item.namaItem}");

                    int stok = EconomyManager.instance.AmbilStok(item.namaItem);
                    if (stok < item.jumlah)
                        ui.SetWarnaTeks(Color.red);
                    else
                        ui.SetWarnaTeks(Color.white);
                }
                else
                {
                    Debug.LogError("Prefab ItemPesanan tidak memiliki komponen ItemPesananUI!");
                }
            }
        }
    }

    void CekStokDanJual()
    {
        string itemKurang = null;
        foreach (var item in daftarPesanan)
        {
            int stok = EconomyManager.instance.AmbilStok(item.namaItem);
            if (stok < item.jumlah)
            {
                itemKurang = item.namaItem;
                break;
            }
        }

        if (itemKurang != null)
        {
            // Stok kosong – mainkan SFX barang kosong
            PlaySFX(sfxBarangKosong);

            teksPesanPelanggan.text = customerData.teksBarangKosong;
            if (gambarPelanggan != null && customerData.fotoMenolak != null)
                gambarPelanggan.sprite = customerData.fotoMenolak;

            // Panel tetap terbuka, tidak ada delay destroy
            return;
        }

        // Sukses – mainkan SFX tersedia
        PlaySFX(sfxTersedia);

        if (truck != null)
        {
            truck.Jual();
            teksPesanPelanggan.text = customerData.teksTersedia;
            if (gambarPelanggan != null && customerData.fotoTersedia != null)
                gambarPelanggan.sprite = customerData.fotoTersedia;
        }

        // Mulai delay 2 detik, lalu tutup panel dan lanjutkan perjalanan
        StartCoroutine(DelayDestroy(2f));
    }

    void SkipPenjualan()
    {
        // Mainkan SFX menolak
        PlaySFX(sfxMenolak);

        if (truck != null)
        {
            truck.Batalkan();
            teksPesanPelanggan.text = customerData.teksMenolak;
            if (gambarPelanggan != null && customerData.fotoMenolak != null)
                gambarPelanggan.sprite = customerData.fotoMenolak;
        }

        // Mulai delay 2 detik, lalu tutup panel dan lanjutkan perjalanan
        StartCoroutine(DelayDestroy(2f));
    }

    IEnumerator DelayDestroy(float delay)
    {
        // Nonaktifkan tombol agar tidak bisa diklik selama delay
        tombolJual.interactable = false;
        tombolTunggu.interactable = false;
        tombolGaPunya.interactable = false;

        yield return new WaitForSeconds(delay);

        // Beritahu truck untuk lanjut perjalanan
        if (truck != null) truck.LanjutkanPerjalanan();

        // Kembalikan BGM dan destroy panel
        KembalikanBGM();
        Destroy(gameObject);
    }
}