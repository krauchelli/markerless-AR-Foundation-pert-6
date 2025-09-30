using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class ARRaycastPlaceObject : MonoBehaviour
{
    // The prefab for the object you want to place in the world.
    public GameObject objectToPlace;

    // The object that has been instantiated in the world.
    private GameObject spawnedObject;

    // A reference to the ARRaycastManager component.
    private ARRaycastManager raycastManager;

    // A list to store raycast hits.
    // Refinement: Changed from 'static' to a private instance list for better practice.
    private List<ARRaycastHit> hits = new List<ARRaycastHit>();

    void Start()
    {
        // Get the ARRaycastManager component when the script starts.
        raycastManager = GetComponent<ARRaycastManager>();
    }

    void Update()
    {
        // Check if there is any touch input on the screen.
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);

            // Check if the touch is in the "Began" phase (a new tap).
            if (touch.phase == TouchPhase.Began)
            {
                // Perform a raycast from the touch position against detected planes.
                if (raycastManager.Raycast(touch.position, hits, TrackableType.PlaneWithinPolygon))
                {
                    // If the raycast hits a plane, get the pose (position and rotation) of the hit.
                    Pose hitPose = hits[0].pose;

                    // Check if an object has been spawned yet.
                    if (spawnedObject == null)
                    {
                        // If this is the first time, create a new object.
                        spawnedObject = Instantiate(objectToPlace, hitPose.position, hitPose.rotation);
                    }
                    else
                    {
                        // If an object already exists, just move it to the new position.
                        spawnedObject.transform.position = hitPose.position;
                    }
                }
            }
        }
    }
}