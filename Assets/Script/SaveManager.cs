using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.IO;
using System.Collections.Generic;
using System.Linq;

public class SaveManager : MonoBehaviour
{
    public static SaveManager instance;

    [Header("Auto Save")]
    public float autoSaveInterval = 60f;
    private float autoSaveTimer = 0f;

    [Header("Prefab Referensi (untuk load)")]
    public GameObject prefabLahanAsli;
    public GameObject prefabMesinPotong;
    public GameObject prefabMesinMebel;
    public GameObject prefabMesinPemahat;

    [Header("UI Load Panel")]
    public GameObject panelLoadGame;
    public Transform kontenSaveList;
    public GameObject prefabTombolSaveItem; // prefab dengan 2 Text (nama profil & waktu) + 2 Button (Load & Delete)

    [Header("Panel Konfirmasi Hapus")]
    public GameObject panelConfirmDelete;
    public TextMeshProUGUI teksConfirmDelete;
    public Button tombolConfirmYa;
    public Button tombolConfirmTidak;

    [Header("Referensi UI")]
    public GameObject uiUtama;

    private string currentProfileName = "";
    private string selectedFileToDelete = ""; // untuk konfirmasi hapus

    private string saveFolder => Application.persistentDataPath + "/Saves/";

    void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);
        panelLoadGame.SetActive(false);
        panelConfirmDelete.SetActive(false);
    }

    void Start()
    {
        if (!Directory.Exists(saveFolder))
            Directory.CreateDirectory(saveFolder);

        // Tombol konfirmasi hapus
        tombolConfirmYa.onClick.AddListener(() => {
            if (!string.IsNullOrEmpty(selectedFileToDelete))
            {
                DeleteSave(selectedFileToDelete);
                panelConfirmDelete.SetActive(false);
                selectedFileToDelete = "";
                RefreshSaveList(); // refresh daftar
            }
        });
        tombolConfirmTidak.onClick.AddListener(() => {
            panelConfirmDelete.SetActive(false);
            selectedFileToDelete = "";
        });
    }

    void Update()
    {
        autoSaveTimer += Time.deltaTime;
        if (autoSaveTimer >= autoSaveInterval)
        {
            autoSaveTimer = 0;
            AutoSave();
        }
    }

    // ==================== SAVE ====================

    public void ManualSave()
    {
        string profileName = PlayerPrefs.GetString("PlayerName", "Guest");
        SaveGame(profileName);
        Debug.Log("Manual Save berhasil untuk profil: " + profileName);
    }

    void AutoSave()
    {
        string profileName = PlayerPrefs.GetString("PlayerName", "Guest");
        SaveGame(profileName);
        Debug.Log("Auto Save berhasil untuk profil: " + profileName);
    }

    void SaveGame(string profileName)
    {
        if (string.IsNullOrEmpty(profileName)) profileName = "Guest";

        SaveData data = new SaveData();
        data.profileName = profileName;
        data.timestamp = System.DateTime.Now.ToString("yyyy-MM-dd");

        // --- Profil ---
        data.playerName = PlayerPrefs.GetString("PlayerName", "Guest");
        data.photoIndex = PlayerPrefs.GetInt("PlayerPhotoIndex", 0);

        // --- Economy ---
        EconomyManager em = EconomyManager.instance;
        if (em != null)
        {
            data.koin = em.koin;
            data.listrik = em.listrik;
            data.bensin = em.bensin;
            data.xp = em.xp;
            data.level = em.level;
            data.kayuMentah = em.kayuMentah;
            data.kayuGelondongan = em.kayuGelondongan;
            data.papanHalus = em.papanHalus;
            data.kursi = em.kursi;
            data.meja = em.meja;
            data.alatMakan = em.alatMakan;
            data.telenan = em.telenan;
            data.piring = em.piring;
            data.ulekan = em.ulekan;
            data.patungKayu = em.patungKayu;
            data.alatMasak = em.alatMasak;
            data.wadahPerhiasan = em.wadahPerhiasan;
        }

        // --- Truck ---
        LogisticsManager lm = LogisticsManager.instance;
        if (lm != null) data.truckIndex = lm.indexTruckSaatIni;

        // --- Lahan ---
        if (LahanBuilder.instance != null)
        {
            data.lahanPositions = new List<SerializableVector3>();
            foreach (Vector3 pos in LahanBuilder.instance.daftarPosisiAsli)
                data.lahanPositions.Add(new SerializableVector3(pos));
        }

        // --- Mesin ---
        data.mesinData = new List<MesinData>();
        foreach (MesinPabrik m in FindObjectsOfType<MesinPabrik>())
        {
            MesinData md = new MesinData();
            md.jenis = m.jenisMesin.ToString();
            md.posisi = new SerializableVector3(m.transform.position);
            data.mesinData.Add(md);
        }
        foreach (MesinPemahat m in FindObjectsOfType<MesinPemahat>())
        {
            MesinData md = new MesinData();
            md.jenis = "Pemahat";
            md.posisi = new SerializableVector3(m.transform.position);
            data.mesinData.Add(md);
        }

        // --- Pohon ---
        data.pohonData = new List<PohonData>();
        foreach (PohonScript p in FindObjectsOfType<PohonScript>())
        {
            PohonData pd = new PohonData();
            pd.posisi = new SerializableVector3(p.transform.position);
            pd.waktuTumbuh = p.waktuTumbuh;
            pd.sisaWaktu = p.sisaWaktu;
            pd.siapPanen = p.siapPanen;
            data.pohonData.Add(pd);
        }

        string json = JsonUtility.ToJson(data, true);
        string path = saveFolder + profileName + ".json";
        File.WriteAllText(path, json);

        // Jika panel load terbuka, refresh daftar
        if (panelLoadGame.activeSelf)
            RefreshSaveList();
    }

    // ==================== LOAD ====================

    public void LoadGame(string profileName)
    {
        string path = saveFolder + profileName + ".json";
        if (!File.Exists(path))
        {
            Debug.LogWarning("Save file tidak ditemukan: " + profileName);
            return;
        }

        string json = File.ReadAllText(path);
        SaveData data = JsonUtility.FromJson<SaveData>(json);
        if (data == null)
        {
            Debug.LogError("Gagal membaca save file!");
            return;
        }

        // Hapus semua objek di dunia
        ClearWorldObjects();

        // --- Load Profil ---
        PlayerPrefs.SetString("PlayerName", data.playerName);
        PlayerPrefs.SetInt("PlayerPhotoIndex", data.photoIndex);
        PlayerPrefs.Save();
        if (ProfileManager.instance != null)
        {
            ProfileManager.instance.LoadProfile();
            ProfileManager.instance.UpdateHeader();
        }

        // --- Load Economy ---
        EconomyManager em = EconomyManager.instance;
        if (em != null)
        {
            em.koin = data.koin;
            em.listrik = data.listrik;
            em.bensin = data.bensin;
            em.xp = data.xp;
            em.level = data.level;
            em.kayuMentah = data.kayuMentah;
            em.kayuGelondongan = data.kayuGelondongan;
            em.papanHalus = data.papanHalus;
            em.kursi = data.kursi;
            em.meja = data.meja;
            em.alatMakan = data.alatMakan;
            em.telenan = data.telenan;
            em.piring = data.piring;
            em.ulekan = data.ulekan;
            em.patungKayu = data.patungKayu;
            em.alatMasak = data.alatMasak;
            em.wadahPerhiasan = data.wadahPerhiasan;
        }

        // --- Load Truck ---
        LogisticsManager lm = LogisticsManager.instance;
        if (lm != null)
        {
            lm.indexTruckSaatIni = data.truckIndex;
            lm.UpdateTampilanTruck();
            lm.SpawnTruckObject(lm.indexTruckSaatIni);
        }

        // --- Load Lahan ---
        if (LahanBuilder.instance != null && LahanBuilder.instance.prefabLahanAsli != null)
        {
            LahanBuilder lb = LahanBuilder.instance;
            for (int i = lb.objekLahanAsliTerpasang.Count - 1; i >= 0; i--)
            {
                if (lb.objekLahanAsliTerpasang[i] != null)
                    Destroy(lb.objekLahanAsliTerpasang[i]);
            }
            lb.objekLahanAsliTerpasang.Clear();
            lb.daftarPosisiAsli.Clear();

            foreach (SerializableVector3 pos in data.lahanPositions)
            {
                Vector3 v = pos.ToVector3();
                lb.daftarPosisiAsli.Add(v);
                GameObject lahan = Instantiate(lb.prefabLahanAsli, v, Quaternion.identity);
                lahan.layer = LayerMask.NameToLayer("BendaGrid");
                lb.objekLahanAsliTerpasang.Add(lahan);
            }
        }

        // --- Load Mesin ---
        foreach (MesinData md in data.mesinData)
        {
            Vector3 pos = md.posisi.ToVector3();
            GameObject prefab = null;
            if (md.jenis == "PotongKayu") prefab = prefabMesinPotong;
            else if (md.jenis == "Mebel") prefab = prefabMesinMebel;
            else if (md.jenis == "Pemahat") prefab = prefabMesinPemahat;
            if (prefab != null)
            {
                GameObject mesin = Instantiate(prefab, pos, Quaternion.identity);
                mesin.layer = LayerMask.NameToLayer("BendaGrid");
            }
        }

        // --- Load Pohon ---
        if (LahanBuilder.instance != null)
        {
            Dictionary<Vector3, GameObject> lahanDict = new Dictionary<Vector3, GameObject>();
            foreach (GameObject go in LahanBuilder.instance.objekLahanAsliTerpasang)
            {
                if (go != null)
                    lahanDict[go.transform.position] = go;
            }

            foreach (PohonData pd in data.pohonData)
            {
                Vector3 pos = pd.posisi.ToVector3();
                GameObject targetLahan = null;
                float minDist = 0.5f;
                foreach (var kvp in lahanDict)
                {
                    float dist = Vector3.Distance(kvp.Key, pos);
                    if (dist < minDist)
                    {
                        minDist = dist;
                        targetLahan = kvp.Value;
                    }
                }
                if (targetLahan != null)
                {
                    if (targetLahan.GetComponentInChildren<PohonScript>() != null)
                        continue;
                    GameObject pohonPrefab = Resources.Load<GameObject>("Pohon");
                    if (pohonPrefab != null)
                    {
                        GameObject pohon = Instantiate(pohonPrefab, pos, Quaternion.identity);
                        pohon.transform.SetParent(targetLahan.transform);
                        PohonScript ps = pohon.GetComponent<PohonScript>();
                        if (ps != null)
                        {
                            ps.waktuTumbuh = pd.waktuTumbuh;
                            ps.sisaWaktu = pd.sisaWaktu;
                            ps.siapPanen = pd.siapPanen;
                        }
                    }
                }
            }
        }

        panelLoadGame.SetActive(false);

        // Aktifkan UI utama jika ada
        if (uiUtama != null) uiUtama.SetActive(true);

        Debug.Log("Load selesai! Profil: " + profileName);
    }

    // ==================== UI LOAD PANEL ====================

    public void BukaPanelLoad()
    {
        panelLoadGame.SetActive(true);
        RefreshSaveList();
    }

    public void TutupPanelLoad()
    {
        panelLoadGame.SetActive(false);
    }

    public void RefreshSaveList()
    {
        foreach (Transform child in kontenSaveList) Destroy(child.gameObject);

        string[] files = Directory.GetFiles(saveFolder, "*.json");
        foreach (string file in files)
        {
            string fileName = Path.GetFileNameWithoutExtension(file);
            string fileContent = File.ReadAllText(file);
            SaveData data = JsonUtility.FromJson<SaveData>(fileContent);
            string displayName = (data != null && !string.IsNullOrEmpty(data.playerName)) ? data.playerName : fileName;
            string timestamp = (data != null) ? data.timestamp : File.GetLastWriteTime(file).ToString("dd-MM-yyyy HH:mm");

            GameObject item = Instantiate(prefabTombolSaveItem, kontenSaveList);
            TextMeshProUGUI[] teks = item.GetComponentsInChildren<TextMeshProUGUI>();
            if (teks.Length >= 1) teks[0].text = displayName;
            if (teks.Length >= 2) teks[1].text = timestamp;

            Button[] btns = item.GetComponentsInChildren<Button>();
            // Asumsi: tombol pertama = Load, tombol kedua = Delete
            if (btns.Length >= 1)
                btns[0].onClick.AddListener(() => LoadGame(fileName));
            if (btns.Length >= 2)
            {
                string name = fileName; // capture
                btns[1].onClick.AddListener(() => {
                    selectedFileToDelete = name;
                    teksConfirmDelete.text = $"Apakah anda yakin ingin menghapus {displayName}?";
                    panelConfirmDelete.SetActive(true);
                });
            }
        }
    }

    public void DeleteSave(string profileName)
    {
        string path = saveFolder + profileName + ".json";
        if (File.Exists(path))
        {
            File.Delete(path);
            Debug.Log("Save dihapus: " + profileName);
            RefreshSaveList();
        }
    }

    // ==================== HELPER ====================

    void ClearWorldObjects()
    {
        foreach (MesinPabrik m in FindObjectsOfType<MesinPabrik>()) Destroy(m.gameObject);
        foreach (MesinPemahat m in FindObjectsOfType<MesinPemahat>()) Destroy(m.gameObject);

        if (LahanBuilder.instance != null)
        {
            for (int i = LahanBuilder.instance.objekLahanAsliTerpasang.Count - 1; i >= 0; i--)
            {
                if (LahanBuilder.instance.objekLahanAsliTerpasang[i] != null)
                    Destroy(LahanBuilder.instance.objekLahanAsliTerpasang[i]);
            }
            LahanBuilder.instance.objekLahanAsliTerpasang.Clear();
            LahanBuilder.instance.daftarPosisiAsli.Clear();
        }
        foreach (PohonScript p in FindObjectsOfType<PohonScript>()) Destroy(p.gameObject);
    }
}

// Data classes (tidak berubah, hanya tambahkan profileName di SaveData)
[System.Serializable]
public class SaveData
{
    public string profileName; // nama file sebenarnya
    public string timestamp;
    public string playerName;
    public int photoIndex;
    public int koin, listrik, bensin, xp, level;
    public int kayuMentah, kayuGelondongan, papanHalus, kursi, meja;
    public int alatMakan, telenan, piring, ulekan, patungKayu, alatMasak, wadahPerhiasan;
    public int truckIndex;
    public List<SerializableVector3> lahanPositions;
    public List<MesinData> mesinData;
    public List<PohonData> pohonData;
}

[System.Serializable]
public class SerializableVector3
{
    public float x, y, z;
    public SerializableVector3(Vector3 v) { x = v.x; y = v.y; z = v.z; }
    public Vector3 ToVector3() => new Vector3(x, y, z);
}

[System.Serializable]
public class MesinData { public string jenis; public SerializableVector3 posisi; }

[System.Serializable]
public class PohonData { public SerializableVector3 posisi; public float waktuTumbuh; public float sisaWaktu; public bool siapPanen; }