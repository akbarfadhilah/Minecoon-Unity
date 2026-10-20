using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class SupirItemUI : MonoBehaviour
{
    public Button tombolPilih;
    public TextMeshProUGUI teksNamaSupir;
    public TextMeshProUGUI teksUpah;

    private SupirData supir;
    private LogisticsManager manager;

    public void Inisialisasi(SupirData data, LogisticsManager mgr)
    {
        supir = data;
        manager = mgr;
        teksNamaSupir.text = data.namaSupir;
        teksUpah.text = $"Upah: {data.upah.ToString("#,0").Replace(",", ".")}";
        tombolPilih.onClick.AddListener(() => manager.PilihSupir(supir, tombolPilih));
    }

    public void SetTextCooldown(string text)
    {
        teksUpah.text = text;
    }

    public void SetInteractable(bool interactable)
    {
        tombolPilih.interactable = interactable;
    }

    public SupirData GetSupirData()
    {
        return supir;
    }
}