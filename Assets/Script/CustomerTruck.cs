using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.EventSystems;

public class CustomerTruck : MonoBehaviour
{
    public CustomerData customerData;
    public GameObject prefabNotifikasi;

    private CustomerSpawner spawner;
    private List<Transform> jalurTujuan;
    private Transform titikBerhenti;
    private List<CustomerSpawner.ItemPesanan> pesanan;
    private string namaCustomer;
    private int totalHarga;
    private int totalXP;
    private GameObject panelOrderPrefab;
    private Sprite fotoPelanggan;

    private GameObject panelAktif;
    private GameObject penandaAktif;
    private int currentTargetIndex = 1;
    private bool sudahSampaiStop = false;

    private enum State { Bergerak, MenungguTransaksi }
    private State state;

    [Header("Pengaturan Gerak")]
    public float kecepatanGerak = 5f;
    public float kecepatanRotasi = 360f; // derajat per detik (cukup cepat untuk belokan siku)

    public CustomerData CustomerData { get; private set; }
    public int TotalHarga { get; private set; }
    public int TotalXP { get; private set; }

    void Start()
    {
        if (GetComponent<Collider>() == null)
        {
            BoxCollider col = gameObject.AddComponent<BoxCollider>();
            col.size = new Vector3(2, 2, 3);
        }
    }

    public void Inisialisasi(CustomerSpawner spwn, List<Transform> jalur, Transform berhenti,
                         List<CustomerSpawner.ItemPesanan> order, string nama, int harga, int xp,
                         GameObject panelPrefab, Sprite foto, CustomerData data)
    {
        spawner = spwn;
        jalurTujuan = jalur;
        titikBerhenti = berhenti;
        pesanan = order;
        namaCustomer = nama;
        totalHarga = harga;
        totalXP = xp;
        panelOrderPrefab = panelPrefab;
        fotoPelanggan = foto;
        CustomerData = data;   // <-- simpan data pelanggan
        TotalHarga = harga;
        TotalXP = xp;

        if (jalurTujuan.Count > 0)
            transform.position = jalurTujuan[0].position;

        currentTargetIndex = 1;
        state = State.Bergerak;
        GerakKeTitikBerikutnya();
    }

    public void SetCustomerData(CustomerData data)
    {
        customerData = data;
    }

    void GerakKeTitikBerikutnya()
    {
        if (currentTargetIndex >= jalurTujuan.Count)
        {
            Destroy(gameObject);
            return;
        }

        Vector3 targetPos = jalurTujuan[currentTargetIndex].position;
        StartCoroutine(GerakKeTitik(targetPos, () => {
            if (!sudahSampaiStop && jalurTujuan[currentTargetIndex] == titikBerhenti)
            {
                sudahSampaiStop = true;
                state = State.MenungguTransaksi;
                MunculkanPenanda();
                Debug.Log($"Truck {namaCustomer} sampai di stop");
            }
            else
            {
                currentTargetIndex++;
                GerakKeTitikBerikutnya();
            }
        }));
    }

    IEnumerator GerakKeTitik(Vector3 target, System.Action onArrive)
    {
        float jarak = Vector3.Distance(transform.position, target);
        float durasi = jarak / kecepatanGerak;
        float t = 0;
        Vector3 startPos = transform.position;

        // Arah menuju target
        Vector3 arahTarget = (target - startPos).normalized;
        Quaternion targetRot = transform.rotation;
        if (arahTarget != Vector3.zero)
        {
            targetRot = Quaternion.LookRotation(arahTarget, Vector3.up) * Quaternion.Euler(0, 90, 0);
        }

        while (t < 1)
        {
            t += Time.deltaTime / durasi;
            // Posisi linear
            transform.position = Vector3.Lerp(startPos, target, t);
            // Rotasi independen, berputar secepat mungkin menuju arah target
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, kecepatanRotasi * Time.deltaTime);
            yield return null;
        }

        // Pastikan posisi dan rotasi tepat di akhir
        transform.position = target;
        transform.rotation = targetRot;
        onArrive?.Invoke();
    }

    void MunculkanPenanda()
    {
        GameObject penanda = new GameObject("Penanda");
        penanda.transform.SetParent(transform);
        penanda.transform.localPosition = new Vector3(0, 1.5f, 0);
        SpriteRenderer sr = penanda.AddComponent<SpriteRenderer>();
        sr.sprite = CreateSquareSprite();
        sr.color = Color.yellow;
        penandaAktif = penanda;
        StartCoroutine(BouncePenanda(penanda));
    }

    Sprite CreateSquareSprite()
    {
        Texture2D tex = new Texture2D(32, 32);
        Color[] colors = new Color[32 * 32];
        for (int i = 0; i < colors.Length; i++) colors[i] = Color.white;
        tex.SetPixels(colors);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f));
    }

    IEnumerator BouncePenanda(GameObject penanda)
    {
        Vector3 startPos = penanda.transform.localPosition;
        float waktu = 0;
        while (penanda != null && state == State.MenungguTransaksi)
        {
            waktu += Time.deltaTime * 3f;
            float offsetY = Mathf.Sin(waktu) * 0.1f;
            penanda.transform.localPosition = startPos + new Vector3(0, offsetY, 0);
            yield return null;
        }
    }

    void OnMouseDown()
    {
        if (EventSystem.current.IsPointerOverGameObject()) return;
        if (state != State.MenungguTransaksi) return;
        if (panelOrderPrefab == null || panelAktif != null) return;

        panelAktif = Instantiate(panelOrderPrefab);
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas != null)
        {
            panelAktif.transform.SetParent(canvas.transform, false);
            RectTransform rect = panelAktif.GetComponent<RectTransform>();
            rect.anchoredPosition = Vector2.zero;
        }

        CustomerOrderUI ui = panelAktif.GetComponent<CustomerOrderUI>();
        if (ui != null)
            ui.BukaPanel(this, namaCustomer, pesanan, totalHarga, fotoPelanggan, CustomerData);
    }

    public void Jual()
    {
        // Cek stok (sebenarnya sudah dicek di UI, tapi amankan)
        foreach (var item in pesanan)
            if (EconomyManager.instance.AmbilStok(item.namaItem) < item.jumlah) return;

        // Kurangi stok, tambah koin & XP
        foreach (var item in pesanan)
            EconomyManager.instance.KurangiStok(item.namaItem, item.jumlah);

        EconomyManager.instance.TambahKoin(totalHarga);
        EconomyManager.instance.TambahXP(totalXP);

        // Hapus penanda, tapi jangan tutup panel dulu
        HapusPenanda();
        // LanjutKeJalanLagi akan dipanggil dari CustomerOrderUI setelah delay
    }

    public void Batalkan()
    {
        HapusPenanda();
        // LanjutKeJalanLagi akan dipanggil dari CustomerOrderUI setelah delay
    }

    public void LanjutkanPerjalanan()
    {
        LanjutKeJalanLagi();
    }

    public void Tunggu()
    {
        TutupPanel();
    }

    void LanjutKeJalanLagi()
    {
        state = State.Bergerak;
        currentTargetIndex++;
        GerakKeTitikBerikutnya();
    }

    void TutupPanel()
    {
        if (panelAktif != null) Destroy(panelAktif);
        panelAktif = null;
    }

    void HapusPenanda()
    {
        if (penandaAktif != null) Destroy(penandaAktif);
    }
}