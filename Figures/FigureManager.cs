using System.Collections.Generic;
using UnityEngine;

public class FigureManager : MonoBehaviour
{
    [SerializeField] private float heightOffset = 0.5f;
    
    private MapData mapData;
    private Dictionary<string, Figure> figureMap = new Dictionary<string, Figure>();
    private FieldManager fieldManager;
    private PlayerManager playerManager;
    private FigurePool figurePool;
    private GameConfig gameConfig;

    public void Initialize()
    {
        fieldManager = ServiceLocator.Instance?.FieldManager;
        playerManager = ServiceLocator.Instance?.PlayerManager;
        figurePool = ServiceLocator.Instance?.FigurePool;
        gameConfig = ServiceLocator.Instance?.GameManager.GetGameConfig();
        if (fieldManager == null || playerManager == null || figurePool == null)
        {
            Debug.LogError("Required components not found during FigureManager initialization!");
        }
        
    }
    
    // Update spawn method to use the figure component on the prefab
    public Figure SpawnFigure(int playerId, int q, int r, Color playerColor, GameObject figurePrefab)
    {
        // Ensure we have a valid prefab
        if (figurePrefab == null)
        {
            Debug.LogError("Cannot spawn figure: Prefab is null");
            return null;
        }

        // Check if prefab has Figure component
        Figure prefabFigure = figurePrefab.GetComponent<Figure>();
        if (prefabFigure == null)
        {
            Debug.LogWarning($"Figure prefab {figurePrefab.name} is missing Figure component!");
            // We'll continue and attach one, but this indicates a setup issue
        }

        // Get the actual height of the hex tile
        float tileHeight = GetHexTileHeight(q, r);  
        Vector3 position = HexUtils.HexToWorldPosition(q, r, tileHeight + heightOffset);    
        GameObject figureObject = Instantiate(figurePrefab, position, Quaternion.identity);

        // Get the Figure component from the instance
        Figure figure = figureObject.GetComponent<Figure>();
        if (figure == null)
        {
            Debug.LogWarning($"Adding missing Figure component to {figurePrefab.name} instance");
            figure = figureObject.AddComponent<Figure>();
        }   

        // Get the figure name from the component or fallback to prefab name
        string figureName = !string.IsNullOrEmpty(figure.FigureName) ? figure.FigureName : figurePrefab.name;

        // Set the instance name
        figureObject.name = $"{figureName}_Player{playerId}_{q}_{r}";   

        // Initialize with player ID and hex coordinates
        figure.Initialize(playerId, figureName);
        figure.SetHexCoordinates(q, r); 

        // Apply the player's color directly
        Renderer renderer = figureObject.GetComponentInChildren<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = playerColor;
            figure.UpdateOriginalColor(playerColor);
        }   

        // Store the figure in our dictionary for tracking
        string key = GetHexKey(q, r);
        figureMap[key] = figure;    

        return figure;
    }
    
    // Spawn figures for each player at their spawn locations
    public void SpawnPlayerFigures(Dictionary<int, Color> playerColors)
    {
        mapData = gameConfig != null ? gameConfig.mapData : null;
        
        if (mapData == null || figurePool == null)
        {
            Debug.LogError("Required components not found!");
            return;
        }
                
        // First collect all spawn locations per player
        Dictionary<int, List<HexTileData>> spawnLocationsPerPlayer = new Dictionary<int, List<HexTileData>>();
        
        foreach (HexTileData tile in mapData.GetAllHexTiles())
        {
            if (tile.specialFunction != null && tile.specialFunction.StartsWith("PlayerSpawn"))
            {                
                // Extract player ID from the special function name (e.g., "PlayerSpawn1" -> 1)
                string playerIdString = tile.specialFunction.Substring(11);
                if (int.TryParse(playerIdString, out int playerId))
                {
                    // Add to the player's spawn locations list
                    if (!spawnLocationsPerPlayer.ContainsKey(playerId))
                    {
                        spawnLocationsPerPlayer[playerId] = new List<HexTileData>();
                    }
                    spawnLocationsPerPlayer[playerId].Add(tile);
                }
            }
        }
        
        // For each player with spawn locations
        foreach (Player player in playerManager.Players)
        {
            int playerId = player.playerId;
            
            // Skip if no spawn locations for this player
            if (!spawnLocationsPerPlayer.ContainsKey(playerId))
            {
                Debug.LogWarning($"No spawn locations found for Player {playerId}");
                continue;
            }
            
            // Get figures assigned to this player
            List<GameObject> assignedFigurePrefabs = figurePool.GetFiguresForPlayer(playerId);
            List<HexTileData> spawnLocations = spawnLocationsPerPlayer[playerId];
            
            // Calculate how many figures to spawn (minimum of available spawn locations and assigned figures)
            int figureCount = Mathf.Min(spawnLocations.Count, assignedFigurePrefabs.Count);
            
            Debug.Log($"Player {playerId}: Spawning {figureCount} figures on {spawnLocations.Count} spawn locations");
            
            // Spawn each figure
            for (int i = 0; i < figureCount; i++)
            {
                HexTileData spawnTile = spawnLocations[i];
                GameObject figurePrefab = assignedFigurePrefabs[i];
                
                // Get player color from dictionary
                Color playerColor = playerColors[playerId];
                
                // Simplified spawn call - no figureName needed
                Figure figure = SpawnFigure(playerId, spawnTile.q, spawnTile.r, playerColor, figurePrefab);
                if (figure != null)
                {
                    player.AddFigure(figure);
                }
            }
        }
    }
    
    // Update the MoveFigure method to use animation
    public void MoveFigure(Figure figure, int newQ, int newR, System.Action onComplete = null)
    {
        if (figure == null)
        {
            onComplete?.Invoke();
            return;
        }

        // Remove figure from old position tracking before animation starts
        string oldKey = GetHexKey(figure.CurrentQ, figure.CurrentR);
        if (figureMap.ContainsKey(oldKey))
        {
            figureMap.Remove(oldKey);
        }

        // Get the actual height of the destination hex tile
        float tileHeight = GetHexTileHeight(newQ, newR);

        // Calculate the new world position with adjusted height
        // Use the tile's height plus our standard offset
        Vector3 newPosition = HexUtils.HexToWorldPosition(newQ, newR, tileHeight + heightOffset);

        // Move the figure with animation
        figure.MoveToPosition(newPosition, () => {
            // Update the figure's coordinates
            figure.SetHexCoordinates(newQ, newR);

            // Add to new position in tracking dictionary
            string newKey = GetHexKey(newQ, newR);
            figureMap[newKey] = figure;

            Debug.Log($"Figure moved to ({newQ}, {newR})");

            // Call the completion callback
            onComplete?.Invoke();
        });
    }
    
    // Get the figure at specified hex coordinates
    public Figure GetFigureAt(int q, int r)
    {
        string key = GetHexKey(q, r);
        figureMap.TryGetValue(key, out Figure figure);
        return figure;
    }
    
    // Check if a hex position is occupied
    public bool IsHexOccupied(int q, int r)
    {
        return GetFigureAt(q, r) != null;
    }
    
    // Create a string key from hex coordinates
    private string GetHexKey(int q, int r)
    {
        return $"{q}_{r}";
    }

    // Helper method to get hex tile height
    private float GetHexTileHeight(int q, int r)
    {
        // Default height if we can't find the tile
        float tileHeight = 0f;

        // Safely attempt to get the hex tile height
        if (fieldManager != null)
        {
            GameObject hexObj = fieldManager.GetHex(q, r);
            if (hexObj != null)
            {
                HexTile hexTile = hexObj.GetComponent<HexTile>();
                if (hexTile != null)
                {
                    tileHeight = hexTile.height;
                }
            }
        }
        else
        {
            Debug.LogWarning("FieldManager not available when getting hex tile height");
        }
        
        return tileHeight;
    }
}