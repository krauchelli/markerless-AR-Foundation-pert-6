using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class ARPlacementManager : MonoBehaviour
{
    public static ARPlacementManager Instance;
    private AppMode currentMode;
    private enum AppMode { Mapping, Placing }

    [Header("Pengaturan UI")]
    public GameObject mappingUIPanel;
    public GameObject placementUIPanel;
    public TextMeshProUGUI mappingStatusText;
    public Slider rotationSlider;
    public Slider scaleSlider;

    [Header("Pengaturan Mapping")]
    public GameObject markerPrefab;
    public LineRenderer areaOutline;
    private List<GameObject> cornerMarkers = new List<GameObject>();

    // --- VARIABEL BARU UNTUK TRACKER 3D ---
    [Header("Pengaturan Tracker")]
    public GameObject liveTrackerPrefab;
    private GameObject liveTrackerInstance;
    // ------------------------------------

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
        
        currentMode = AppMode.Mapping;
        mappingUIPanel.SetActive(true);
        placementUIPanel.SetActive(false);
        SetSlidersActive(false);

        if (areaOutline != null) areaOutline.gameObject.SetActive(true);
        UpdateMappingStatusText();

        if(notificationText != null) notificationText.text = "";
        if(debugPositionText != null) debugPositionText.text = "";

        // --- BARIS BARU ---
        // Buat instance tracker di awal dan sembunyikan
        if (liveTrackerPrefab != null)
        {
            liveTrackerInstance = Instantiate(liveTrackerPrefab);
            liveTrackerInstance.SetActive(false);
        }
        // ------------------
    }

    void Update()
    {
        // Pindahkan logika raycast ke atas agar bisa digunakan oleh semua mode
        Vector2 screenCenter = new Vector2(Screen.width / 2f, Screen.height / 2f);
        bool didRaycastHitPlane = arRaycastManager.Raycast(screenCenter, hits, TrackableType.PlaneWithinPolygon);

        // --- LOGIKA BARU UNTUK TRACKER 3D ---
        if (liveTrackerInstance != null)
        {
            if (didRaycastHitPlane)
            {
                // Jika raycast kena, pindahkan tracker dan tampilkan
                Pose hitPose = hits[0].pose;
                liveTrackerInstance.transform.SetPositionAndRotation(hitPose.position, hitPose.rotation);
                liveTrackerInstance.SetActive(true);
            }
            else
            {
                // Jika tidak kena, sembunyikan tracker
                liveTrackerInstance.SetActive(false);
            }
        }
        // ------------------------------------

        // Panggil fungsi update yang sesuai dengan mode aplikasi
        if (currentMode == AppMode.Mapping)
        {
            UpdateMappingMode(didRaycastHitPlane);
        }
        else if (currentMode == AppMode.Placing)
        {
            UpdatePlacingMode(didRaycastHitPlane);
        }
    }

    // Modifikasi fungsi-fungsi update untuk menerima hasil raycast
    private void UpdateMappingMode(bool didRaycastHitPlane)
    {
        // ... (Fungsi ini tidak perlu diubah, tapi kita bisa memanfaatkannya nanti) ...
    }

    private void UpdatePlacingMode(bool didRaycastHitPlane)
    {
        if (previewObject == null) return;
        
        // Kita sudah punya hasil raycast dari Update(), jadi tinggal pakai
        if(didRaycastHitPlane)
        {
            Pose hitPose = hits[0].pose;
            previewObject.transform.position = hitPose.position;
            previewObject.transform.rotation = hitPose.rotation * Quaternion.Euler(0, currentYRotation, 0);

            if (debugPositionText != null) { debugPositionText.text = "Posisi: " + hitPose.position.ToString("F2"); }
            
            CheckPlacementValidity();
        }
    }
    
    // Modifikasi OnMarkPositionPressed untuk menggunakan tracker
    public void OnMarkPositionPressed()
    {
        if (Input.touchCount > 0 && EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId)) return;
        if (cornerMarkers.Count >= 4)
        {
            ShowNotification("Area sudah memiliki 4 titik. Tekan 'Selesai' atau 'Reset'.", 3f);
            return;
        }

        // Ambil posisi dari tracker yang sudah ada, bukan dari raycast baru
        if (liveTrackerInstance != null && liveTrackerInstance.activeInHierarchy)
        {
            GameObject newMarker = Instantiate(markerPrefab, liveTrackerInstance.transform.position, Quaternion.identity);
            cornerMarkers.Add(newMarker);

            UpdateAreaVisuals();
            UpdateMappingStatusText();
        }
    }

    // ... (Sisa fungsi lainnya tetap sama) ...
    #region Unchanged_Functions
    public void FinishMapping()
    {
        if (cornerMarkers.Count < 3)
        {
            ShowNotification("Tandai minimal 3 titik untuk membentuk sebuah bidang.", 3f);
            return;
        }

        currentMode = AppMode.Placing;
        mappingUIPanel.SetActive(false);
        placementUIPanel.SetActive(true);

        foreach (var marker in cornerMarkers) { marker.SetActive(false); }
        if (areaOutline != null) areaOutline.gameObject.SetActive(false);

        ShowNotification("Mode Penempatan Aktif!", 3f);
    }
    public void ResetMapping()
    {
        foreach (var marker in cornerMarkers) { Destroy(marker); }
        cornerMarkers.Clear();

        if (areaOutline != null) areaOutline.positionCount = 0;
        UpdateMappingStatusText();
        ShowNotification("Mapping direset. Silakan tandai ulang area.", 3f);
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
    public void StartPreview(GameObject prefabToPreview)
    {
        if (previewObject != null) { Destroy(previewObject); }
        previewObject = Instantiate(prefabToPreview);
        
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
    public void OnRotationSliderChanged(float value) { currentYRotation = value; }
    public void OnScaleSliderChanged(float value)
    {
        if(previewObject != null) { previewObject.transform.localScale = Vector3.one * initialScale * value; }
    }
    private void ResetAndShowSliders()
    {
        rotationSlider.value = 0;
        scaleSlider.value = 1;
        currentYRotation = 0;
        SetSlidersActive(true);
    }
    private void SetSlidersActive(bool isActive)
    {
        if(rotationSlider != null) rotationSlider.gameObject.SetActive(isActive);
        if(scaleSlider != null) scaleSlider.gameObject.SetActive(isActive);
    }
    private void CheckPlacementValidity()
    {
        BoxCollider previewCollider = previewObject.GetComponent<BoxCollider>();
        if (previewCollider == null) return;
        
        previewCollider.enabled = false;
        Vector3 center = previewObject.transform.TransformPoint(previewCollider.center);
        Vector3 halfExtents = Vector3.Scale(previewObject.transform.lossyScale, previewCollider.size) / 2f;
        isPlacementValid = !Physics.CheckBox(center, halfExtents, previewObject.transform.rotation);
        previewCollider.enabled = true;

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
    private void UpdateAreaVisuals()
    {
        if (areaOutline == null || cornerMarkers.Count < 2) return;

        areaOutline.positionCount = cornerMarkers.Count + 1;
        for (int i = 0; i < cornerMarkers.Count; i++)
        {
            areaOutline.SetPosition(i, cornerMarkers[i].transform.position);
        }
        areaOutline.SetPosition(cornerMarkers.Count, cornerMarkers[0].transform.position);
    }
    private void UpdateMappingStatusText()
    {
        if (mappingStatusText == null) return;
        switch (cornerMarkers.Count)
        {
            case 0: mappingStatusText.text = "Arahkan ke lantai & tekan 'Tandai Posisi' untuk titik pertama."; break;
            case 1: mappingStatusText.text = "Tandai titik kedua untuk membuat garis."; break;
            case 2: mappingStatusText.text = "Tandai titik ketiga untuk membuat bidang."; break;
            case 3: mappingStatusText.text = "Tandai titik keempat untuk menyelesaikan area."; break;
            case 4: mappingStatusText.text = "Area selesai. Tekan 'Selesai Mapping' untuk melanjutkan."; break;
        }
    }
    #endregion
}