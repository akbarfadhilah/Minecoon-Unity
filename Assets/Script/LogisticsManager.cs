using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;

public class LogisticsManager : MonoBehaviour
{
    public static LogisticsManager instance;

    // ==================== DATA TRUCK (BARU) ====================
    [System.Serializable]
    public class TruckDataDefinition
    {
        public int level;                  // level truck (0 = level 1)
        public GameObject prefabTruck;     // prefab 3D truck
        public Sprite fotoTruck;           // foto untuk UI
        public int kapasitasPoin;          // kapasitas dalam poin (1 barang besar = 8 poin)
        public float konsumsiBensinPerJarak; // bensin per 1 km
        public int hargaUpgrade;           // biaya upgrade ke level berikutnya (0 jika maks)
    }

    [Header("Data Truck (isi di Inspector)")]
    public TruckDataDefinition[] daftarTruck;
    public int indexTruckSaatIni = 0;
    private bool pengirimanSelesai = false;

    // ==================== DATA RUTE & SUPIR (TETAP) ====================
    public Rute[] daftarRute;
    public SupirData[] daftarSupir;

    // ==================== UI PANEL PENJUALAN ====================
    public GameObject panelPenjualan;
    public Transform kontenRute;
    public Transform kontenSupir;
    public GameObject prefabRuteItem;
    public GameObject prefabSupirItem;

    // Elemen UI Truck (di panel penjualan) - HANYA SATU TRUCK
    public TextMeshProUGUI teksNamaTruck;
    public TextMeshProUGUI teksLevelTruck;
    public TextMeshProUGUI teksKapasitasTruck;
    public TextMeshProUGUI teksBensinTruck;
    public Image gambarTruck;
    public Button tombolUpgradeTruck;          // Tombol upgrade (tanpa tombol pilih)
    public GameObject spriteMaxLevel;          // sprite yang muncul saat level maks

    // ==================== PANEL MUATAN ====================
    public Transform kontenBarang;
    public GameObject prefabBarangMuatan;
    public TextMeshProUGUI teksKapasitasTruk;
    public TextMeshProUGUI teksMuatanSaatIni;
    public TextMeshProUGUI teksTotalPendapatan;
    public TextMeshProUGUI teksInfoTrukTerpilih;
    public Button tombolResetMuatan;
    public Button tombolLanjut;

    // ==================== PANEL UPGRADE ====================
    [Header("Panel Upgrade")]
    public GameObject panelUpgradeTruck;
    public TextMeshProUGUI teksUpgradeInfo;
    public Button tombolUpgradeConfirm;
    public Image gambarTruckUpgrade;

    // ==================== PANEL KONFIRMASI UPGRADE ====================
    public GameObject panelKonfirmasiUpgrade;
    public TextMeshProUGUI teksKonfirmasi;
    public Button tombolYa;
    public Button tombolTidak;

    // ==================== REFERENSI LAIN ====================
    public GameObject panelNotifikasi;
    public TextMeshProUGUI teksNotifikasi;
    public TextMeshProUGUI teksEstimasiPerjalanan;
    public TextMeshProUGUI teksTotalModal;

    // ==================== DATA SEMENTARA ====================
    private Rute ruteTerpilih;
    private SupirData supirTerpilih;
    private List<ItemMuatanSementara> muatanSementara = new List<ItemMuatanSementara>();
    private int totalPoinMuatan = 0;
    private int totalPendapatan = 0;

    private Button tombolRuteTerpilihObj;
    private Button tombolSupirTerpilihObj;
    private List<BarangMuatanUI> daftarBarangUI = new List<BarangMuatanUI>();
    private Coroutine perjalananCoroutine;
    private GameObject currentTruckObject;

    [Header("Titik Truck (untuk pergerakan)")]
    public Transform titikParkir;
    public Transform titikTujuan;
    public Transform titikSpawn;

    [Header("Panel Countdown")]
    public GameObject panelCountdown;
    public TextMeshProUGUI teksCountdown;
    public Button tombolCloseCountdown;   // <-- Tombol close di dalam panel countdown

    private int pendingCoinReward = 0;
    private int pendingXPReward = 0;
    private bool isTrukReadyToCollect = false;

    [Header("Harga Bensin per Liter")]
    public float hargaBensinPerLiter = 8000f;

    private Dictionary<SupirData, float> supirCooldownTimers = new Dictionary<SupirData, float>();
    private float updateTimer = 0f;


    void Awake()
    {
        instance = this;
        panelPenjualan.SetActive(false);
        panelUpgradeTruck.SetActive(false);
        panelKonfirmasiUpgrade.SetActive(false);
        if (spriteMaxLevel != null) spriteMaxLevel.SetActive(false);

        if (panelCountdown != null) panelCountdown.SetActive(false);
    }

    void Update()
    {
        // Update teks countdown HANYA jika panel countdown sedang aktif
        if (panelCountdown != null && panelCountdown.activeSelf && currentTruckObject != null)
        {
            TrukPengiriman tp = currentTruckObject.GetComponent<TrukPengiriman>();
            if (tp != null && tp.IsDelivering)
            {
                int detik = Mathf.CeilToInt(tp.RemainingTime);
                teksCountdown.text = $"Truk dalam perjalanan... {detik}s tersisa";
            }
        }

        UpdateSupirCooldown();

        // Update UI supir setiap 0.5 detik
        updateTimer += Time.deltaTime;
        if (updateTimer >= 0.5f)
        {
            updateTimer = 0f;
            UpdateSupirUI();
        }
        // Tidak ada lagi pengaturan interactable tombol di sini
    }

    void Start()
    {
        SiapkanDaftarBarang();
        tombolResetMuatan.onClick.AddListener(ResetMuatan);
        tombolLanjut.onClick.AddListener(LanjutkanPengiriman);
        tombolUpgradeTruck.onClick.AddListener(BukaPanelUpgrade);
        tombolYa.onClick.AddListener(KonfirmasiUpgradeYa);
        tombolTidak.onClick.AddListener(TutupKonfirmasi);
        tombolUpgradeConfirm.onClick.AddListener(BukaKonfirmasiUpgrade);

        if (daftarTruck.Length > 0)
        {
            indexTruckSaatIni = 0;
            UpdateTampilanTruck();
            SpawnTruckObject(indexTruckSaatIni);
        }
        else
            Debug.LogError("Daftar truck kosong! Isi di Inspector.");

        // Pastikan panel countdown tidak aktif di awal
        if (panelCountdown != null) panelCountdown.SetActive(false);

        // Pasang listener untuk tombol close
        if (tombolCloseCountdown != null)
            tombolCloseCountdown.onClick.AddListener(() => {
                if (panelCountdown != null) panelCountdown.SetActive(false);
            });
    }

    void UpdateSupirCooldown()
    {
        List<SupirData> keys = new List<SupirData>(supirCooldownTimers.Keys);
        foreach (var supir in keys)
        {
            if (supirCooldownTimers[supir] > 0)
            {
                supirCooldownTimers[supir] -= Time.deltaTime;
                if (supirCooldownTimers[supir] < 0) supirCooldownTimers[supir] = 0;
            }
        }
    }

    void UpdateSupirUI()
    {
        if (kontenSupir == null) return;
        foreach (Transform child in kontenSupir)
        {
            SupirItemUI ui = child.GetComponent<SupirItemUI>();
            if (ui != null)
            {
                SupirData data = ui.GetSupirData();
                if (data != null)
                {
                    if (supirCooldownTimers.ContainsKey(data) && supirCooldownTimers[data] > 0)
                    {
                        int detik = Mathf.CeilToInt(supirCooldownTimers[data]);
                        ui.SetTextCooldown($"TUNGGU {detik}s");
                        ui.SetInteractable(false);
                    }
                    else
                    {
                        ui.SetTextCooldown($"Upah: {data.upah.ToString("#,0").Replace(",", ".")}");
                        ui.SetInteractable(true);
                    }
                }
            }
        }
    }

    public void TombolBukaPenjualan_Diklik()
    {
        // Cek apakah truk sedang berjalan
        if (currentTruckObject != null)
        {
            TrukPengiriman tp = currentTruckObject.GetComponent<TrukPengiriman>();
            if (tp != null && tp.IsDelivering)
            {
                // Tampilkan panel countdown
                if (panelCountdown != null)
                {
                    panelCountdown.SetActive(true);
                    int detik = Mathf.CeilToInt(tp.RemainingTime);
                    teksCountdown.text = $"Truk dalam perjalanan... {detik}s tersisa";
                }
                return; // keluar, jangan buka panel penjualan
            }
        }
        // Jika tidak ada truk atau truk tidak berjalan, buka panel penjualan seperti biasa
        BukaPanelPenjualan();
    }

    // ==================== METODE UNTUK MENGAMBIL REWARD ====================
    public void AmbilRewardDariTruk()
    {
        if (!isTrukReadyToCollect) return;

        // === 1. HITUNG BONUS ACAK (5% - 60%) ===
        float bonusPersen = Random.Range(0.05f, 0.60f);
        int bonusKoin = Mathf.RoundToInt(pendingCoinReward * bonusPersen);
        int totalKoin = pendingCoinReward + bonusKoin;

        // === 2. BERIKAN REWARD TOTAL ===
        EconomyManager.instance.TambahKoin(totalKoin);
        EconomyManager.instance.TambahXP(pendingXPReward);

        // === 3. FORMAT ANGKA (titik ribuan) ===
        string totalFormatted = totalKoin.ToString("#,0").Replace(",", ".");
        string bonusFormatted = bonusKoin.ToString("#,0").Replace(",", ".");
        string bonusPersenFormatted = (bonusPersen * 100).ToString("F0"); // tanpa desimal

        // === 4. TAMPILKAN NOTIFIKASI ===
        TampilkanPesan(
            $"Pendapatan + Rp. {totalFormatted} (Bonus {bonusPersenFormatted}%! Rp. {bonusFormatted}) dan +{pendingXPReward} XP!",
            3f
        );

        // === 5. RESET MUATAN ===
        muatanSementara.Clear();
        totalPoinMuatan = 0;
        totalPendapatan = 0;
        UpdateTampilanMuatan();
        UpdateSemuaBarang();

        // === 6. RESET STATUS ===
        isTrukReadyToCollect = false;
        pendingCoinReward = 0;
        pendingXPReward = 0;

        // Matikan efek kuning pada truk
        if (currentTruckObject != null)
        {
            TrukPengiriman tp = currentTruckObject.GetComponent<TrukPengiriman>();
            if (tp != null) tp.SetReadyToCollect(false);
        }
    }

    public void SpawnTruckObject(int index)
    {
        if (currentTruckObject != null) Destroy(currentTruckObject);

        TruckDataDefinition data = daftarTruck[index];
        if (data.prefabTruck != null)
        {
            currentTruckObject = Instantiate(data.prefabTruck, titikParkir.position, Quaternion.identity);

            TrukPengiriman tp = currentTruckObject.GetComponent<TrukPengiriman>();
            if (tp == null) tp = currentTruckObject.AddComponent<TrukPengiriman>();
            tp.titikParkir = titikParkir;
            tp.titikTujuan = titikTujuan;
            tp.titikSpawn = titikSpawn;
            tp.SetNamaTruk("Truck Level " + (data.level + 1));

            // Validasi titikSpawn
            if (titikSpawn == null)
                Debug.LogError("titikSpawn belum di-assign di LogisticsManager!");
        }
    }


    public void UpdateTampilanTruck()
    {
        TruckDataDefinition data = daftarTruck[indexTruckSaatIni];
        teksNamaTruck.text = "Truck Level " + (data.level + 1);
        teksLevelTruck.text = "Level: " + (data.level + 1);
        teksKapasitasTruck.text = "Kapasitas: " + data.kapasitasPoin;
        teksBensinTruck.text = "Bensin/km: " + data.konsumsiBensinPerJarak.ToString("0.##");
        if (gambarTruck != null && data.fotoTruck != null)
            gambarTruck.sprite = data.fotoTruck;

        // Cek apakah level maks
        bool isMax = (indexTruckSaatIni >= daftarTruck.Length - 1);
        tombolUpgradeTruck.gameObject.SetActive(!isMax);
        if (spriteMaxLevel != null) spriteMaxLevel.SetActive(isMax);

        UpdateTampilanMuatan();
        UpdateSemuaBarang();
    }

    // ==================== UPGRADE ====================

    void BukaPanelUpgrade()
    {
        if (indexTruckSaatIni >= daftarTruck.Length - 1) return;

        panelUpgradeTruck.SetActive(true);

        int nextIndex = indexTruckSaatIni + 1;
        TruckDataDefinition current = daftarTruck[indexTruckSaatIni];
        TruckDataDefinition next = daftarTruck[nextIndex];

        // Update info teks
        string biayaFormatted = next.hargaUpgrade.ToString("#,0").Replace(",", ".");
        teksUpgradeInfo.text =
            $"Level: {current.level + 1} >> {next.level + 1}\n" +
            $"Kapasitas:\n{current.kapasitasPoin} >> {next.kapasitasPoin}\n" +
            $"Bensin/km:\n{current.konsumsiBensinPerJarak.ToString("0.##")} >> {next.konsumsiBensinPerJarak.ToString("0.##")}\n" +
            $"Biaya Upgrade:\nRp. {biayaFormatted}";

        // ===== TAMPILKAN FOTO TRUCK LEVEL BERIKUTNYA =====
        if (gambarTruckUpgrade != null)
        {
            if (next.fotoTruck != null)
            {
                gambarTruckUpgrade.sprite = next.fotoTruck;
                gambarTruckUpgrade.enabled = true;
            }
            else
            {
                gambarTruckUpgrade.enabled = false; // jika tidak ada foto, sembunyikan
            }
        }
    }

    void BukaKonfirmasiUpgrade()
    {
        panelKonfirmasiUpgrade.SetActive(true);
        teksKonfirmasi.text = "Apakah anda yakin ingin mengupgrade truck anda?";
        tombolYa.gameObject.SetActive(true);
        tombolTidak.gameObject.SetActive(true);
    }

    void TutupKonfirmasi()
    {
        panelKonfirmasiUpgrade.SetActive(false);
    }

    void KonfirmasiUpgradeYa()
    {
        int nextIndex = indexTruckSaatIni + 1;
        if (nextIndex >= daftarTruck.Length)
        {
            teksKonfirmasi.text = "Truck sudah level maksimum!";
            tombolYa.gameObject.SetActive(false);
            tombolTidak.gameObject.SetActive(false);
            return;
        }
        int biaya = daftarTruck[nextIndex].hargaUpgrade;
        if (EconomyManager.instance.koin >= biaya)
        {
            EconomyManager.instance.koin -= biaya;
            indexTruckSaatIni = nextIndex;
            // Jangan update UI dulu, nanti setelah animasi
            panelUpgradeTruck.SetActive(false);
            panelKonfirmasiUpgrade.SetActive(false);
            StartCoroutine(ProsesUpgrade());
        }
        else
        {
            teksKonfirmasi.text = "Waduh.. Uang nya ga cukup nih!";
            tombolYa.gameObject.SetActive(false);
            tombolTidak.gameObject.SetActive(false);
            StartCoroutine(AutoTutupKonfirmasi(2f));
        }
    }

    IEnumerator AutoTutupKonfirmasi(float delay)
    {
        yield return new WaitForSeconds(delay);
        panelKonfirmasiUpgrade.SetActive(false);
    }

    IEnumerator ProsesUpgrade()
    {
        // Tampilkan pesan sukses
        panelKonfirmasiUpgrade.SetActive(true);
        teksKonfirmasi.text = "Bagus! dalam 3 Detik anda akan diarahkan keluar secara otomatis!";
        tombolYa.gameObject.SetActive(false);
        tombolTidak.gameObject.SetActive(false);
        yield return new WaitForSeconds(3f);

        // Tutup semua panel
        panelKonfirmasiUpgrade.SetActive(false);
        panelUpgradeTruck.SetActive(false);
        panelPenjualan.SetActive(false);

        // Mulai animasi upgrade truck
        yield return StartCoroutine(AnimasiUpgradeTruck());

        // Setelah animasi selesai, update UI truck
        UpdateTampilanTruck();
    }

    IEnumerator AnimasiUpgradeTruck()
    {
        if (currentTruckObject == null) yield break;

        // 1. Simpan truck lama
        GameObject truckLama = currentTruckObject;
        Vector3 baseScaleLama = truckLama.transform.localScale;

        // 2. Buat truck baru (tapi belum aktif)
        int nextIndex = indexTruckSaatIni; // index sudah di-update
        TruckDataDefinition dataBaru = daftarTruck[nextIndex];
        GameObject truckBaru = Instantiate(dataBaru.prefabTruck, titikParkir.position, Quaternion.identity);
        truckBaru.SetActive(false); // sembunyikan dulu
                                    // Tambahkan komponen TrukPengiriman dan set titik
        TrukPengiriman tp = truckBaru.GetComponent<TrukPengiriman>();
        if (tp == null) tp = truckBaru.AddComponent<TrukPengiriman>();
        tp.titikParkir = titikParkir;
        tp.titikTujuan = titikTujuan;
        tp.titikSpawn = titikSpawn;
 
        tp.SetNamaTruk("Truck Level " + (dataBaru.level + 1));
        // Simpan skala normal truck baru
        Vector3 baseScaleBaru = truckBaru.transform.localScale;

        // 3. Animasi truck lama
        float faktorBesar = 4f / 3f; // 3 -> 4

        // 0.0 - 0.2 : scale 1 -> 4/3 (membesar)
        float t = 0;
        float durasi1 = 0.2f;
        while (t < durasi1)
        {
            t += Time.deltaTime;
            float progress = t / durasi1;
            float faktor = Mathf.Lerp(1f, faktorBesar, progress);
            truckLama.transform.localScale = baseScaleLama * faktor;
            yield return null;
        }
        truckLama.transform.localScale = baseScaleLama * faktorBesar;

        // 0.2 - 0.7 : scale 4/3 -> 0 (mengecil hingga hilang)
        t = 0;
        float durasi2 = 0.5f;
        while (t < durasi2)
        {
            t += Time.deltaTime;
            float progress = t / durasi2;
            float faktor = Mathf.Lerp(faktorBesar, 0f, progress);
            truckLama.transform.localScale = baseScaleLama * faktor;
            yield return null;
        }
        truckLama.transform.localScale = Vector3.zero;

        // Hancurkan truck lama
        Destroy(truckLama);

        // 4. Aktifkan truck baru dengan scale 0
        truckBaru.SetActive(true);
        truckBaru.transform.localScale = Vector3.zero;
        currentTruckObject = truckBaru; // set sebagai current

        // 5. Animasi truck baru
        // 0.7 - 1.0 : scale 0 -> 4/3 (membesar)
        t = 0;
        float durasi3 = 0.3f;
        while (t < durasi3)
        {
            t += Time.deltaTime;
            float progress = t / durasi3;
            float faktor = Mathf.Lerp(0f, faktorBesar, progress);
            truckBaru.transform.localScale = baseScaleBaru * faktor;
            yield return null;
        }
        truckBaru.transform.localScale = baseScaleBaru * faktorBesar;

        // 1.0 - 1.5 : scale 4/3 -> 1 (kembali normal)
        t = 0;
        float durasi4 = 0.5f;
        while (t < durasi4)
        {
            t += Time.deltaTime;
            float progress = t / durasi4;
            float faktor = Mathf.Lerp(faktorBesar, 1f, progress);
            truckBaru.transform.localScale = baseScaleBaru * faktor;
            yield return null;
        }
        truckBaru.transform.localScale = baseScaleBaru;

        // Truck baru siap digunakan
    }

    void SiapkanDaftarBarang()
    {
        string[] semuaBarang = { "Kayu Mentah", "Kayu Gelondongan", "Papan Halus", "Kursi", "Meja",
                                  "Alat makan", "Telenan", "Piring", "Ulekan", "Patung Kayu", "Alat masak", "Wadah perhiasan" };
        foreach (string barang in semuaBarang)
        {
            GameObject baris = Instantiate(prefabBarangMuatan, kontenBarang);
            BarangMuatanUI ui = baris.GetComponent<BarangMuatanUI>();
            if (ui != null)
            {
                ui.Inisialisasi(barang, this);
                daftarBarangUI.Add(ui);
            }
        }
        UpdateSemuaBarang();
    }

    public void UpdateSemuaBarang()
    {
        foreach (BarangMuatanUI ui in daftarBarangUI)
        {
            int stok = EconomyManager.instance.AmbilStok(ui.teksNamaBarang.text);
            var muatan = muatanSementara.Find(m => m.namaItem == ui.teksNamaBarang.text);
            int jumlahDiangkut = muatan != null ? muatan.jumlah : 0;
            ui.UpdateTampilan(stok, jumlahDiangkut);
        }
    }

    public void BukaPanelPenjualan()
    {
        panelPenjualan.SetActive(true);
        TampilkanSemuaPilihan();
        UpdateSemuaBarang();
        UpdateSupirUI();
    }

    void TampilkanSemuaPilihan()
    {
        TampilkanPilihRute();
        TampilkanPilihSupir();
        TampilkanMuatanBarang();
    }

    void TampilkanPilihRute()
    {
        BersihkanKonten(kontenRute);
        foreach (Rute rute in daftarRute)
        {
            GameObject itemObj = Instantiate(prefabRuteItem, kontenRute);
            RuteItemUI ui = itemObj.GetComponent<RuteItemUI>();
            ui.Inisialisasi(rute, this);
        }
    }

    public void PilihRute(Rute rute, Button btn)
    {
        ruteTerpilih = rute;
        SetButtonColor(tombolRuteTerpilihObj, false);
        tombolRuteTerpilihObj = btn;
        SetButtonColor(btn, true);
        UpdateEstimasiDanModal();
    }

    void TampilkanPilihSupir()
    {
        BersihkanKonten(kontenSupir);
        if (daftarSupir == null) return;
        foreach (SupirData supir in daftarSupir)
        {
            GameObject itemObj = Instantiate(prefabSupirItem, kontenSupir);
            SupirItemUI ui = itemObj.GetComponent<SupirItemUI>();
            ui.Inisialisasi(supir, this);
        }
        UpdateSupirUI();
    }

    public void PilihSupir(SupirData supir, Button btn)
    {
        // Cek cooldown
        if (supirCooldownTimers.ContainsKey(supir) && supirCooldownTimers[supir] > 0)
        {
            TampilkanPesan("Maaf supir sedang istirahat!", 2f);
            return;
        }

        supirTerpilih = supir;
        SetButtonColor(tombolSupirTerpilihObj, false);
        tombolSupirTerpilihObj = btn;
        SetButtonColor(btn, true);
        UpdateEstimasiDanModal();
    }

    void UpdateEstimasiDanModal()
    {
        if (ruteTerpilih == null || supirTerpilih == null)
        {
            teksEstimasiPerjalanan.text = "Pilih rute, truk, dan supir terlebih dahulu.";
            teksTotalModal.text = "";
            return;
        }

        TruckDataDefinition dataTruck = daftarTruck[indexTruckSaatIni];
        int kebutuhanBensin = Mathf.CeilToInt(dataTruck.konsumsiBensinPerJarak * ruteTerpilih.jarakTempuh);
        int upah = supirTerpilih.upah;
        float totalWaktu = supirTerpilih.waktuPerJarak * ruteTerpilih.jarakTempuh * 2;

        teksEstimasiPerjalanan.text = $"Estimasi Perjalanan: {totalWaktu:F0} detik";
        string upahFormatted = upah.ToString("#,0").Replace(",", ".");
        teksTotalModal.text = $"Modal : Bensin {kebutuhanBensin} L & Rp. {upahFormatted}";
    }

    void SetButtonColor(Button btn, bool selected)
    {
        if (btn == null) return;
        Image img = btn.GetComponent<Image>();
        if (img != null) img.color = selected ? Color.green : Color.white;
    }

    void TampilkanMuatanBarang()
    {
        muatanSementara.Clear();
        totalPoinMuatan = 0;
        totalPendapatan = 0;

        UpdateTampilanMuatan();
        UpdateSemuaBarang();
    }

    public static int DapatkanBobotItem(string namaItem)
    {
        switch (namaItem)
        {
            case "Kayu Mentah": return 8;
            case "Kayu Gelondongan": case "Papan Halus": return 4;
            case "Kursi": case "Meja": return 2;
            case "Alat makan":
            case "Telenan":
            case "Piring":
            case "Ulekan":
            case "Patung Kayu":
            case "Alat masak":
            case "Wadah perhiasan": return 1;
            default: return 1;
        }
    }

    public void TambahBarangKeMuatan(string namaItem, int jumlah)
    {
        if (daftarTruck.Length == 0)
        {
            TampilkanPesan("Truk belum tersedia!", 2f);
            return;
        }

        int stok = EconomyManager.instance.AmbilStok(namaItem);

        // Hitung berapa banyak barang ini yang sudah dimuat ke truk
        var existing = muatanSementara.Find(m => m.namaItem == namaItem);
        int alreadyLoaded = existing != null ? existing.jumlah : 0;
        int remainingStock = stok - alreadyLoaded;

        // Jika sudah tidak ada stok tersisa, tolak
        if (remainingStock <= 0)
        {
            TampilkanPesan($"Stok {namaItem} habis!", 2f);
            return;
        }

        // Jika jumlah yang diminta melebihi stok tersisa, batasi dan beri notifikasi
        if (jumlah > remainingStock)
        {
            TampilkanPesan($"Total barang mu tidak sesuai dengan yang di gudang! (Stok {namaItem} tersisa {remainingStock})", 2f);
            // Batasi jumlah agar tidak melebihi stok
            jumlah = remainingStock;
        }

        int bobot = DapatkanBobotItem(namaItem);
        int kapasitas = daftarTruck[indexTruckSaatIni].kapasitasPoin;

        // Cek apakah muatan masih muat
        if (totalPoinMuatan + (bobot * jumlah) <= kapasitas)
        {
            if (existing != null)
                existing.jumlah += jumlah;
            else
                muatanSementara.Add(new ItemMuatanSementara(namaItem, jumlah));

            totalPoinMuatan += bobot * jumlah;
            UpdateTampilanMuatan();
            UpdateSemuaBarang(); // refresh UI stok (tapi stok gudang tidak berubah)
        }
        else
        {
            TampilkanPesan("Truk penuh!", 2f);
        }
    }

    public void KurangiBarangDariMuatan(string namaItem, int jumlah)
    {
        var existing = muatanSementara.Find(m => m.namaItem == namaItem);
        if (existing != null)
        {
            int kurangi = Mathf.Min(jumlah, existing.jumlah);
            existing.jumlah -= kurangi;
            if (existing.jumlah <= 0)
                muatanSementara.Remove(existing);

            int bobot = DapatkanBobotItem(namaItem);
            totalPoinMuatan -= bobot * kurangi;
            UpdateTampilanMuatan();
            UpdateSemuaBarang();
        }
    }

    void HitungTotalPendapatan()
    {
        totalPendapatan = 0;
        foreach (var item in muatanSementara)
        {
            int harga = EconomyManager.instance.AmbilHargaJual(item.namaItem);
            totalPendapatan += harga * item.jumlah;
        }
    }

    private int HitungTotalPendapatanLengkap()
    {
        // Pendapatan dari barang
        int pendapatanBarang = 0;
        foreach (var item in muatanSementara)
        {
            int harga = EconomyManager.instance.AmbilHargaJual(item.namaItem);
            pendapatanBarang += harga * item.jumlah;
        }

        int total = pendapatanBarang;

        // Tambahan dari bensin dan upah (jika rute & supir sudah dipilih)
        if (ruteTerpilih != null && supirTerpilih != null)
        {
            TruckDataDefinition dataTruck = daftarTruck[indexTruckSaatIni];
            int kebutuhanBensin = Mathf.CeilToInt(dataTruck.konsumsiBensinPerJarak * ruteTerpilih.jarakTempuh);
            int upah = supirTerpilih.upah;

            int pendapatanDariBensin = Mathf.RoundToInt((kebutuhanBensin / 2f) * hargaBensinPerLiter);
            int pendapatanDariUpah = Mathf.RoundToInt(upah / 2f);
            total += pendapatanDariBensin + pendapatanDariUpah;
        }

        return total;
    }

    void UpdateTampilanMuatan()
    {
        if (daftarTruck.Length == 0)
        {
            teksKapasitasTruk.text = "Kapasitas Truk: -";
            teksInfoTrukTerpilih.text = "Truk: belum dipilih";
            teksMuatanSaatIni.text = "";
            teksTotalPendapatan.text = "Total Pendapatan: Rp. ";
            return;
        }

        TruckDataDefinition dataTruck = daftarTruck[indexTruckSaatIni];
        int kapasitas = dataTruck.kapasitasPoin;
        teksKapasitasTruk.text = $"Kapasitas Truk: {totalPoinMuatan} / {kapasitas} poin";
        teksInfoTrukTerpilih.text = $"Truk: Level {dataTruck.level + 1} | Rute: {(ruteTerpilih != null ? ruteTerpilih.namaKota : "-")}";

        string daftar = "";
        foreach (var item in muatanSementara)
            daftar += $"{item.jumlah} - {item.namaItem}\n";
        teksMuatanSaatIni.text = daftar;

        // ===== TOTAL PENDAPATAN REAL-TIME =====
        totalPendapatan = HitungTotalPendapatanLengkap();
        string totalFormatted = totalPendapatan.ToString("#,0").Replace(",", ".");
        teksTotalPendapatan.text = $"Total Pendapatan: Rp. {totalFormatted}";
    }

    public void ResetMuatan()
    {
        muatanSementara.Clear();
        totalPoinMuatan = 0;
        totalPendapatan = 0;
        foreach (BarangMuatanUI ui in daftarBarangUI) ui.ResetInputField();
        UpdateTampilanMuatan();
        UpdateSemuaBarang();
    }

    public void LanjutkanPengiriman()
    {
        if (ruteTerpilih == null) { TampilkanPesan("Pilih rute!", 2f); return; }
        if (supirTerpilih == null) { TampilkanPesan("Pilih supir!", 2f); return; }
        if (muatanSementara.Count == 0) { TampilkanPesan("Muatan kosong!", 2f); return; }

        // Reset flag sebelum pengiriman baru
        pengirimanSelesai = false;

        TruckDataDefinition dataTruck = daftarTruck[indexTruckSaatIni];
        int kebutuhanBensin = Mathf.CeilToInt(dataTruck.konsumsiBensinPerJarak * ruteTerpilih.jarakTempuh);
        int upah = supirTerpilih.upah;

        if (EconomyManager.instance.bensin < kebutuhanBensin) { TampilkanPesan("Bensin tidak cukup!", 2f); return; }
        if (EconomyManager.instance.koin < upah) { TampilkanPesan("Uang tidak cukup untuk bayar supir!", 2f); return; }

        EconomyManager.instance.bensin -= kebutuhanBensin;
        EconomyManager.instance.koin -= upah;

        foreach (var item in muatanSementara)
            EconomyManager.instance.KurangiStok(item.namaItem, item.jumlah);

        // Simpan supir yang dipakai untuk cooldown nanti
        SupirData supirSaatIni = supirTerpilih;

        panelPenjualan.SetActive(false);

        if (currentTruckObject == null)
            SpawnTruckObject(indexTruckSaatIni);

        TrukPengiriman trukKirim = currentTruckObject.GetComponent<TrukPengiriman>();
        if (trukKirim == null) trukKirim = currentTruckObject.AddComponent<TrukPengiriman>();

        trukKirim.titikParkir = titikParkir;
        trukKirim.titikTujuan = titikTujuan;
        trukKirim.titikSpawn = titikSpawn;
        trukKirim.SetNamaTruk("Truck Level " + (dataTruck.level + 1));
        trukKirim.ResetState();

        float estimasiTotal = supirTerpilih.waktuPerJarak * ruteTerpilih.jarakTempuh * 2;
        Debug.Log($"Mulai pengiriman dengan estimasi {estimasiTotal:F2} detik");

        trukKirim.MulaiPengiriman(estimasiTotal,
            () => {
                // Tiba di tujuan – tidak ada reward di sini
                Debug.Log("Truk sampai di tujuan, menunggu kembali.");
            },
            () => {
                // Kembali ke parkir – siapkan reward
                pengirimanSelesai = true;

                pendingCoinReward = totalPendapatan;
                pendingXPReward = totalPoinMuatan;
                isTrukReadyToCollect = true;

                trukKirim.SetReadyToCollect(true);

                // Set cooldown supir yang dipakai
                if (supirSaatIni != null)
                {
                    supirCooldownTimers[supirSaatIni] = supirSaatIni.cooldownDuration;
                    UpdateSupirUI(); // refresh UI
                }

                Debug.Log($"Truk kembali, siap panen: {pendingCoinReward} coin, {pendingXPReward} XP.");
            }
        );

        StartCoroutine(TungguPengirimanSelesai());
    }

    IEnumerator TungguPengirimanSelesai()
    {
        while (!pengirimanSelesai) yield return null;
        // Tidak ada pembersihan di sini – pembersihan dilakukan di AmbilRewardDariTruk()
        Debug.Log("Pengiriman selesai, truk siap dipanen.");
    }

    void TampilkanPesan(string pesan, float durasi)
    {
        if (panelNotifikasi != null)
        {
            teksNotifikasi.text = pesan;
            panelNotifikasi.SetActive(true);
            Invoke(nameof(HideNotifikasi), durasi);
        }
    }

    void HideNotifikasi() => panelNotifikasi.SetActive(false);

    void BersihkanKonten(Transform konten)
    {
        foreach (Transform child in konten) Destroy(child.gameObject);
    }

    [System.Serializable]
    public class ItemMuatanSementara
    {
        public string namaItem;
        public int jumlah;
        public ItemMuatanSementara(string nama, int jml) { namaItem = nama; jumlah = jml; }
    }
}