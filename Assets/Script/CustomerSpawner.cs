using UnityEngine;
using System.Collections.Generic;
using UnityEngine.EventSystems;

public class CustomerSpawner : MonoBehaviour
{
    [Header("Daftar Pelanggan (isi di Inspector)")]
    public CustomerData[] daftarPelanggan;

    [Header("Titik Jalur")]
    public Transform titikSpawn;
    public Transform titikBerhenti;
    public Transform[] titikWaypoints;   // titik belok SETELAH stop
    public Transform titikFinish;

    [Header("UI")]
    public GameObject panelOrderPrefab;

    private CustomerTruck truckAktif;
    private float timer;

    private string[] semuaBarang = { "Kayu Mentah", "Kayu Gelondongan", "Papan Halus", "Kursi", "Meja",
                                     "Alat makan", "Telenan", "Piring", "Ulekan", "Patung Kayu",
                                     "Alat masak", "Wadah perhiasan" };

    void Start()
    {
        if (daftarPelanggan == null || daftarPelanggan.Length == 0)
            Debug.LogError("Daftar pelanggan kosong!");
        timer = 0f;
    }

    void Update()
    {
        if (truckAktif == null)
        {
            timer -= Time.deltaTime;
            if (timer <= 0)
                SpawnPelangganAcak();
        }
    }

    void SpawnPelangganAcak()
    {
        int index = Random.Range(0, daftarPelanggan.Length);
        CustomerData customer = daftarPelanggan[index];

        if (customer.prefabTruck == null)
        {
            Debug.LogError($"Pelanggan '{customer.nama}' tidak punya prefab truck!");
            timer = 1f;
            return;
        }

        GameObject truckObj = Instantiate(customer.prefabTruck, titikSpawn.position, titikSpawn.rotation);
        CustomerTruck truck = truckObj.GetComponent<CustomerTruck>();
        if (truck == null) truck = truckObj.AddComponent<CustomerTruck>();

        // Urutan jalur: Spawn -> Stop -> Waypoints -> Finish
        List<Transform> jalur = new List<Transform>();
        jalur.Add(titikSpawn);
        jalur.Add(titikBerhenti);
        if (titikWaypoints != null) jalur.AddRange(titikWaypoints);
        jalur.Add(titikFinish);

        List<ItemPesanan> pesanan = GeneratePesanan(customer);
        int totalHarga = HitungTotalHarga(pesanan);
        int totalXP = HitungTotalXP(pesanan);

        truck.Inisialisasi(this, jalur, titikBerhenti, pesanan,
                   customer.nama, totalHarga, totalXP, panelOrderPrefab, customer.foto, customer);

        truckAktif = truck;
        timer = customer.waktuCooldown;
        Debug.Log($"Pelanggan '{customer.nama}' datang, cooldown {customer.waktuCooldown} detik");
    }

    List<ItemPesanan> GeneratePesanan(CustomerData customer)
    {
        List<string> barangTersedia = new List<string>(semuaBarang);
        List<ItemPesanan> pesanan = new List<ItemPesanan>();
        int jenis = Mathf.Min(customer.jumlahJenisBarangRandom, barangTersedia.Count);
        for (int i = 0; i < jenis; i++)
        {
            int idx = Random.Range(0, barangTersedia.Count);
            string barang = barangTersedia[idx];
            barangTersedia.RemoveAt(idx);
            int jumlah = Random.Range(1, customer.rangeMaksimalPerItem + 1);
            pesanan.Add(new ItemPesanan(barang, jumlah));
        }
        return pesanan;
    }

    int HitungTotalHarga(List<ItemPesanan> pesanan)
    {
        int total = 0;
        foreach (var item in pesanan)
            total += EconomyManager.instance.AmbilHargaJual(item.namaItem) * item.jumlah;
        return total;
    }

    int HitungTotalXP(List<ItemPesanan> pesanan)
    {
        int total = 0;
        foreach (var item in pesanan)
            total += LogisticsManager.DapatkanBobotItem(item.namaItem) * item.jumlah;
        return total;
    }

    public void TruckSelesai(CustomerTruck truck)
    {
        if (truckAktif == truck) truckAktif = null;
    }

    [System.Serializable]
    public class ItemPesanan
    {
        public string namaItem;
        public int jumlah;
        public ItemPesanan(string n, int j) { namaItem = n; jumlah = j; }
    }
}