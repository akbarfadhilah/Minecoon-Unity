using UnityEngine;
using UnityEngine.EventSystems;

public class CoinClick : MonoBehaviour
{
    private int nilaiKoin;
    private GameObject objCoin;

    public void Init(int nilai, GameObject coinObj)
    {
        nilaiKoin = nilai;
        objCoin = coinObj;
    }

    void OnMouseDown()
    {
        if (EventSystem.current.IsPointerOverGameObject()) return;
        EconomyManager.instance.TambahKoin(nilaiKoin);
        Debug.Log($"+{nilaiKoin} koin dari penjualan!");
        Destroy(objCoin);
    }
}