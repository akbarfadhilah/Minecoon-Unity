using UnityEngine;
using UnityEngine.UI;
using TMPro; // optional untuk teks halaman

public class HowToPlayManager : MonoBehaviour
{
    [Header("Panel & Display")]
    public GameObject panelHowToPlay;   // Panel utama yang berisi gambar
    public Image imageDisplay;          // Komponen Image untuk menampilkan sprite

    [Header("Gambar Panduan (Array)")]
    public Sprite[] sprites;            // Masukkan semua gambar panduan di sini, urut sesuai step

    [Header("Tombol Navigasi")]
    public Button buttonNext;           // Tombol Next
    public Button buttonPrev;           // Tombol Prev (opsional)
    public Button buttonClose;          // Tombol Close (opsional)
    public Button buttonOpen;           // Tombol untuk membuka panel (dari luar)

    [Header("Teks Halaman (Opsional)")]
    public TextMeshProUGUI teksHalaman; // contoh: "1 / 15"

    private int currentIndex = 0;

    void Start()
    {
        // Pastikan panel tertutup di awal
        if (panelHowToPlay != null)
            panelHowToPlay.SetActive(false);

        // Pasang listener tombol
        if (buttonNext != null)
            buttonNext.onClick.AddListener(OnNext);
        if (buttonPrev != null)
            buttonPrev.onClick.AddListener(OnPrev);
        if (buttonClose != null)
            buttonClose.onClick.AddListener(ClosePanel);
        if (buttonOpen != null)
            buttonOpen.onClick.AddListener(OpenPanel);
    }

    /// <summary>
    /// Buka panel, tampilkan gambar pertama
    /// </summary>
    public void OpenPanel()
    {
        if (panelHowToPlay == null || sprites == null || sprites.Length == 0)
        {
            Debug.LogWarning("Panel atau sprite belum di-assign!");
            return;
        }

        panelHowToPlay.SetActive(true);
        currentIndex = 0;
        UpdateDisplay();
    }

    /// <summary>
    /// Tutup panel
    /// </summary>
    public void ClosePanel()
    {
        if (panelHowToPlay != null)
            panelHowToPlay.SetActive(false);
    }

    /// <summary>
    /// Tombol Next: lanjut ke gambar berikutnya, jika sudah terakhir tutup panel
    /// </summary>
    void OnNext()
    {
        if (sprites == null || sprites.Length == 0) return;

        if (currentIndex < sprites.Length - 1)
        {
            currentIndex++;
            UpdateDisplay();
        }
        else
        {
            // Jika sudah di gambar terakhir, tutup panel
            ClosePanel();
        }
    }

    /// <summary>
    /// Tombol Prev: kembali ke gambar sebelumnya
    /// </summary>
    void OnPrev()
    {
        if (sprites == null || sprites.Length == 0) return;
        if (currentIndex > 0)
        {
            currentIndex--;
            UpdateDisplay();
        }
    }

    /// <summary>
    /// Update gambar dan teks halaman
    /// </summary>
    void UpdateDisplay()
    {
        if (imageDisplay != null && sprites != null && sprites.Length > 0)
        {
            imageDisplay.sprite = sprites[currentIndex];
        }

        // Update teks halaman jika ada (contoh: "1 / 15")
        if (teksHalaman != null)
        {
            teksHalaman.text = $"{currentIndex + 1} / {sprites.Length}";
        }
    }
}