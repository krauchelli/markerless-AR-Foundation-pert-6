using UnityEngine;
using System.Collections.Generic;

public class FurnitureManager : MonoBehaviour
{
    public static FurnitureManager Instance;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); }
        else { Instance = this; }
    }

    public List<GameObject> furniturePrefabs;

    // Kita tidak perlu lagi menyimpan 'currentlySelectedPrefab' di sini.
    // Tugas itu sekarang sepenuhnya milik ARPlacementManager.

    public void OnFurnitureSelected(int furnitureIndex)
    {
        if (furnitureIndex >= 0 && furnitureIndex < furniturePrefabs.Count)
        {
            // Langsung perintahkan ARPlacementManager untuk memulai preview
            // dengan prefab yang sesuai.
            ARPlacementManager.Instance.StartPreview(furniturePrefabs[furnitureIndex]);
        }
        else
        {
            Debug.LogWarning("Index furnitur tidak valid: " + furnitureIndex);
        }
    }
}