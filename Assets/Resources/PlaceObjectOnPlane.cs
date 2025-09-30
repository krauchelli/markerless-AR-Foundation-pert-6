using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

[RequireComponent(typeof(ARRaycastManager))] 
public class PlaceObjectOnPlane : MonoBehaviour
{
    [Tooltip("Prefab yang akan ditempatkan saat pengguna mengetuk layar.")]
    public GameObject objectToPlace;

    private ARRaycastManager raycastManager;
    private GameObject spawnedObject;
    private static List<ARRaycastHit> hits = new List<ARRaycastHit>();

    void Awake()
    {
        raycastManager = GetComponent<ARRaycastManager>();
    }

    void Update()
    {
        // Cek input dari sentuhan atau klik mouse
        if (!TryGetTouchPosition(out Vector2 touchPosition))
            return;

        // Proses hanya saat input baru dimulai
        if (Input.GetMouseButtonDown(0) || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began))
        {
            // Tembakkan raycast dari posisi input
            if (raycastManager.Raycast(touchPosition, hits, TrackableType.PlaneWithinPolygon))
            {
                var hitPose = hits[0].pose;
                Debug.Log($"Raycast berhasil mengenai bidang di posisi: {hitPose.position}");

                if (spawnedObject == null)
                {
                    spawnedObject = Instantiate(objectToPlace, hitPose.position, hitPose.rotation);
                }
                else
                {
                    spawnedObject.transform.position = hitPose.position;
                    spawnedObject.transform.rotation = hitPose.rotation;
                }
            }
        }
    }

    // Fungsi bantuan untuk mendapatkan posisi input
    bool TryGetTouchPosition(out Vector2 touchPosition)
    {
        if (Input.touchCount > 0)
        {
            touchPosition = Input.GetTouch(0).position;
            return true;
        }
        if (Input.GetMouseButton(0))
        {
            touchPosition = Input.mousePosition;
            return true;
        }
        touchPosition = default;
        return false;
    }
}