using UnityEngine;
using TMPro;

public class BeliListrik : MonoBehaviour
{
    [Header("Harga dan Jumlah")]
    public int hargaCoin = 50;
    public int jumlahListrik = 100;

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
            EconomyManager.instance.koin -= hargaCoin;
            EconomyManager.instance.listrik += jumlahListrik;

            string pesan = $"Berhasil membeli {jumlahListrik} Listrik!";
            Debug.Log(pesan);
            if (teksNotifikasi != null) teksNotifikasi.text = pesan;

            if (SFXManager.instance != null && suaraBerhasil != null)
                SFXManager.instance.PlaySound(suaraBerhasil);
        }
        else
        {
            string pesan = $"Koin tidak cukup! Butuh {hargaCoin} coin.";
            Debug.Log(pesan);
            if (teksNotifikasi != null) teksNotifikasi.text = pesan;

            if (SFXManager.instance != null && suaraGagal != null)
                SFXManager.instance.PlaySound(suaraGagal);
        }
    }
}