using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages the game's main boss behavior and victory conditions
/// </summary>
public class BossManager : MonoBehaviour
{
    private List<Figure> bosses = new List<Figure>();
    
    private FigureManager figureManager;
    private GameStateSystem gameStateSystem;
    private FigurePool figurePool;
    
    public void Initialize()
    {
        figureManager = ServiceLocator.Instance?.FigureManager;
        gameStateSystem = ServiceLocator.Instance?.GameStateSystem;
        figurePool = ServiceLocator.Instance?.FigurePool;
        
        if (figureManager == null || gameStateSystem == null || figurePool == null)
        {
            Debug.LogError("BossManager: Required components not found during initialization!");
            return;
        }
        
        // Subscribe to defeat event
        if (ServiceLocator.Instance?.Events != null)
        {
            ServiceLocator.Instance.Events.Subscribe<FigureDefeatedEvent>(OnFigureDefeated);
        }
        
    }

    private void OnFigureDefeated(FigureDefeatedEvent evt)
    {
        // Check if the defeated figure was a boss
        if (evt.DefeatedFigure != null && bosses.Contains(evt.DefeatedFigure))
        {
            Debug.Log("Boss defeated! Game Over - Player Victory!");
            
            // Publish victory event
            if (ServiceLocator.Instance?.Events != null)
            {
                ServiceLocator.Instance.Events.Publish(new GameVictoryEvent 
                { 
                    WinningPlayerId = evt.AttackerFigure?.PlayerId ?? -1,
                    DefeatReason = "Boss defeated"
                });
            }
            
            // Transition to victory game state
            if (gameStateSystem != null)
            {
                gameStateSystem.SetGameState(GameStateSystem.GameState.Victory);
            }
        }
    }
    
    // Updated method with boss name parameter
    public void SpawnBoss(string bossName)
    {
        // Get spawn locations for this specific boss
        List<Vector2Int> bossSpawnLocations = new List<Vector2Int>();

        // Get locations from FigureManager
        Dictionary<string, List<Vector2Int>> bossSpawnGroups = figureManager.GetBossSpawnLocations();
        if (bossSpawnGroups.TryGetValue(bossName, out List<Vector2Int> locations))
        {
            bossSpawnLocations.AddRange(locations);
        }
        else
        {
            Debug.Log($"No spawn locations found for boss '{bossName}'");
            return;
        }

        // Check if we have any spawn locations
        if (bossSpawnLocations.Count == 0)
        {
            Debug.LogWarning($"Empty spawn location list for boss '{bossName}'");
            return;
        }

        // Get boss prefab by name
        GameObject bossPrefab = GetBossPrefab(bossName);
        if (bossPrefab == null)
        {
            Debug.LogError($"No prefab found for boss '{bossName}'");
            return;
        }

        // Verify all positions are free
        bool canSpawn = true;
        foreach (var pos in bossSpawnLocations)
        {
            if (figureManager.IsHexOccupied(pos.x, pos.y))
            {
                Debug.LogWarning($"Cannot spawn boss '{bossName}' - position {pos} is occupied");
                canSpawn = false;
                break;
            }
        }

        if (canSpawn)
        {
            // Find optimal center position
            Vector2Int centerPos = FindCenterPosition(bossSpawnLocations);

            // Clear reference to any existing boss
            // Spawn the boss (ID -2 indicates boss)
            Figure boss = figureManager.SpawnFigure(-2, centerPos.x, centerPos.y, Color.gray, bossPrefab);

            if (boss != null)
            {
                // Setup multi-hex occupation
                SetupMultiHexOccupation(boss, bossSpawnLocations);
                bosses.Add(boss);
                Debug.Log($"Spawned boss '{bossName}' at position {centerPos}, occupying {bossSpawnLocations.Count} hexes");
            }
        }
    }

    // Updated method to find boss prefab by name
    private GameObject GetBossPrefab(string bossName)
    {
        if (figurePool == null || string.IsNullOrEmpty(bossName))
            return null;

        // Get all boss prefabs from the figure pool
        List<GameObject> bossPrefabs = figurePool.GetBossPrefabs();

        foreach (var prefab in bossPrefabs)
        {
            if (prefab == null) continue;

            // Check the prefab name itself
            if (prefab.name.Equals(bossName, System.StringComparison.OrdinalIgnoreCase))
                return prefab;

            // Check if the prefab has a Figure component with this name
            Figure figure = prefab.GetComponent<Figure>();
            if (figure != null && figure.FigureName.Equals(bossName, System.StringComparison.OrdinalIgnoreCase))
                return prefab;
        }

        Debug.LogWarning($"No prefab found for boss '{bossName}'");
        return null;
    }
    
    private Vector2Int FindCenterPosition(List<Vector2Int> positions)
    {
        if (positions.Count == 0)
            return new Vector2Int(0, 0);
            
        if (positions.Count == 1)
            return positions[0];
            
        // Find the average position
        int sumQ = 0, sumR = 0;
        foreach (var pos in positions)
        {
            sumQ += pos.x;
            sumR += pos.y;
        }
        
        Vector2Int avg = new Vector2Int(sumQ / positions.Count, sumR / positions.Count);
        
        // Find the closest position to this average
        Vector2Int closest = positions[0];
        float minDist = HexUtils.HexDistance(closest, avg);
        
        for (int i = 1; i < positions.Count; i++)
        {
            float dist = HexUtils.HexDistance(positions[i], avg);
            if (dist < minDist)
            {
                minDist = dist;
                closest = positions[i];
            }
        }
        
        return closest;
    }
    
    private void SetupMultiHexOccupation(Figure boss, List<Vector2Int> positions)
    {
        // Add a MultiHexOccupier component to the boss
        MultiHexOccupier occupier = boss.gameObject.GetComponent<MultiHexOccupier>();
        if (occupier == null)
        {
            occupier = boss.gameObject.AddComponent<MultiHexOccupier>();
        }
        
        // Set the occupied positions
        occupier.Initialize(positions, figureManager);
    }
}