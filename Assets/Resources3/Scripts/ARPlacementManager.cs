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

    [Header("Pengaturan Umpan Balik Visual")]
    public Material previewValidMaterial;
    public Material previewInvalidMaterial;
    public TextMeshProUGUI notificationText;
    public TextMeshProUGUI debugPositionText;
    
    [Header("Pengaturan UI")]
    public Slider rotationSlider;
    public Slider scaleSlider;

    private ARRaycastManager arRaycastManager;
    private List<ARRaycastHit> hits = new List<ARRaycastHit>();
    private GameObject previewObject;
    private bool isPlacementValid;
    private float initialScale;
    private float currentYRotation;

    // --- BARIS BARU ---
    // List untuk melacak semua objek yang sudah berhasil dikunci
    private List<GameObject> placedObjects = new List<GameObject>();
    // ------------------

    // ... (Fungsi Awake, Start, Update, StartPreview, OnRotationSliderChanged, OnScaleSliderChanged, dll. tetap SAMA) ...
    #region Unchanged_Methods
    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); }
        else { Instance = this; }
    }

    void Start()
    {
        arRaycastManager = GetComponent<ARRaycastManager>();
        if(notificationText != null) notificationText.text = "";
        if(debugPositionText != null) debugPositionText.text = "";
        SetSlidersActive(false);
    }

    void Update()
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

    public void StartPreview(GameObject prefabToPreview)
    {
        if (previewObject != null) { Destroy(previewObject); }
        previewObject = Instantiate(prefabToPreview);
        
        FurnitureData data = prefabToPreview.GetComponent<FurnitureData>();
        initialScale = (data != null) ? data.desiredScale : 1.0f;
        
        previewObject.transform.localScale = Vector3.one * initialScale;
        
        ResetAndShowSliders();
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
    #endregion
    // --- AKHIR BAGIAN YANG TIDAK BERUBAH ---


    // --- FUNGSI LockPlacement YANG DIMODIFIKASI ---
    public void LockPlacement()
    {
        if (previewObject == null) return;
        
        if (isPlacementValid)
        {
            ShowNotification(previewObject.name + " berhasil dikunci!", 2.5f);
            
            // --- BARIS BARU ---
            // Tambahkan objek ke daftar objek yang sudah ditempatkan
            placedObjects.Add(previewObject);
            // ------------------
            
            previewObject.GetComponent<BoxCollider>().enabled = true;
            previewObject = null;
            SetSlidersActive(false);
        }
        else
        {
            ShowNotification("Tidak bisa dikunci, area sudah terisi!", 2.5f);
        }
    }

    // --- FUNGSI BARU UNTUK MENGHAPUS SEMUA ---
    public void ClearAllPlacedObjects()
    {
        // Menangani edge case: hapus juga objek yang sedang di-preview
        if (previewObject != null)
        {
            Destroy(previewObject);
            previewObject = null;
            SetSlidersActive(false);
        }

        // Loop melalui semua objek yang sudah dikunci dan hancurkan
        foreach (GameObject obj in placedObjects)
        {
            Destroy(obj);
        }

        // Kosongkan list setelah semua objek dihancurkan
        placedObjects.Clear();

        ShowNotification("Semua furnitur telah dibersihkan.", 2.5f);
        Debug.Log("Semua objek yang ditempatkan telah dihapus.");
    }
}