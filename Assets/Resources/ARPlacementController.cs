using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

// This script controls the placement of an AR object based on screen taps.
public class ARPlacementController : MonoBehaviour
{
    // The 3D object prefab we want to place.
    // This will be assigned in the Unity Inspector.
    public GameObject objectToPlace;

    // The object that has been placed in the scene.
    private GameObject spawnedObject;

    // A reference to the ARRaycastManager component.
    private ARRaycastManager raycastManager;

    // A list to store the results of our raycast.
    private List<ARRaycastHit> hits = new List<ARRaycastHit>();

    void Start()
    {
        // Get the ARRaycastManager component attached to this same GameObject.
        raycastManager = GetComponent<ARRaycastManager>();
    }


    void Update()
    {
        // Cek apakah ada sentuhan di layar
        if (Input.touchCount > 0)
        {
            Debug.Log("LEVEL 1: Touch detected on screen. Total touches: " + Input.touchCount);

            Touch touch = Input.GetTouch(0);

            // Cek apakah ini adalah awal dari sentuhan (tap)
            if (touch.phase == TouchPhase.Began)
            {
                Debug.Log("LEVEL 2: Touch phase is 'Began'. Attempting to raycast...");

                // Lakukan Raycast untuk melihat apakah sentuhan mengenai plane
                if (raycastManager.Raycast(touch.position, hits, UnityEngine.XR.ARSubsystems.TrackableType.PlaneWithinPolygon))
                {
                    Debug.Log("LEVEL 3 - SUCCESS: Raycast hit a plane!");
                    
                    Pose hitPose = hits[0].pose;

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
                else
                {
                    Debug.Log("LEVEL 3 - FAILED: Raycast did NOT hit a plane.");
                }
            }
        }
    }
    // void Update()
    // {
    //     // Check if there is at least one touch on the screen.
    //     if (Input.touchCount > 0)
    //     {
    //         Touch touch = Input.GetTouch(0);

    //         // Check if the touch just began.
    //         if (touch.phase == TouchPhase.Began)
    //         {
    //             // A tap was registered! Let's log it.
    //             Debug.Log("Tap detected at position: " + touch.position);

    //             // Perform a raycast from the touch position against detected planes.
    //             if (raycastManager.Raycast(touch.position, hits, TrackableType.PlaneWithinPolygon))
    //             {
    //                 // The raycast hit a plane! Let's log it.
    //                 Debug.Log("Raycast successfully hit a plane.");

    //                 Pose hitPose = hits[0].pose;

    //                 if (spawnedObject == null)
    //                 {
    //                     spawnedObject = Instantiate(objectToPlace, hitPose.position, hitPose.rotation);
    //                 }
    //                 else
    //                 {
    //                     spawnedObject.transform.position = hitPose.position;
    //                     spawnedObject.transform.rotation = hitPose.rotation;
    //                 }
    //             }
    //             else
    //             {
    //                 // The raycast did NOT hit a plane. Let's log it.
    //                 Debug.Log("Raycast did not hit a detected plane.");
    //             }
    //         }
    //     }
    // }
}