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

    // Spawn tracking
    private Dictionary<int, List<Vector2Int>> playerSpawnLocations = new Dictionary<int, List<Vector2Int>>();
    private List<Vector2Int> monsterSpawnLocations = new List<Vector2Int>();
    private Dictionary<string, List<Vector2Int>> bossSpawnLocations = new Dictionary<string, List<Vector2Int>>();
    
    // Monster capture queue (monsters that players have captured)
    private Dictionary<int, List<GameObject>> capturedMonsters = new Dictionary<int, List<GameObject>>();

    public List<Vector2Int> GetMonsterSpawnLocations() => monsterSpawnLocations;
    public Dictionary<int, List<Vector2Int>> GetPlayerSpawnLocations() => playerSpawnLocations;
    public Dictionary<string, List<Vector2Int>> GetBossSpawnLocations() => bossSpawnLocations;

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

        // Load spawn data from map
        LoadSpawnLocationsFromMap();
        
    }
    
    // Load all spawn locations from map data
    public void LoadSpawnLocationsFromMap()
    {
        mapData = gameConfig != null ? gameConfig.mapData : null;
        if (mapData == null)
        {
            Debug.LogError("Map data not found!");
            return;
        }
        
        // Clear previous spawn locations
        playerSpawnLocations.Clear();
        monsterSpawnLocations.Clear();
        bossSpawnLocations.Clear();
        
        // Scan all tiles for special functions
        foreach (HexTileData tile in mapData.GetAllHexTiles())
        {
            if (tile.specialFunction == null) continue;
            
            // Process PlayerSpawn tiles
            if (tile.specialFunction.StartsWith("PlayerSpawn"))
            {
                string playerIdString = tile.specialFunction.Substring(11);
                if (int.TryParse(playerIdString, out int playerId))
                {
                    if (!playerSpawnLocations.ContainsKey(playerId))
                    {
                        playerSpawnLocations[playerId] = new List<Vector2Int>();
                    }
                    playerSpawnLocations[playerId].Add(new Vector2Int(tile.q, tile.r));
                }
            }
            // Process MonsterSpawn tiles
            else if (tile.specialFunction.StartsWith("MonsterSpawn"))
            {
                monsterSpawnLocations.Add(new Vector2Int(tile.q, tile.r));
            }
            // Process BossSpawn tiles
            else if (tile.specialFunction.StartsWith("BossSpawn"))
            {
                // Extract boss name (format: "BossSpawnDefaultName")
                string bossName = tile.specialFunction.Substring(9); // Skip "BossSpawn"

                if (!string.IsNullOrEmpty(bossName))
                {
                    // Create entry for this boss if it doesn't exist
                    if (!bossSpawnLocations.ContainsKey(bossName))
                    {
                        bossSpawnLocations[bossName] = new List<Vector2Int>();
                    }

                    // Add this tile to the boss's spawn group
                    bossSpawnLocations[bossName].Add(new Vector2Int(tile.q, tile.r));
                }
            }
        }
        
        Debug.Log($"Loaded {playerSpawnLocations.Count} player spawn groups, " + 
                 $"{monsterSpawnLocations.Count} monster spawn positions, and " +
                 $"{bossSpawnLocations.Count} boss with spawn locations");
    }

    // Spawn figures for each player at their spawn locations
    public void InitFigures(Dictionary<int, Color> playerColors)
    {
        // Load spawn locations if they haven't been loaded yet
        if (playerSpawnLocations.Count == 0)
        {
            LoadSpawnLocationsFromMap();
        }
        
        if (figurePool == null)
        {
            Debug.LogError("FigurePool not found!");
            return;
        }
        
        // For each player with spawn locations
        foreach (Player player in playerManager.Players)
        {
            int playerId = player.playerId;
            
            // Skip if no spawn locations for this player
            if (!playerSpawnLocations.ContainsKey(playerId))
            {
                Debug.LogWarning($"No spawn locations found for Player {playerId}");
                continue;
            }
            
            // Get figures assigned to this player
            List<GameObject> assignedFigurePrefabs = figurePool.GetFiguresForPlayer(playerId);
            List<Vector2Int> spawnLocations = playerSpawnLocations[playerId];
            
            // Calculate how many figures to spawn 
            int figureCount = Mathf.Min(spawnLocations.Count, assignedFigurePrefabs.Count);
            
            Debug.Log($"Player {playerId}: Spawning {figureCount} figures on {spawnLocations.Count} spawn locations");
            
            // Spawn each figure
            for (int i = 0; i < figureCount; i++)
            {
                Vector2Int spawnTile = spawnLocations[i];
                GameObject figurePrefab = assignedFigurePrefabs[i];
                
                // Get player color from dictionary
                Color playerColor = playerColors[playerId];
                
                Figure figure = SpawnFigure(playerId, spawnTile.x, spawnTile.y, playerColor, figurePrefab);
                if (figure != null)
                {
                    player.AddFigure(figure);
                }
            }
        }

        SpawnMonsters();
    }

    // Spawn monsters for system turn
    public List<Figure> SpawnMonsters(bool useLeftoverFigures = false)
    {
        List<Figure> spawnedMonsters = new List<Figure>();

        // Load spawn locations if they haven't been loaded
        if (monsterSpawnLocations.Count == 0)
        {
            Debug.Log("No monster spawn locations found!");
            return spawnedMonsters;
        }

        if (figurePool == null)
        {
            Debug.LogError("FigurePool not found!");
            return spawnedMonsters;
        }

        // Get figure prefabs based on the specified strategy
        List<GameObject> monsterPrefabs;

        if (useLeftoverFigures)
        {
            // Approach 1: Use figures not assigned to any player
            monsterPrefabs = figurePool.GetUnassignedFigures();
            Debug.Log($"Using {monsterPrefabs.Count} leftover figures for monster spawning");
        }
        else
        {
            // Approach 2: Use figures specifically assigned for monsters (-1 ID)
            string[] figureNames = new string[monsterSpawnLocations.Count];
            for (int i = 0; i < monsterSpawnLocations.Count; i++)
            {
                figureNames[i] = "Default";
            }
            figurePool.AssignDuplicateFiguresToPlayerByName(-1, figureNames);

            monsterPrefabs = figurePool.GetFiguresForPlayer(-1);
        }

        if (monsterPrefabs.Count == 0)
        {
            Debug.Log("No figure prefabs available for monsters");
            return spawnedMonsters;
        }

        // For each monster spawn location
        foreach (Vector2Int spawnPos in monsterSpawnLocations)
        {
            // Skip occupied positions
            if (IsHexOccupied(spawnPos.x, spawnPos.y))
                continue;

            // Choose a random figure prefab from the monster pool
            GameObject figurePrefab = monsterPrefabs[Random.Range(0, monsterPrefabs.Count)];

            // Spawn monster with ID -1 (system-controlled)
            Figure monster = SpawnFigure(-1, spawnPos.x, spawnPos.y, Color.gray, figurePrefab);
            if (monster != null)
            {
                spawnedMonsters.Add(monster);
                Debug.Log($"Spawned monster {monster.FigureName} at ({spawnPos.x}, {spawnPos.y})");
            }
        }

        return spawnedMonsters;
    }
    
    // Handle monster capture when a player defeats it
    public void CaptureMonster(Figure monster, int capturingPlayerId)
    {
        if (monster == null || monster.PlayerId >= 0) // Only capture system figures (negative IDs)
            return;

        Player player = playerManager.GetPlayerById(capturingPlayerId);
        if (player == null)
            return;

        // Store the figure prefab in the captured queue
        GameObject figurePrefab = figurePool.FindFigurePrefabByName(monster.FigureName);
        if (figurePrefab != null)
        {
            // Add to captured monsters queue
            if (!capturedMonsters.ContainsKey(capturingPlayerId))
            {
                capturedMonsters[capturingPlayerId] = new List<GameObject>();
            }
            capturedMonsters[capturingPlayerId].Add(figurePrefab);

            Debug.Log($"Player {capturingPlayerId} captured a {monster.FigureName}!");
        }
    }
    
    // Spawn any monsters that players have captured
    private void SpawnCapturedMonsters(Player player)
    {
        if (player == null)
            return;
            
        int playerId = player.playerId;
        
        // Check if player has any captured monsters
        if (!capturedMonsters.ContainsKey(playerId) || capturedMonsters[playerId].Count == 0)
            return;
            
        // Get available spawn locations
        if (!playerSpawnLocations.ContainsKey(playerId) || playerSpawnLocations[playerId].Count == 0)
            return;
            
        List<Vector2Int> availableSpawns = new List<Vector2Int>();
        foreach (Vector2Int spawnPos in playerSpawnLocations[playerId])
        {
            if (!IsHexOccupied(spawnPos.x, spawnPos.y))
            {
                availableSpawns.Add(spawnPos);
            }
        }
        
        // Spawn as many captured monsters as we have space for
        int monstersToSpawn = Mathf.Min(availableSpawns.Count, capturedMonsters[playerId].Count);
        for (int i = 0; i < monstersToSpawn; i++)
        {
            Vector2Int spawnPos = availableSpawns[i];
            GameObject monsterPrefab = capturedMonsters[playerId][i];
            
            // Get player color
            Color playerColor = player.playerColor;
            
            // Spawn the captured monster as a player figure
            Figure figure = SpawnFigure(playerId, spawnPos.x, spawnPos.y, playerColor, monsterPrefab);
            if (figure != null)
            {
                player.AddFigure(figure);
                Debug.Log($"Player {playerId} spawned captured {figure.FigureName} at ({spawnPos.x}, {spawnPos.y})");
            }
        }
        
        // Remove the spawned monsters from the captured list
        capturedMonsters[playerId].RemoveRange(0, monstersToSpawn);
    }
    
    // New methods for handling multi-hex figures
    
    // Register additional positions for multi-hex figures
    public void RegisterAdditionalPosition(Figure figure, int q, int r)
    {
        if (figure == null)
            return;
            
        string key = GetHexKey(q, r);
        figureMap[key] = figure;
    }
    
    // Clear additional positions when a multi-hex figure moves or is destroyed
    public void ClearAdditionalPositions(List<Vector2Int> positions, Figure figure)
    {
        if (figure == null)
            return;
            
        foreach (var pos in positions)
        {
            string key = GetHexKey(pos.x, pos.y);
            
            // Only remove if it's this figure occupying the position
            if (figureMap.TryGetValue(key, out Figure occupant) && occupant == figure)
            {
                figureMap.Remove(key);
            }
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
    
        // First check if there's a figure directly at this position
        if (figureMap.TryGetValue(key, out Figure figure))
        {
            return figure;
        }

        // If not found and we should check for boss occupancy
        // Check all known bosses to see if they occupy this position
        foreach (Figure potentialBoss in figureMap.Values)
        {
            // Skip if not a boss (boss ID is -2)
            if (potentialBoss.PlayerId != -2)
                continue;

            MultiHexOccupier occupier = potentialBoss.GetComponent<MultiHexOccupier>();
            if (occupier != null && occupier.OccupiesPosition(q, r))
            {
                return potentialBoss;
            }
        }
        
        return null;
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