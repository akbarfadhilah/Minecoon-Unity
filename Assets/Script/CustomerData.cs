using UnityEngine;

[System.Serializable]
public class CustomerData
{
    [Header("Informasi Pelanggan")]
    public string nama;
    public Sprite foto;           
    public GameObject prefabTruck;  

    [Header("Pesanan Random")]
    public int jumlahJenisBarangRandom = 2;  
    public int rangeMaksimalPerItem = 10;   

    [Header("Waktu")]
    public float waktuCooldown = 100f;

    [Header("Notifikasi")]
    public Sprite fotoTersedia;
    public Sprite fotoMenolak;
    public string teksTersedia = "Tersedia!";
    public string teksMenolak = "Menolak!";
    public string teksBarangKosong = "Stok Habis!";
    public string teksKedatangan = "Selamat Datang!";
}