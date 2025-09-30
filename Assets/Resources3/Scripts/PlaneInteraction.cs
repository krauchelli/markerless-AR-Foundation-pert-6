using UnityEngine;

public class PlaneInteraction : MonoBehaviour
{
    // Fungsi ini otomatis dipanggil oleh Unity ketika
    // objek dengan Collider ini disentuh atau di-klik.
    void OnMouseDown()
    {
        Debug.Log("Plane ditekan! Posisi: " + transform.position);

        // Di masa depan, di sinilah kita akan memanggil
        // fungsi untuk menempatkan furnitur.
    }
}