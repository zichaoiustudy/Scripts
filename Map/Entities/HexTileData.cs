using UnityEngine;

/// <summary>
/// Serializable data representation of a hexagonal tile.
/// Used for saving/loading map data and transferring tile information.
/// </summary>
[System.Serializable]
public class HexTileData
{
    public int q;
    public int r;
    public Color color = Color.white;
    public float height = 1f;
    public int altitude = 3;
    public string terrainType = "default";
    public string specialFunction = "";
    public float scale = 1f;
    
    public HexTileData(int q, int r)
    {
        this.q = q;
        this.r = r;
    }

    public HexTileData(int q, int r, Color color, float height, int altitude, string terrainType = "default")
    {
        this.q = q;
        this.r = r;
        this.color = color;
        this.height = height;
        this.altitude = altitude;
        this.terrainType = terrainType;
    }
    
    /// <summary>
    /// Creates a HexTileData from a HexTile MonoBehaviour
    /// </summary>
    public static HexTileData FromHexTile(HexTile hexTile)
    {
        HexTileData data = new HexTileData(hexTile.q, hexTile.r);
        data.color = hexTile.color;
        data.height = hexTile.height;
        data.altitude = hexTile.altitude;
        data.terrainType = hexTile.terrainType;
        data.specialFunction = hexTile.specialFunction;
        data.scale = hexTile.scale;
        return data;
    }
}