using UnityEngine;

public class ExitGame : MonoBehaviour
{
    // Fungsi ini akan dipanggil oleh tombol
    public void QuitGame()
    {
#if UNITY_EDITOR
        // Jika di dalam Editor Unity, hentikan Play Mode
        UnityEditor.EditorApplication.isPlaying = false;
#else
            // Jika di build final (exe, apk, dll), keluar dari aplikasi
            Application.Quit();
#endif
    }
}