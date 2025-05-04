using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewMapData", menuName = "Hex Chess/Map Data", order = 1)]
public class MapData : ScriptableObject
{
    [SerializeField]
    private List<HexTileData> hexTiles = new List<HexTileData>();
    
    private Dictionary<string, HexTileData> tileDataMap;

    // Initialize the dictionary for faster lookups
    private void OnEnable()
    {
        BuildLookupDictionary();
    }

    private void BuildLookupDictionary()
    {
        tileDataMap = new Dictionary<string, HexTileData>();
        foreach (var tile in hexTiles)
        {
            string key = $"{tile.q}_{tile.r}";
            tileDataMap[key] = tile;
        }
    }

    // Add a new hex tile data
    public void AddHexTile(HexTileData tileData)
    {
        string key = $"{tileData.q}_{tileData.r}";
        
        if (tileDataMap == null)
            BuildLookupDictionary();
            
        // Check if we already have a tile at these coordinates
        if (tileDataMap.TryGetValue(key, out HexTileData existingTile))
        {
            // Remove the existing tile
            hexTiles.Remove(existingTile);
        }
        
        // Add the new tile
        hexTiles.Add(tileData);
        tileDataMap[key] = tileData;
    }

    // Get hex tile data by coordinates
    public HexTileData GetHexTileData(int q, int r)
    {
        if (tileDataMap == null)
            BuildLookupDictionary();
            
        string key = $"{q}_{r}";
        if (tileDataMap.TryGetValue(key, out HexTileData data))
            return data;
            
        return null;
    }

    public void RemoveHexTile(int q, int r)
    {
        if (tileDataMap == null)
            BuildLookupDictionary();
            
        string key = $"{q}_{r}";
        if (tileDataMap.TryGetValue(key, out HexTileData data))
        {
            hexTiles.Remove(data);
            tileDataMap.Remove(key);
        }
    }

    // Clear all hex tile data
    public void ClearHexTiles()
    {
        hexTiles.Clear();
        tileDataMap?.Clear();
    }

    // Get all hex tile data
    public List<HexTileData> GetAllHexTiles()
    {
        return hexTiles;
    }
}