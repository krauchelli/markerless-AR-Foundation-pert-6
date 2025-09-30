using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems; // Namespace untuk mendeteksi sentuhan pada UI
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class ARPlacementManager : MonoBehaviour
{
    public static ARPlacementManager Instance;

    // Enum untuk mendefinisikan mode aplikasi saat ini
    private enum AppMode { Mapping, Placing }
    private AppMode currentMode;

    [Header("Pengaturan UI")]
    public GameObject mappingUIPanel;         // Panel UI untuk mode mapping
    public GameObject placementUIPanel;       // Panel UI untuk mode penempatan
    public TextMeshProUGUI mappingStatusText; // Teks instruksi saat mapping
    public Slider rotationSlider;
    public Slider scaleSlider;

    [Header("Pengaturan Mapping")]
    public GameObject markerPrefab;           // Prefab untuk penanda sudut
    public LineRenderer areaOutline;          // Komponen untuk menggambar garis batas
    private List<GameObject> cornerMarkers = new List<GameObject>();

    [Header("Pengaturan Umpan Balik Visual")]
    public Material previewValidMaterial;
    public Material previewInvalidMaterial;
    public TextMeshProUGUI notificationText;
    public TextMeshProUGUI debugPositionText;

    // Variabel Internal
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
        
        // Atur kondisi awal aplikasi ke mode Mapping
        currentMode = AppMode.Mapping;
        mappingUIPanel.SetActive(true);
        placementUIPanel.SetActive(false);
        SetSlidersActive(false);

        if (areaOutline != null) areaOutline.gameObject.SetActive(true);
        UpdateMappingStatusText();

        if(notificationText != null) notificationText.text = "";
        if(debugPositionText != null) debugPositionText.text = "";
    }

    void Update()
    {
        // Panggil fungsi update yang sesuai dengan mode aplikasi
        if (currentMode == AppMode.Placing)
        {
            UpdatePlacingMode();
        }
    }

    // --- LOGIKA UTAMA UNTUK SETIAP MODE ---

    private void UpdatePlacingMode()
    {
        if (previewObject == null) return;

        Vector2 screenCenter = new Vector2(Screen.width / 2f, Screen.height / 2f);
        if (arRaycastManager.Raycast(screenCenter, hits, TrackableType.PlaneWithinPolygon))
        {
            Pose hitPose = hits[0].pose;
            previewObject.transform.position = hitPose.position;
            previewObject.transform.rotation = hitPose.rotation * Quaternion.Euler(0, currentYRotation, 0);

            if (debugPositionText != null) { debugPositionText.text = "Posisi: " + hitPose.position.ToString("F2"); }
            
            CheckPlacementValidity();
        }
    }

    // --- FUNGSI-FUNGSI YANG DIPANGGIL OLEH TOMBOL UI ---

    public void OnMarkPositionPressed()
    {
        // Cek agar tidak menandai jika jari sedang menyentuh tombol UI
        if (Input.touchCount > 0 && EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId)) return;
        
        if (cornerMarkers.Count >= 4)
        {
            ShowNotification("Area sudah memiliki 4 titik. Tekan 'Selesai' atau 'Reset'.", 3f);
            return;
        }

        Vector2 screenCenter = new Vector2(Screen.width / 2f, Screen.height / 2f);
        if (arRaycastManager.Raycast(screenCenter, hits, TrackableType.PlaneWithinPolygon))
        {
            Pose hitPose = hits[0].pose;
            GameObject newMarker = Instantiate(markerPrefab, hitPose.position, Quaternion.identity);
            cornerMarkers.Add(newMarker);

            UpdateAreaVisuals();
            UpdateMappingStatusText();
        }
    }

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

        // Sembunyikan semua elemen mapping
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

    // --- FUNGSI-FUNGSI LAIN (TETAP SAMA) ---
    #region Unchanged_Helper_Functions
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