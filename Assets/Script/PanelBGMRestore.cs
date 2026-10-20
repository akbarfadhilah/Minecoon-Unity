using UnityEngine;

public class PanelBGMRestore : MonoBehaviour
{
    void OnDisable()
    {
        if (BGMManager.instance != null && BGMManager.instance.bgmUtama != null)
            BGMManager.instance.ChangeBGM(BGMManager.instance.bgmUtama, 0.8f);
    }
}