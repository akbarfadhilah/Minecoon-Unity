using UnityEngine;
using UnityEngine.EventSystems;

public class TokoClick : MonoBehaviour
{
    [Header("Referensi Panel UI")]
    public GameObject panelToko; // Drag panel toko dari Canvas

    [Header("BGM")]
    public AudioClip bgmSupermarket; // suara saat di toko

    private void OnMouseDown()
    {
        if (EventSystem.current.IsPointerOverGameObject()) return;
        if (panelToko == null)
        {
            Debug.LogWarning("Panel Toko belum di-assign di Inspector!");
            return;
        }

        bool akanAktif = !panelToko.activeSelf; // panel akan menjadi aktif atau tidak?

        if (akanAktif)
        {
            // Membuka panel: ganti BGM ke supermarket
            if (BGMManager.instance != null && bgmSupermarket != null)
                BGMManager.instance.ChangeBGM(bgmSupermarket, 0.8f);
        }
        else
        {
            // Menutup panel: kembali ke BGM utama (alam)
            if (BGMManager.instance != null && BGMManager.instance.bgmUtama != null)
                BGMManager.instance.ChangeBGM(BGMManager.instance.bgmUtama, 0.8f);
        }

        panelToko.SetActive(akanAktif);
    }
}