using UnityEngine;
using TMPro;
using UnityEngine.EventSystems;

public class MesinScript : MonoBehaviour
{
    public float waktuProses = 5f; // Jeda 5 detik
    private float sisaWaktu;
    private bool sedangMemproses = false;

    public TextMeshProUGUI teksStatus; // Untuk tulisan di atas mesin

    void Start()
    {
        if (teksStatus != null) teksStatus.text = "SIAP";
    }

    void Update()
    {
        if (sedangMemproses)
        {
            sisaWaktu -= Time.deltaTime;
            teksStatus.text = "MEMOTONG: " + sisaWaktu.ToString("F1") + "s";

            if (sisaWaktu <= 0)
            {
                SelesaiProses();
            }
        }
    }

    void OnMouseDown()
    {
        if (EventSystem.current.IsPointerOverGameObject()) return;
        // Syarat: Tidak lagi memproses DAN punya kayu mentah
        if (!sedangMemproses && EconomyManager.instance.kayuMentah > 0)
        {
            MulaiProses();
        }
        else if (EconomyManager.instance.kayuMentah <= 0)
        {
            Debug.Log("Kayu mentah habis!");
        }
    }

    void MulaiProses()
    {
        sedangMemproses = true;
        sisaWaktu = waktuProses;

        // Ambil 1 kayu mentah dari gudang
        EconomyManager.instance.kayuMentah -= 1;
        Debug.Log("Proses dimulai...");
    }

    void SelesaiProses()
    {
        sedangMemproses = false;

        // Tambah 2 kayu gelondongan (Sesuai permintaanmu)
        EconomyManager.instance.kayuGelondongan += 2;

        teksStatus.text = "SELESAI!";
        Debug.Log("Dapat 2 Gelondongan!");

        // Setelah 1 detik, balik lagi ke tulisan SIAP
        Invoke("ResetTeks", 1f);
    }

    void ResetTeks()
    {
        if (!sedangMemproses) teksStatus.text = "SIAP";
    }
}