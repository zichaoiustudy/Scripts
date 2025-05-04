using UnityEngine;

public class FieldManager : MonoBehaviour
{
    [SerializeField] private GameObject hexPrefab; // Prefab of the hex tile

    private MapData mapData; // Reference to the map data asset
    private int fieldSize; // Radius of the hexagonal field

    private GameObject[,] hexGrid; // 2D array to store hex tiles
    
    // Generate the hexagonal field
    public void GenerateField()
    {
        // Get configuration from GameConfig through ServiceLocator
        if (ServiceLocator.Instance?.GameManager != null)
        {
            GameConfig gameConfig = ServiceLocator.Instance.GameManager.GetGameConfig();
            fieldSize = gameConfig != null ? gameConfig.fieldRadius : 6;
            mapData = gameConfig != null ? gameConfig.mapData : null;
        }
        
        if (mapData == null)
        {
            Debug.LogError("MapData is not set in GameConfig!");
            return;
        }
        
        // Initialize the grid array
        int diameter = fieldSize * 2 + 1;
        hexGrid = new GameObject[diameter, diameter];
        
        float hexWidth = 1.0f; // Adjust based on your hex prefab size
        float hexHeight = 0.866f; // Height of a hex is approximately 0.866 * width
        
        // Generate the hexagonal grid
        for (int q = -fieldSize; q <= fieldSize; q++)
        {
            int r1 = Mathf.Max(-fieldSize, -q - fieldSize);
            int r2 = Mathf.Min(fieldSize, -q + fieldSize);
            
            for (int r = r1; r <= r2; r++)
            {
                // Convert axial coordinates to world position
                float x = hexWidth * (1.5f * q);
                float z = hexHeight * (2f * r + q);
                
                // Create hex at position
                Vector3 position = new Vector3(x, 0, z);
                GameObject hexObj = Instantiate(hexPrefab, position, hexPrefab.transform.rotation, transform);
                hexObj.name = $"Hex_{q}_{r}";
                
                // Apply appearance from map data if available
                ApplyHexAppearance(hexObj, q, r);
                
                // Store in grid (convert to array indices)
                int arrayQ = q + fieldSize;
                int arrayR = r + fieldSize;
                hexGrid[arrayQ, arrayR] = hexObj;
            }
        }

        Debug.Log($"Field generation complete. Created {transform.childCount} hex tiles.");
    }
    
    // Apply appearance to a hex tile based on map data
    private void ApplyHexAppearance(GameObject hexObj, int q, int r)
    {
        // Add or get HexTile component
        HexTile hexTile = hexObj.GetComponent<HexTile>();
        if (hexTile == null)
        {
            hexTile = hexObj.AddComponent<HexTile>();
        }
        
        // Always set the correct coordinates, regardless of mapData
        hexTile.q = q;
        hexTile.r = r;
        
        if (mapData == null)
            return;
            
        HexTileData tileData = mapData.GetHexTileData(q, r);
        if (tileData == null)
            return;
            
        // Only apply the appearance/properties from tileData,
        // but keep the coordinates we've already set
        hexTile.color = tileData.color;
        hexTile.height = tileData.height;
        hexTile.altitude = tileData.altitude;
        hexTile.terrainType = tileData.terrainType;
        hexTile.specialFunction = tileData.specialFunction;
        hexTile.scale = tileData.scale;
        
        // Apply visual properties from the tile data
        
        // Apply color to the renderer
        Renderer renderer = hexObj.GetComponent<Renderer>();
        if (renderer != null)
        {
            Material material = new Material(renderer.sharedMaterial);
            material.color = tileData.color;
            renderer.material = material;
        }
        
        // Apply height and scale
        hexObj.transform.localScale = new Vector3(
            tileData.scale, 
            tileData.height, 
            tileData.scale
        );
        
    }
    
    // Get a specific hex by axial coordinates
    public GameObject GetHex(int q, int r)
    {
        int arrayQ = q + fieldSize;
        int arrayR = r + fieldSize;
        
        if (arrayQ >= 0 && arrayQ < hexGrid.GetLength(0) && 
            arrayR >= 0 && arrayR < hexGrid.GetLength(1))
        {
            return hexGrid[arrayQ, arrayR];
        }
        
        return null;
    }

    public int GetHexAltitude(int q, int r, int defaultValue = 3)
    {
        GameObject hexObj = GetHex(q, r);
        if (hexObj != null)
        {
            HexTile hexTile = hexObj.GetComponent<HexTile>();
            if (hexTile != null)
            {
                return hexTile.altitude;
            }
        }
        return defaultValue;
    }
    
    // Update all hex appearances based on current map data
    public void UpdateAllHexAppearances()
    {   
        if (mapData == null || hexGrid == null)
            return;
            
        for (int q = -fieldSize; q <= fieldSize; q++)
        {
            int r1 = Mathf.Max(-fieldSize, -q - fieldSize);
            int r2 = Mathf.Min(fieldSize, -q + fieldSize);
            
            for (int r = r1; r <= r2; r++)
            {
                GameObject hexObj = GetHex(q, r);
                if (hexObj != null)
                {
                    ApplyHexAppearance(hexObj, q, r);
                }
            }
        }
    }
}
