using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class RuteItemUI : MonoBehaviour
{
    public Button tombolPilih;
    public TextMeshProUGUI teksNamaKota;
    public TextMeshProUGUI teksJarak;

    private Rute rute;
    private LogisticsManager manager;

    public void Inisialisasi(Rute data, LogisticsManager mgr)
    {
        rute = data;
        manager = mgr;
        teksNamaKota.text = data.namaKota;
        teksJarak.text = $"Jarak: {data.jarakTempuh}";
        tombolPilih.onClick.AddListener(() => manager.PilihRute(rute, tombolPilih));
    }
}