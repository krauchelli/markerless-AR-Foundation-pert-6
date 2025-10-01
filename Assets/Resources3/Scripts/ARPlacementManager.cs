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
    private enum AppMode { Mapping, Placing }
    private AppMode currentMode;

    [Header("Pengaturan UI")]
    public GameObject mappingUIPanel;
    public GameObject placementUIPanel;
    public Slider panjangSlider;
    public TextMeshProUGUI panjangValueText;
    public Slider lebarSlider;
    public TextMeshProUGUI lebarValueText;
    public Button kunciPosisiButton;
    public Slider rotationSlider;
    public Slider scaleSlider;

    [Header("Pengaturan Mapping")]
    public GameObject virtualFloorPrefab;
    private GameObject mappingAreaPreview;

    [Header("Pengaturan Tracker")]
    public GameObject liveTrackerPrefab;
    private GameObject liveTrackerInstance;

    [Header("Pengaturan Umpan Balik Visual")]
    public Material previewValidMaterial;
    public Material previewInvalidMaterial;
    public TextMeshProUGUI notificationText;
    
    // Variabel Internal
    private ARRaycastManager arRaycastManager;
    private List<ARRaycastHit> hits = new List<ARRaycastHit>();
    private GameObject previewObject;
    private bool isPlacementValid;
    private float initialScale;
    private float currentYRotation;
    private List<GameObject> placedObjects = new List<GameObject>();
    private GameObject definedAreaObject;
    private BoxCollider definedAreaCollider;

    void Start()
    {
        arRaycastManager = GetComponent<ARRaycastManager>();
        
        currentMode = AppMode.Mapping;
        mappingUIPanel.SetActive(true);
        placementUIPanel.SetActive(false);
        SetSlidersActive(false);

        if (notificationText != null) notificationText.text = "Atur ukuran bidang menggunakan slider.";
        if (liveTrackerPrefab != null)
        {
            liveTrackerInstance = Instantiate(liveTrackerPrefab);
            liveTrackerInstance.SetActive(false);
        }
        
        CreateAreaPreview();
    }

    void Update()
    {
        Vector2 screenCenter = new Vector2(Screen.width / 2f, Screen.height / 2f);
        bool didRaycastHitPlane = arRaycastManager.Raycast(screenCenter, hits, TrackableType.PlaneWithinPolygon);
        
        if (didRaycastHitPlane)
        {
            Pose hitPose = hits[0].pose;
            if (liveTrackerInstance != null)
            {
                liveTrackerInstance.transform.SetPositionAndRotation(hitPose.position, hitPose.rotation);
                liveTrackerInstance.SetActive(previewObject == null && mappingAreaPreview == null);
            }
            if (mappingAreaPreview != null)
            {
                mappingAreaPreview.transform.position = hitPose.position;
            }
        }
        else
        {
            if (liveTrackerInstance != null) liveTrackerInstance.SetActive(false);
        }
        
        if (currentMode == AppMode.Placing)
        {
            UpdatePlacingMode(didRaycastHitPlane);
        }
    }
    
    private void CreateAreaPreview()
    {
        if (mappingAreaPreview != null) Destroy(mappingAreaPreview);
        mappingAreaPreview = Instantiate(virtualFloorPrefab);
        
        OnPanjangSliderChanged(panjangSlider.value);
        OnLebarSliderChanged(lebarSlider.value);
        
        if (kunciPosisiButton != null) kunciPosisiButton.gameObject.SetActive(true);
    }

    public void OnPanjangSliderChanged(float value)
    {
        if (mappingAreaPreview != null)
        {
            float lebar = mappingAreaPreview.transform.localScale.x;
            mappingAreaPreview.transform.localScale = new Vector3(lebar, 1, value);
        }
        if (panjangValueText != null)
        {
            panjangValueText.text = "Panjang: " + value.ToString("F1") + " m";
        }
    }

    public void OnLebarSliderChanged(float value)
    {
        if (mappingAreaPreview != null)
        {
            float panjang = mappingAreaPreview.transform.localScale.z;
            mappingAreaPreview.transform.localScale = new Vector3(value, 1, panjang);
        }
        if (lebarValueText != null)
        {
            lebarValueText.text = "Lebar: " + value.ToString("F1") + " m";
        }
    }

    public void OnLockAreaPosition()
    {
        if (mappingAreaPreview == null) return;
        
        definedAreaObject = mappingAreaPreview;
        definedAreaCollider = definedAreaObject.GetComponent<BoxCollider>();
        mappingAreaPreview = null;

        currentMode = AppMode.Placing;
        mappingUIPanel.SetActive(false);
        placementUIPanel.SetActive(true);
        
        ShowNotification("Mode Penempatan Aktif!", 3f);
    }

    public void ResetMapping()
    {
        if (mappingAreaPreview != null) Destroy(mappingAreaPreview);
        if (definedAreaObject != null) Destroy(definedAreaObject);
        
        definedAreaCollider = null;
        
        if (panjangSlider != null) panjangSlider.value = 3;
        if (lebarSlider != null) lebarSlider.value = 3;
        CreateAreaPreview();
        
        ShowNotification("Mapping direset.", 3f);
    }
    
    private void UpdatePlacingMode(bool didRaycastHitPlane)
    {
        if (previewObject == null) return;
        if(didRaycastHitPlane)
        {
            Pose hitPose = hits[0].pose;
            previewObject.transform.position = hitPose.position;
            previewObject.transform.rotation = hitPose.rotation * Quaternion.Euler(0, currentYRotation, 0);
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
            ShowNotification("Tidak bisa dikunci, area terisi atau di luar batas!", 3f);
        }
    }

    private void CheckPlacementValidity()
    {
        BoxCollider previewCollider = previewObject.GetComponent<BoxCollider>();
        if (previewCollider == null) return;
        
        int furnitureLayerMask = 1 << LayerMask.NameToLayer("Furniture");
        Vector3 center = previewObject.transform.TransformPoint(previewCollider.center);
        Vector3 halfExtents = Vector3.Scale(previewObject.transform.lossyScale, previewCollider.size) / 2f;
        bool furnitureCollision = Physics.CheckBox(center, halfExtents, previewObject.transform.rotation, furnitureLayerMask, QueryTriggerInteraction.Ignore);

        bool isInBounds = false;
        if (!furnitureCollision && definedAreaCollider != null)
        {
            Vector3 checkCenter = previewObject.transform.position + (Vector3.up * 0.05f);
            Vector3 checkHalfExtents = new Vector3(0.05f, 0.05f, 0.05f);
            Collider[] overlappingColliders = Physics.OverlapBox(checkCenter, checkHalfExtents, Quaternion.identity);
            foreach (var col in overlappingColliders)
            {
                if (col == definedAreaCollider) { isInBounds = true; break; }
            }
        }
        
        isPlacementValid = !furnitureCollision && isInBounds;
        ApplyValidityMaterial();
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
    
    private void ApplyValidityMaterial()
    {
        Material materialToApply = isPlacementValid ? previewValidMaterial : previewInvalidMaterial;
        Renderer[] renderers = previewObject.GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in renderers) { renderer.material = materialToApply; }
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