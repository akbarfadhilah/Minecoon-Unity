using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ItemPesananUI : MonoBehaviour
{
    public Image iconBarang;
    public TextMeshProUGUI teksDetailPesanan;

    public void SetData(Sprite sprite, string detail)
    {
        if (iconBarang != null && sprite != null)
            iconBarang.sprite = sprite;
        if (teksDetailPesanan != null)
            teksDetailPesanan.text = detail;
    }

    public void SetWarnaTeks(Color warna)
    {
        if (teksDetailPesanan != null)
            teksDetailPesanan.color = warna;
    }
}