using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

#if UNITY_EDITOR
public static class MapGenerator
{
    [MenuItem("HexChess/Generate Default Map")]
    public static void GenerateDefaultMap()
    {
        // Create a new MapData asset
        MapData mapData = ScriptableObject.CreateInstance<MapData>();
        
        // Define map size
        int mapSize = 6;
        
        // Generate a terrain map with custom altitude
        GenerateAltitudeTerrainMap(mapData, mapSize);
        
        // Save the asset
        string path = EditorUtility.SaveFilePanelInProject(
            "Save Map Data",
            "DefaultMap",
            "asset",
            "Save the generated map data"
        );
        
        if (string.IsNullOrEmpty(path))
            return;
            
        AssetDatabase.CreateAsset(mapData, path);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        Debug.Log($"Map data generated and saved to {path}");
    }
    
    public static void GenerateAltitudeTerrainMap(MapData mapData, int mapSize)
    {
        // Clear any existing data
        mapData.ClearHexTiles();
        
        // Create a dictionary to store assigned hex tiles
        Dictionary<(int, int), bool> assignedTiles = new Dictionary<(int, int), bool>();
        
        // Define altitude colors - from lowest (1) to highest (5)
        Color[] altitudeColors = new Color[6] {
            Color.black, // Not used (index 0)
            new Color(0.1f, 0.5f, 0.3f), // Altitude 1: Deep blue (water)
            new Color(0.2f, 0.7f, 0.3f),  // Altitude 2: Light green (lowland)
            new Color(0.6f, 0.9f, 0.5f),  // Altitude 3: Green (plain)
            new Color(0.9f, 0.9f, 0.7f),  // Altitude 4: Light yellow (hill)
            new Color(0.9f, 0.9f, 0.9f)   // Altitude 5: White/gray (mountains)
            
        };
        
        // Define altitude heights
        float[] altitudeHeights = new float[6] {
            0f,   // Not used (index 0)
            0.2f, // Altitude 1: Lowest
            0.6f, // Altitude 2: Low
            1.0f, // Altitude 3: Medium
            1.4f, // Altitude 4: High
            1.8f  // Altitude 5: Highest
        };
        
        // Define terrain types based on altitude
        string[] altitudeTerrainTypes = new string[6] {
            "",          // Not used (index 0)
            "Water",     // Altitude 1
            "Lowland",   // Altitude 2
            "Plain",     // Altitude 3
            "Hill",      // Altitude 4
            "Mountain"   // Altitude 5
        };
        
        // Generate altitude 5 terrain (highest)
        List<Vector2Int> altitude5Coords = new List<Vector2Int> {
            new Vector2Int(-2, -3), new Vector2Int(1, -3), new Vector2Int(-3, 5), 
            new Vector2Int(-3, 2), new Vector2Int(5, -2), new Vector2Int(2, 1)
        };
        GenerateTerrainForCoordinates(mapData, assignedTiles, altitude5Coords, 5, 
            altitudeColors[5], altitudeHeights[5], altitudeTerrainTypes[5]);
        
        // Generate altitude 4 terrain
        List<Vector2Int> altitude4Coords = new List<Vector2Int> {
            new Vector2Int(-2, -4), new Vector2Int(2, -6), new Vector2Int(1, -5), 
            new Vector2Int(2, -5), new Vector2Int(-1, -3), new Vector2Int(0, -3), 
            new Vector2Int(0, -2), new Vector2Int(-6, 4), new Vector2Int(-5, 4), 
            new Vector2Int(-5, 3), new Vector2Int(-4, 6), new Vector2Int(-3, 4), 
            new Vector2Int(-3, 3), new Vector2Int(-2, 2), new Vector2Int(3, 2), 
            new Vector2Int(4, 2), new Vector2Int(4, 1), new Vector2Int(6, -2), 
            new Vector2Int(4, -1), new Vector2Int(3, 0), new Vector2Int(2, 0)
        };
        GenerateTerrainForCoordinates(mapData, assignedTiles, altitude4Coords, 4, 
            altitudeColors[4], altitudeHeights[4], altitudeTerrainTypes[4]);
        
        // Generate altitude 2 terrain
        List<Vector2Int> altitude2Coords = new List<Vector2Int> {
            new Vector2Int(4, -6), new Vector2Int(3, -5), new Vector2Int(4, -5),
            new Vector2Int(2, -2), new Vector2Int(3, -3), new Vector2Int(4, -3),
            new Vector2Int(6, -4), new Vector2Int(-2, 6), new Vector2Int(-1, 4),
            new Vector2Int(0, 3), new Vector2Int(0, 2), new Vector2Int(2, 4),
            new Vector2Int(2, 3), new Vector2Int(1, 4), new Vector2Int(-5, 1),
            new Vector2Int(-5, 2), new Vector2Int(-6, 2), new Vector2Int(-2, 0),
            new Vector2Int(-4, -2), new Vector2Int(-3, -1), new Vector2Int(-3, 0)
        };
        GenerateTerrainForCoordinates(mapData, assignedTiles, altitude2Coords, 2, 
            altitudeColors[2], altitudeHeights[2], altitudeTerrainTypes[2]);
        
        // Generate altitude 1 terrain (lowest)
        List<Vector2Int> altitude1Coords = new List<Vector2Int> {
            new Vector2Int(-3, -2), new Vector2Int(-3, 1), new Vector2Int(-2, 5),
            new Vector2Int(1, 2), new Vector2Int(5, -3), new Vector2Int(2, -3)
        };
        GenerateTerrainForCoordinates(mapData, assignedTiles, altitude1Coords, 1, 
            altitudeColors[1], altitudeHeights[1], altitudeTerrainTypes[1]);
        
        // Fill the rest with altitude 3 (medium)
        FillRemainingWithAltitude(mapData, assignedTiles, mapSize, 3, 
            altitudeColors[3], altitudeHeights[3], altitudeTerrainTypes[3]);
            
        // Add player spawn points
        AddPlayerSpawnPoints(mapData, mapSize);
    }
    
    private static void GenerateTerrainForCoordinates(
        MapData mapData, 
        Dictionary<(int, int), bool> assignedTiles, 
        List<Vector2Int> coordinates, 
        int altitude,
        Color color, 
        float height, 
        string terrainType)
    {
        foreach (Vector2Int coord in coordinates)
        {
            int q = coord.x;
            int r = coord.y;
            
            // Skip if already assigned
            if (assignedTiles.ContainsKey((q, r)))
                continue;
                
            HexTileData tileData = new HexTileData(q, r, color, height, altitude, terrainType);
            mapData.AddHexTile(tileData);
            assignedTiles[(q, r)] = true;
        }
    }
    
    private static void FillRemainingWithAltitude(
        MapData mapData, 
        Dictionary<(int, int), bool> assignedTiles, 
        int mapSize,
        int altitude, 
        Color color, 
        float height, 
        string terrainType)
    {
        // Fill all remaining hexes in the map with the specified altitude
        for (int q = -mapSize; q <= mapSize; q++)
        {
            int r1 = Mathf.Max(-mapSize, -q - mapSize);
            int r2 = Mathf.Min(mapSize, -q + mapSize);
            
            for (int r = r1; r <= r2; r++)
            {
                // Skip if already assigned
                if (assignedTiles.ContainsKey((q, r)))
                    continue;
                
                HexTileData tileData = new HexTileData(q, r, color, height, altitude, terrainType);
                mapData.AddHexTile(tileData);
                assignedTiles[(q, r)] = true;
            }
        }
    }
    
    private static void AddPlayerSpawnPoints(MapData mapData, int mapSize)
    {
        // Use the specified player spawn locations
        Vector2Int[] spawnPoints = new Vector2Int[] {
            new Vector2Int(0, 6),
            new Vector2Int(0, -6),
            new Vector2Int(6, 0),
            new Vector2Int(-6, 0),
            new Vector2Int(-6, 6),
            new Vector2Int(6, -6)
        };

        // Add all spawn points
        for (int i = 0; i < spawnPoints.Length; i++)
        {
            Vector2Int spawnPoint = spawnPoints[i];
            HexTileData spawnTileData = mapData.GetHexTileData(spawnPoint.x, spawnPoint.y);
            
            // If the tile already exists, update it, otherwise create a new one
            if (spawnTileData == null)
            {
                spawnTileData = new HexTileData(spawnPoint.x, spawnPoint.y);
                mapData.AddHexTile(spawnTileData);
            }
            
            spawnTileData.specialFunction = $"PlayerSpawn{i + 1}";
        }
    }
}
#endif