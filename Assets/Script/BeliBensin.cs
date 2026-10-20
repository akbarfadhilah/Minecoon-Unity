using UnityEngine;
using TMPro;

public class BeliBensin : MonoBehaviour
{
    [Header("Harga dan Jumlah")]
    public int hargaCoin = 30;
    public int jumlahBensin = 50;

    [Header("Opsional: Teks Notifikasi")]
    public TextMeshProUGUI teksNotifikasi;

    [Header("SFX")]
    public AudioClip suaraBerhasil;
    public AudioClip suaraGagal;

    public void Beli()
    {
        if (EconomyManager.instance == null)
        {
            Debug.LogError("EconomyManager tidak ditemukan!");
            return;
        }

        if (EconomyManager.instance.koin >= hargaCoin)
        {
            // Kurangi koin, tambah bensin
            EconomyManager.instance.koin -= hargaCoin;
            EconomyManager.instance.bensin += jumlahBensin;

            string pesan = $"Berhasil membeli {jumlahBensin} Bensin!";
            Debug.Log(pesan);
            if (teksNotifikasi != null) teksNotifikasi.text = pesan;

            // Mainkan suara berhasil
            if (SFXManager.instance != null && suaraBerhasil != null)
                SFXManager.instance.PlaySound(suaraBerhasil);
        }
        else
        {
            string pesan = $"Koin tidak cukup! Butuh {hargaCoin} coin.";
            Debug.Log(pesan);
            if (teksNotifikasi != null) teksNotifikasi.text = pesan;

            // Mainkan suara gagal
            if (SFXManager.instance != null && suaraGagal != null)
                SFXManager.instance.PlaySound(suaraGagal);
        }
    }
}