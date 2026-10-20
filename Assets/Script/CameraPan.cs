using UnityEngine;
using UnityEngine.EventSystems;

public class CameraPan : MonoBehaviour
{
    [Header("Batas Pergerakan Kamera")]
    public float minX = 2f;
    public float maxX = 10f;
    public float minZ = -10f;
    public float maxZ = -2f;

    [Header("Pengaturan Sensitivitas")]
    public float sensitivitas = 0.5f;

    [Header("Tombol Mouse")]
    [Tooltip("0 = Klik Kiri, 1 = Klik Kanan, 2 = Klik Tengah")]
    public int tombolMouse = 0;

    private bool sedangDrag = false;

    void Update()
    {
        // 1. Deteksi awal klik mouse
        if (Input.GetMouseButtonDown(tombolMouse))
        {
            // PENGAMAN: Jika mouse mengklik UI (Tombol/Panel), batalkan drag kamera!
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }

            sedangDrag = true;
        }

        // 2. Deteksi ketika klik mouse dilepas
        if (Input.GetMouseButtonUp(tombolMouse))
        {
            sedangDrag = false;
        }

        // 3. Proses menggeser kamera saat mouse ditahan & digerakkan
        if (sedangDrag && Input.GetMouseButton(tombolMouse))
        {
            // Ambil pergerakan mouse pada frame ini
            // Dikali minus (-) agar efeknya seperti "menarik/menggeser tanah" (Natural Drag)
            float geserX = -Input.GetAxis("Mouse X") * sensitivitas;
            float geserZ = -Input.GetAxis("Mouse Y") * sensitivitas;

            // Hitung posisi target baru kamera (Hanya mengubah posisi X dan Z)
            Vector3 posisiBaru = transform.position + new Vector3(geserX, 0, geserZ);

            // BATASAN (Clamp) sesuai dengan permintaan kamu:
            posisiBaru.x = Mathf.Clamp(posisiBaru.x, minX, maxX);
            posisiBaru.z = Mathf.Clamp(posisiBaru.z, minZ, maxZ);

            // Terapkan posisi baru ke kamera
            transform.position = posisiBaru;
        }
    }
}