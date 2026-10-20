using UnityEngine;
using System.Collections;

public class PohonScript : MonoBehaviour
{
    [Header("Waktu Tumbuh")]
    public float waktuTumbuh = 10f;

    [Header("Prefab")]
    public GameObject prefabBibit;
    public GameObject prefabPohonMatang;

    [Header("Efek Transisi (Opsional)")]
    public GameObject efekTransisiPrefab;

    [Header("Status")]
    public bool siapPanen = false;
    public float sisaWaktu; // <-- TAMBAHKAN INI

    private GameObject currentStage;

    void Start()
    {
        gameObject.layer = LayerMask.NameToLayer("BendaGrid");
        sisaWaktu = waktuTumbuh;

        if (prefabBibit != null)
        {
            currentStage = Instantiate(prefabBibit, transform);
            currentStage.transform.localPosition = Vector3.zero;
            currentStage.transform.localScale = Vector3.one * 0.75f;
            StartCoroutine(AnimateScale(currentStage, 0.75f, 1.30f, waktuTumbuh / 2f));
        }

        StartCoroutine(Tumbuh());
    }

    IEnumerator Tumbuh()
    {
        float paruhWaktu = waktuTumbuh / 2f;

        // Fase bibit tumbuh
        float elapsed = 0;
        while (elapsed < paruhWaktu)
        {
            elapsed += Time.deltaTime;
            sisaWaktu = waktuTumbuh - elapsed;
            yield return null;
        }

        // Efek transisi
        if (efekTransisiPrefab != null)
        {
            GameObject efek = Instantiate(efekTransisiPrefab, transform.position, Quaternion.identity);
            Destroy(efek, 1f);
        }

        if (currentStage != null)
            Destroy(currentStage);

        if (prefabPohonMatang != null)
        {
            currentStage = Instantiate(prefabPohonMatang, transform);
            currentStage.transform.localPosition = Vector3.zero;
            currentStage.transform.localScale = Vector3.one * 0.5f;
            StartCoroutine(AnimateScale(currentStage, 0.5f, 1f, paruhWaktu));
        }

        // Fase pohon matang tumbuh
        elapsed = 0;
        while (elapsed < paruhWaktu)
        {
            elapsed += Time.deltaTime;
            sisaWaktu = paruhWaktu - elapsed;
            yield return null;
        }

        siapPanen = true;
        sisaWaktu = 0f;
    }

    IEnumerator AnimateScale(GameObject obj, float startScale, float endScale, float duration)
    {
        float elapsed = 0;
        while (elapsed < duration)
        {
            if (obj == null) yield break;
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float scale = Mathf.Lerp(startScale, endScale, t);
            if (obj != null)
                obj.transform.localScale = Vector3.one * scale;
            yield return null;
        }
        if (obj != null)
            obj.transform.localScale = Vector3.one * endScale;
    }
}