using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class ARPlacementManager : MonoBehaviour
{
    public static ARPlacementManager Instance;

    [Header("Pengaturan UI")]
    public Slider rotationSlider;
    public Slider scaleSlider;

    [Header("Pengaturan Tracker")]
    public GameObject liveTrackerPrefab;
    private GameObject liveTrackerInstance;

    [Header("Pengaturan Umpan Balik Visual")]
    public Material previewValidMaterial;
    public Material previewInvalidMaterial;
    public TextMeshProUGUI notificationText;
    public TextMeshProUGUI debugPositionText;

    private ARRaycastManager arRaycastManager;
    private List<ARRaycastHit> hits = new List<ARRaycastHit>();
    private GameObject previewObject;
    private bool isPlacementValid;
    private float initialScale;
    private float currentYRotation;
    private List<GameObject> placedObjects = new List<GameObject>();

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); }
        else { Instance = this; }
    }

    void Start()
    {
        arRaycastManager = GetComponent<ARRaycastManager>();
        if (notificationText != null) notificationText.text = "";
        if (debugPositionText != null) debugPositionText.text = "";
        SetSlidersActive(false);

        if (liveTrackerPrefab != null)
        {
            liveTrackerInstance = Instantiate(liveTrackerPrefab);
            liveTrackerInstance.SetActive(false);
        }
    }

    void Update()
    {
        Vector2 screenCenter = new Vector2(Screen.width / 2f, Screen.height / 2f);
        bool didRaycastHitPlane = arRaycastManager.Raycast(screenCenter, hits, TrackableType.PlaneWithinPolygon);

        if (liveTrackerInstance != null)
        {
            if (didRaycastHitPlane && previewObject == null)
            {
                liveTrackerInstance.transform.SetPositionAndRotation(hits[0].pose.position, hits[0].pose.rotation);
                liveTrackerInstance.SetActive(true);
            }
            else
            {
                liveTrackerInstance.SetActive(false);
            }
        }

        if (previewObject == null) return;

        if (didRaycastHitPlane)
        {
            Pose hitPose = hits[0].pose;
            previewObject.transform.position = hitPose.position;
            previewObject.transform.rotation = hitPose.rotation * Quaternion.Euler(0, currentYRotation, 0);

            if (debugPositionText != null) { debugPositionText.text = "Posisi: " + hitPose.position.ToString("F2"); }

            CheckPlacementValidity();
        }
        else
        {
            isPlacementValid = false;
            ApplyValidityMaterial();
        }
    }

    public void StartPreview(GameObject prefabToPreview)
    {
        if (previewObject != null) { Destroy(previewObject); }
        previewObject = Instantiate(prefabToPreview);
        
        SetLayerRecursively(previewObject, LayerMask.NameToLayer("PreviewObject"));

        FurnitureData data = prefabToPreview.GetComponent<FurnitureData>();
        initialScale = (data != null) ? data.desiredScale : 1.0f;
        
        previewObject.transform.localScale = Vector3.one * initialScale;
        
        ResetAndShowSliders();
    }

    public void LockPlacement()
    {
        if (previewObject == null) return;
        
        if (isPlacementValid)
        {
            SetLayerRecursively(previewObject, LayerMask.NameToLayer("Furniture"));

            ShowNotification(previewObject.name + " berhasil dikunci!", 2.5f);
            placedObjects.Add(previewObject);
            previewObject.GetComponent<BoxCollider>().enabled = true;
            previewObject = null;
            SetSlidersActive(false);
        }
        else
        {
            ShowNotification("Tidak bisa dikunci, area sudah terisi!", 2.5f);
        }
    }

    private void CheckPlacementValidity()
    {
        BoxCollider previewCollider = previewObject.GetComponent<BoxCollider>();
        if (previewCollider == null) return;
        
        // Buat LayerMask yang HANYA berisi layer "Furniture".
        int layerMask = 1 << LayerMask.NameToLayer("Furniture");

        Vector3 center = previewObject.transform.TransformPoint(previewCollider.center);
        Vector3 halfExtents = Vector3.Scale(previewObject.transform.lossyScale, previewCollider.size) / 2f;
        
        // CheckBox sekarang hanya akan mencari tabrakan dengan objek lain di layer "Furniture"
        bool isColliding = Physics.CheckBox(center, halfExtents, previewObject.transform.rotation, layerMask, QueryTriggerInteraction.Ignore);

        isPlacementValid = !isColliding;
        
        ApplyValidityMaterial();
    }

    private void ApplyValidityMaterial()
    {
        Material materialToApply = isPlacementValid ? previewValidMaterial : previewInvalidMaterial;
        Renderer[] renderers = previewObject.GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in renderers) { renderer.material = materialToApply; }
    }

    public void ClearAllPlacedObjects()
    {
        if (previewObject != null)
        {
            Destroy(previewObject);
            previewObject = null;
            SetSlidersActive(false);
        }

        foreach (GameObject obj in placedObjects) { Destroy(obj); }
        placedObjects.Clear();
        
        ShowNotification("Semua furnitur telah dibersihkan.", 2.5f);
        Debug.Log("Semua objek yang ditempatkan telah dihapus.");
    }
    
    public void OnRotationSliderChanged(float value) { currentYRotation = value; }

    public void OnScaleSliderChanged(float value)
    {
        if(previewObject != null) { previewObject.transform.localScale = Vector3.one * initialScale * value; }
    }
    
    private void ResetAndShowSliders()
    {
        if(rotationSlider != null) rotationSlider.value = 0;
        if(scaleSlider != null) scaleSlider.value = 1;
        currentYRotation = 0;
        SetSlidersActive(true);
    }

    private void SetSlidersActive(bool isActive)
    {
        if(rotationSlider != null) rotationSlider.gameObject.SetActive(isActive);
        if(scaleSlider != null) scaleSlider.gameObject.SetActive(isActive);
    }

    private void ShowNotification(string message, float duration)
    {
        if(notificationText == null) return;
        StopAllCoroutines();
        StartCoroutine(NotificationCoroutine(message, duration));
    }

    private IEnumerator NotificationCoroutine(string message, float duration)
    {
        notificationText.text = message;
        yield return new WaitForSeconds(duration);
        notificationText.text = "";
    }
    
    private void SetLayerRecursively(GameObject obj, int newLayer)
    {
        if (obj == null) return;
        obj.layer = newLayer;
        foreach (Transform child in obj.transform)
        {
            if (child == null) continue;
            SetLayerRecursively(child.gameObject, newLayer);
        }
    }
}