using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class ProfileManager : MonoBehaviour
{
    public static ProfileManager instance;

    [Header("Header UI")]
    public Image headerFoto;
    public TextMeshProUGUI headerNama;

    [Header("Panel Edit Profile")]
    public GameObject panelEditProfile;
    public Transform kontenFoto;
    public GameObject prefabTombolFoto;
    public TMP_InputField inputNama;
    public Button tombolSave;
    public Button tombolCancel;

    [Header("Data Foto Profil")]
    public Sprite[] daftarFoto;

    private int selectedFotoIndex = 0;
    private string namaPemain = "Player";

    // Warna: normal (100%) dan highlight (50% transparan)
    private Color normalColor = new Color(1f, 1f, 1f, 1f);
    private Color highlightColor = new Color(1f, 1f, 1f, 0.5f);
    private List<Image> listTombolImages = new List<Image>();

    void Awake()
    {
        instance = this;
        panelEditProfile.SetActive(false);
    }

    void Start()
    {
        LoadProfile();
        UpdateHeader();
        SetupPanelEdit();
        tombolSave.onClick.AddListener(SaveProfile);
        tombolCancel.onClick.AddListener(CancelEdit);
    }

    public void LoadProfile()
    {
        namaPemain = PlayerPrefs.GetString("PlayerName", "Player");
        selectedFotoIndex = PlayerPrefs.GetInt("PlayerPhotoIndex", 0);
        if (daftarFoto.Length == 0)
        {
            Debug.LogWarning("Daftar foto profil kosong!");
            return;
        }
        if (selectedFotoIndex >= daftarFoto.Length)
            selectedFotoIndex = 0;
    }

    public void SaveProfile()
    {
        string nama = inputNama.text.Trim();
        if (string.IsNullOrEmpty(nama)) nama = "Player";
        namaPemain = nama;

        PlayerPrefs.SetString("PlayerName", namaPemain);
        PlayerPrefs.SetInt("PlayerPhotoIndex", selectedFotoIndex);
        PlayerPrefs.Save();

        UpdateHeader();      // ← header foto langsung berubah
        panelEditProfile.SetActive(false);
        Debug.Log("Profil disimpan!");
    }

    public void CancelEdit()
    {
        LoadProfile();
        RefreshPanelEdit();
        UpdateHeader();      // ← kembalikan header ke data tersimpan
        panelEditProfile.SetActive(false);
        Debug.Log("Edit dibatalkan.");
    }

    public void UpdateHeader()
    {
        if (headerNama != null)
        {
            string displayName = namaPemain;
            if (displayName.Length > 6)
                displayName = displayName.Substring(0, 6) + "..";
            headerNama.text = displayName;
        }
        if (headerFoto != null && daftarFoto.Length > 0 && selectedFotoIndex < daftarFoto.Length)
        {
            headerFoto.sprite = daftarFoto[selectedFotoIndex];
        }
    }

    void SetupPanelEdit()
    {
        if (daftarFoto.Length == 0) return;
        foreach (Transform child in kontenFoto) Destroy(child.gameObject);
        listTombolImages.Clear();

        for (int i = 0; i < daftarFoto.Length; i++)
        {
            GameObject btnObj = Instantiate(prefabTombolFoto, kontenFoto);
            Image img = btnObj.GetComponent<Image>();
            if (img != null && daftarFoto[i] != null)
                img.sprite = daftarFoto[i];
            listTombolImages.Add(img);

            int index = i;
            Button btn = btnObj.GetComponent<Button>();
            btn.onClick.AddListener(() => SelectFoto(index));
        }

        inputNama.text = namaPemain;
        RefreshPanelEdit();
    }

    void RefreshPanelEdit()
    {
        // Yang dipilih → normal (100%), yang lain → highlight (50%)
        for (int i = 0; i < listTombolImages.Count; i++)
        {
            if (listTombolImages[i] != null)
                listTombolImages[i].color = (i == selectedFotoIndex) ? normalColor : highlightColor;
        }
    }

    void SelectFoto(int index)
    {
        if (index < 0 || index >= daftarFoto.Length) return;
        selectedFotoIndex = index;
        RefreshPanelEdit();
        // Header tidak langsung berubah sampai tombol Save ditekan
        // (agar pemain bisa lihat preview? Opsional)
        // Jika ingin preview sementara, aktifkan baris di bawah:
        // UpdateHeader();
    }

    public void BukaEditProfile()
    {
        LoadProfile();
        inputNama.text = namaPemain;
        RefreshPanelEdit();
        panelEditProfile.SetActive(true);
    }
}