using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem; // Namespace untuk Input System baru
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class ObjectPlacer : MonoBehaviour
{
    // Prefab objek yang akan diletakkan
    public GameObject objectToPlace;

    private ARRaycastManager arRaycastManager;
    private List<ARRaycastHit> hits = new List<ARRaycastHit>();

    void Start()
    {
        // Dapatkan komponen ARRaycastManager yang sudah ada di XR Origin (AR)
        arRaycastManager = GetComponent<ARRaycastManager>();
    }

    void Update()
    {
        // Cek apakah ada input sentuhan menggunakan Input System baru
        if (Touchscreen.current == null || !Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
        {
            return; // Jika tidak ada sentuhan, hentikan eksekusi
        }

        // Dapatkan posisi sentuhan
        Vector2 touchPosition = Touchscreen.current.primaryTouch.position.ReadValue();

        // Lakukan raycast dari posisi sentuhan
        if (arRaycastManager.Raycast(touchPosition, hits, TrackableType.PlaneWithinPolygon))
        {
            // Jika raycast mengenai permukaan, dapatkan posisinya
            Pose hitPose = hits[0].pose;

            // Buat instance baru dari objek di posisi tersebut
            Instantiate(objectToPlace, hitPose.position, hitPose.rotation);
        }
    }
}