using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class EconomyManager : MonoBehaviour
{
    public static EconomyManager instance;

    public int xp = 0;
    public int level = 1;
    public int koin = 150;
    public int listrik = 100;
    public int bensin = 500;
    public int kayuMentah = 0;
    public int kayuGelondongan = 0;
    public int kursi = 0;
    public int meja = 0;
    public int papanHalus = 0;
    public int alatMakan = 0;
    public int telenan = 0;
    public int piring = 0;
    public int ulekan = 0;
    public int patungKayu = 0;
    public int alatMasak = 0;
    public int wadahPerhiasan = 0;

    [Header("UI - Ekonomi")]
    public TextMeshProUGUI teksKoin;
    public TextMeshProUGUI teksBensin;
    public TextMeshProUGUI teksListrik;

    [Header("UI - XP")]
    public TextMeshProUGUI teksXpLevel;      // hanya angka level
    public TextMeshProUGUI teksXpProgress;   // "xp / butuh"
    public Slider progressBarXp;             // progress bar

    [Header("UI - Statistik")]
    public TextMeshProUGUI teksTotalPekerja;
    public TextMeshProUGUI teksTotalLahan;
    public TextMeshProUGUI teksTotalPabrik;

    [Header("Visual / Sprite Barang")]
    public Sprite spriteKayuMentah;
    public Sprite spriteKayuGelondongan;
    public Sprite spritePapanHalus;
    public Sprite spriteKursi;
    public Sprite spriteMeja;
    public Sprite spriteAlatMakan;
    public Sprite spriteTelenan;
    public Sprite spritePiring;
    public Sprite spriteUlekan;
    public Sprite spritePatungKayu;
    public Sprite spriteAlatMasak;
    public Sprite spriteWadahPerhiasan;


    void Awake() { instance = this; }

    void Update()
    {
        // Format Koin (Rp. xxx.xxx)
        if (teksKoin != null)
            teksKoin.text = $"Rp. {koin:#,0}";

        // Bensin dengan satuan L
        if (teksBensin != null)
            teksBensin.text = $"{bensin} L";

        // Listrik dengan satuan kWh
        if (teksListrik != null)
            teksListrik.text = $"{listrik} kWh";

        // XP Level (hanya angka)
        if (teksXpLevel != null)
            teksXpLevel.text = level.ToString();

        // XP Progress (xp / butuh) dan Progress Bar
        int butuh = XpButuhLevel();
        if (teksXpProgress != null)
            teksXpProgress.text = $"{xp} / {butuh}";

        if (progressBarXp != null && butuh > 0)
        {
            float fill = (float)xp / butuh;
            progressBarXp.value = Mathf.Clamp01(fill);
        }

        // Total Pekerja Aktif
        int totalPekerja = HitungTotalPekerja();
        if (teksTotalPekerja != null)
            teksTotalPekerja.text = totalPekerja.ToString();

        // Total Lahan
        int totalLahan = HitungTotalLahan();
        if (teksTotalLahan != null)
            teksTotalLahan.text = totalLahan.ToString();

        // Total Pabrik
        int totalPabrik = HitungTotalPabrik();
        if (teksTotalPabrik != null)
            teksTotalPabrik.text = totalPabrik.ToString();


        // Ambil durasi/jumlah XP yang dibutuhkan pada level saat ini
        int butuhXP = XpButuhLevel();

        // Pastikan teks XP progress terupdate
        if (teksXpProgress != null)
        {
            teksXpProgress.text = $"{xp} / {butuhXP}"; // Menyederhanakan string interpolation (IDE0071)
        }

        if (teksXpLevel != null)
        {
            teksXpLevel.text = level.ToString();
        }

        // Menggerakkan Slider secara Realtime
        if (progressBarXp != null)
        {
            progressBarXp.maxValue = butuhXP; // Batas maksimal slider mengikuti target level
            progressBarXp.value = xp;        // Nilai isi slider mengikuti XP saat ini
        }
    }

    int HitungTotalPekerja()
    {
        int aktif = 0;

        PekerjaPetaniPohonJati[] petani = FindObjectsByType<PekerjaPetaniPohonJati>();
        foreach (var p in petani) if (p.isAktif) aktif++;

        PekerjaMesin[] mesin = FindObjectsByType<PekerjaMesin>();
        foreach (var p in mesin) if (p.isAktif) aktif++;

        PekerjaMebel[] mebel = FindObjectsByType<PekerjaMebel>();
        foreach (var p in mebel) if (p.isAktif) aktif++;

        PekerjaPemahat[] pemahat = FindObjectsByType<PekerjaPemahat>();
        foreach (var p in pemahat) if (p.isAktif) aktif++;

        return aktif;
    }

    int HitungTotalLahan()
    {
        GameObject[] lahan = GameObject.FindGameObjectsWithTag("Lahan");
        if (lahan.Length > 0) return lahan.Length;
        if (LahanBuilder.instance != null)
            return LahanBuilder.instance.daftarPosisiAsli.Count;
        return 0;
    }

    int HitungTotalPabrik()
    {
        MesinPabrik[] mesin = FindObjectsByType<MesinPabrik>();
        MesinPemahat[] pemahat = FindObjectsByType<MesinPemahat>();
        return mesin.Length + pemahat.Length;
    }

    public int XpButuhLevel()
    {
        return 100 * (int)Mathf.Pow(2, level - 1);
    }

    public void TambahXP(int jumlah)
    {
        xp += jumlah;
        // Cek naik level
        while (xp >= XpButuhLevel() && level < 100) // batas level 100
        {
            xp -= XpButuhLevel();
            level++;
            Debug.Log($"Naik level! Sekarang level {level}");
            // Bisa tambahkan efek atau notifikasi
        }
        // Update UI jika diperlukan
    }



    public int AmbilStok(string namaItem)
    {
        if (namaItem == "Kayu Mentah") return kayuMentah;
        if (namaItem == "Kayu Gelondongan") return kayuGelondongan;
        if (namaItem == "Kursi") return kursi;
        if (namaItem == "Papan Halus") return papanHalus;
        if (namaItem == "Meja") return meja;
        if (namaItem == "Listrik") return listrik;

        if (namaItem == "Alat makan") return alatMakan;
        if (namaItem == "Telenan") return telenan;
        if (namaItem == "Piring") return piring;
        if (namaItem == "Ulekan") return ulekan;
        if (namaItem == "Patung Kayu") return patungKayu;
        if (namaItem == "Alat masak") return alatMasak;
        if (namaItem == "Wadah perhiasan") return wadahPerhiasan;
        return 0;
    }

    public void KurangiStok(string namaItem, int jumlah)
    {
        if (namaItem == "Kayu Mentah") kayuMentah -= jumlah;
        else if (namaItem == "Kayu Gelondongan") kayuGelondongan -= jumlah;
        else if (namaItem == "Kursi") kursi -= jumlah;
        else if (namaItem == "Meja") meja -= jumlah;
        else if (namaItem == "Listrik") listrik -= jumlah;
        else if (namaItem == "Papan Halus") papanHalus -= jumlah;

        else if (namaItem == "Alat makan") alatMakan -= jumlah;
        else if (namaItem == "Telenan") telenan -= jumlah;
        else if (namaItem == "Piring") piring -= jumlah;
        else if (namaItem == "Ulekan") ulekan -= jumlah;
        else if (namaItem == "Patung Kayu") patungKayu -= jumlah;
        else if (namaItem == "Alat masak") alatMasak -= jumlah;
        else if (namaItem == "Wadah perhiasan") wadahPerhiasan -= jumlah;
    }

    public bool KurangiKoin(int jumlah)
    {
        if (koin >= jumlah) { koin -= jumlah; return true; }
        return false;
    }

    public void TambahKoin(int jumlah) { koin += jumlah; }

    public int AmbilHargaJual(string namaItem)
    {
        return namaItem switch
        {
            "Kayu Mentah" => 10000,
            "Kayu Gelondongan" => 12000,
            "Papan Halus" => 8000,
            "Kursi" => 40000,
            "Meja" => 50000,
            "Alat makan" => 15000,
            "Telenan" => 30000,
            "Piring" => 25000,
            "Ulekan" => 55000,
            "Patung Kayu" => 100000,
            "Alat masak" => 25000,
            "Wadah perhiasan" => 50000,
            _ => 10 //'default'
        };
    }

    public Sprite AmbilSpriteBarang(string namaItem)
    {
        return namaItem switch
        {
            "Kayu Mentah" => spriteKayuMentah,
            "Kayu Gelondongan" => spriteKayuGelondongan,
            "Gelondongan" => spriteKayuGelondongan,
            "Papan Halus" => spritePapanHalus,
            "Kursi" => spriteKursi,
            "Meja" => spriteMeja,
            "Alat makan" => spriteAlatMakan,
            "Telenan" => spriteTelenan,
            "Piring" => spritePiring,
            "Ulekan" => spriteUlekan,
            "Patung Kayu" => spritePatungKayu,
            "Alat masak" => spriteAlatMasak,
            "Wadah perhiasan" => spriteWadahPerhiasan,
            _ => null
        };
    }
}