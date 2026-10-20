using UnityEngine;
using UnityEngine.UI;
using UnityEngine.VFX;

[RequireComponent(typeof(Button))]
public class ButtonSFX : MonoBehaviour
{
    public AudioClip suaraKlik;
    private Button btn;

    void Start()
    {
        btn = GetComponent<Button>();
        btn.onClick.AddListener(() => {
            if (SFXManager.instance != null)
                SFXManager.instance.PlaySound(suaraKlik);
        });
    }
}