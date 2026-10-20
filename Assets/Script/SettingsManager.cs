using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SettingsManager : MonoBehaviour
{
    public static SettingsManager instance;

    [Header("Referensi UI")]
    public Slider sliderBGM;
    public Slider sliderSFX;
    public TextMeshProUGUI teksVolumeBGM;
    public TextMeshProUGUI teksVolumeSFX;

    [Header("Referensi Manager")]
    public BGMManager bgmManager;
    public SFXManager sfxManager;

    private void Awake()
    {
        if (instance == null)
            instance = this;
        else
            Destroy(gameObject);
    }

    private void Start()
    {
        // Muat volume tersimpan, atau pakai default 0.5 (50%)
        float bgmVol = PlayerPrefs.GetFloat("BGMVolume", 0.5f);
        float sfxVol = PlayerPrefs.GetFloat("SFXVolume", 0.5f);

        // Set slider
        if (sliderBGM != null) sliderBGM.value = bgmVol;
        if (sliderSFX != null) sliderSFX.value = sfxVol;

        // Terapkan volume
        SetBGMVolume(bgmVol);
        SetSFXVolume(sfxVol);

        // Update teks
        UpdateTeks();

        // Pasang listener
        if (sliderBGM != null)
            sliderBGM.onValueChanged.AddListener(delegate { OnBGMChanged(); });
        if (sliderSFX != null)
            sliderSFX.onValueChanged.AddListener(delegate { OnSFXChanged(); });
    }

    public void OnBGMChanged()
    {
        float val = sliderBGM.value;
        SetBGMVolume(val);
        PlayerPrefs.SetFloat("BGMVolume", val);
        UpdateTeks();
    }

    public void OnSFXChanged()
    {
        float val = sliderSFX.value;
        SetSFXVolume(val);
        PlayerPrefs.SetFloat("SFXVolume", val);
        UpdateTeks();
    }

    void SetBGMVolume(float vol)
    {
        if (bgmManager != null)
            bgmManager.SetVolume(vol);
    }

    void SetSFXVolume(float vol)
    {
        if (sfxManager != null)
            sfxManager.SetVolume(vol);
    }

    void UpdateTeks()
    {
        if (teksVolumeBGM != null)
            teksVolumeBGM.text = $"BGM: {Mathf.RoundToInt(sliderBGM.value * 100)}%";
        if (teksVolumeSFX != null)
            teksVolumeSFX.text = $"SFX: {Mathf.RoundToInt(sliderSFX.value * 100)}%";
    }
}