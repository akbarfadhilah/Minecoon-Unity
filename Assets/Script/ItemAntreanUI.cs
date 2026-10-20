using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ItemAntreanUI : MonoBehaviour
{
    public Image icon;
    public TextMeshProUGUI teksJumlah;
    public TextMeshProUGUI teksNama;

    public void SetData(Sprite sprite, int jumlah, string nama)
    {
        if (icon != null) icon.sprite = sprite;
        if (teksJumlah != null) teksJumlah.text = jumlah.ToString();
        if (teksNama != null) teksNama.text = nama;
    }
}