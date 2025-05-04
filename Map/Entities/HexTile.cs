using UnityEngine;

public class HexTile : MonoBehaviour
{
    public int q;
    public int r;
    public Color color = Color.white;
    public float height = 1f;
    public float scale = 1f;
    public int altitude = 3;
    public string terrainType = "default";
    public string specialFunction = "";
    
    // Get properties for easier access
    public int Q => q;
    public int R => r;
    
    // Initialize with the tile data
    public void Initialize(HexTileData tileData)
    {
        if (tileData == null) return;
        
        q = tileData.q;
        r = tileData.r;
        color = tileData.color;
        height = tileData.height;
        altitude = tileData.altitude;
        terrainType = tileData.terrainType;
        specialFunction = tileData.specialFunction;
        scale = tileData.scale;
        
        ApplyVisualProperties();
    }
    
    // Get the current tile data
    public HexTileData GetTileData()
    {
        return HexTileData.FromHexTile(this);
    }
    
    private void ApplyVisualProperties()
    {
        // Apply color to the renderer
        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null)
        {
            Material material = new Material(renderer.sharedMaterial);
            material.color = color;
            renderer.material = material;
        }
        
        // Apply height and scale
        transform.localScale = new Vector3(
            scale, 
            height, 
            scale
        );
        
        // Update name to include coordinates and any special function
        if (!string.IsNullOrEmpty(specialFunction))
        {
            gameObject.name = $"Hex_{q}_{r}_{specialFunction}";
        }
        else
        {
            gameObject.name = $"Hex_{q}_{r}";
        }
    }
}