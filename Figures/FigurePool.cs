using System.Collections.Generic;
using UnityEngine;

public class FigurePool
{
    // List of all available figure prefabs
    private List<GameObject> availableFigurePrefabs = new List<GameObject>();
    
    // Track which figures are assigned to which player (for unique figures)
    private Dictionary<GameObject, int> figureAssignments = new Dictionary<GameObject, int>();
    
    // Track how many copies of each figure name are assigned to each player (for duplicates)
    private Dictionary<string, Dictionary<int, int>> figureCopyCounts = new Dictionary<string, Dictionary<int, int>>();
    
    #region Initialization
    
    /// <summary>
    /// Initialize the figure pool from GameConfig
    /// </summary>
    public void Initialize(GameConfig config)
    {
        // Reset all figures
        availableFigurePrefabs.Clear();
        figureAssignments.Clear();
        figureCopyCounts.Clear();
        
        if (config != null && config.figurePrefabs != null && config.figurePrefabs.Count > 0)
        {
            // Copy all figure prefabs from the config
            foreach(var prefab in config.figurePrefabs)
            {
                if (prefab != null)
                {
                    availableFigurePrefabs.Add(prefab);
                }
            }
            
            Debug.Log($"FigurePool initialized with {availableFigurePrefabs.Count} figure prefabs");
        }
        else
        {
            Debug.LogWarning("No figure prefabs in the pool! Character models will be missing.");
        }
    }
    
    /// <summary>
    /// Reset all assignments
    /// </summary>
    public void ResetAssignments()
    {
        figureAssignments.Clear();
        figureCopyCounts.Clear();
    }
    
    #endregion
    
    #region Figure Inventory Methods
    
    /// <summary>
    /// Get figures that haven't been uniquely assigned to any player
    /// </summary>
    public List<GameObject> GetUnassignedFigures()
    {
        List<GameObject> unassigned = new List<GameObject>();
        foreach (var figure in availableFigurePrefabs)
        {
            if (!figureAssignments.ContainsKey(figure))
            {
                unassigned.Add(figure);
            }
        }
        return unassigned;
    }
    
    /// <summary>
    /// Find figure prefab by name
    /// </summary>
    private GameObject FindFigurePrefabByName(string figureName)
    {
        if (string.IsNullOrEmpty(figureName))
            return null;
    
        foreach (var prefab in availableFigurePrefabs)
        {
            if (prefab == null) 
                continue;
                
            // Check if it matches the prefab name directly
            if (prefab.name.Equals(figureName, System.StringComparison.OrdinalIgnoreCase))
                return prefab;
                
            // Also check if it has a Figure component with matching name
            Figure component = prefab.GetComponent<Figure>();
            if (component != null && !string.IsNullOrEmpty(component.FigureName) && 
                component.FigureName.Equals(figureName, System.StringComparison.OrdinalIgnoreCase))
                return prefab;
        }
        
        return null; // No match found
    }
    
    /// <summary>
    /// Get the display name of a figure prefab
    /// </summary>
    private string GetFigureDisplayName(GameObject figurePrefab)
    {
        if (figurePrefab == null)
            return "Unknown";
            
        Figure component = figurePrefab.GetComponent<Figure>();
        return component != null && !string.IsNullOrEmpty(component.FigureName) 
            ? component.FigureName 
            : figurePrefab.name;
    }
    
    #endregion
    
    #region Method 1: Random Assignment (Unique Figures)
    
    /// <summary>
    /// Randomly assign unique figures to a player
    /// </summary>
    public List<GameObject> RandomlyAssignFiguresToPlayer(int playerId, int count)
    {
        List<GameObject> unassignedFigures = GetUnassignedFigures();
        List<GameObject> assignedFigures = new List<GameObject>();
        
        // Shuffle the unassigned figures
        for (int i = 0; i < unassignedFigures.Count; i++)
        {
            int rnd = Random.Range(i, unassignedFigures.Count);
            GameObject temp = unassignedFigures[i];
            unassignedFigures[i] = unassignedFigures[rnd];
            unassignedFigures[rnd] = temp;
        }
        
        // Assign up to 'count' figures
        int figuresAssigned = 0;
        foreach (GameObject figure in unassignedFigures)
        {
            if (figuresAssigned >= count) break;
            
            // Assign figure to player
            figureAssignments[figure] = playerId;
            assignedFigures.Add(figure);
            figuresAssigned++;
        }
        
        // Get figure names for logging
        List<string> figureNames = new List<string>();
        foreach (GameObject fig in assignedFigures)
        {
            figureNames.Add(GetFigureDisplayName(fig));
        }
        
        Debug.Log($"Player {playerId} randomly assigned {assignedFigures.Count} figures: {string.Join(", ", figureNames)}");
        return assignedFigures;
    }
    
    #endregion
    
    #region Method 2: Named Assignment (Unique Figures)
    
    /// <summary>
    /// Assign specific figure to player by name (unique assignment - one figure per player)
    /// </summary>
    public GameObject AssignFigureToPlayerByName(int playerId, string figureName)
    {
        // Find the figure prefab with the given name
        GameObject figurePrefab = FindFigurePrefabByName(figureName);
        
        // If not found, return null
        if (figurePrefab == null)
        {
            Debug.LogWarning($"Figure with name '{figureName}' not found in pool");
            return null;
        }
        
        // Check if already assigned to another player
        if (figureAssignments.TryGetValue(figurePrefab, out int currentPlayerId) && currentPlayerId != playerId)
        {
            Debug.LogWarning($"Figure '{figureName}' is already assigned to Player {currentPlayerId}");
            return null;
        }
        
        // Assign to the player
        figureAssignments[figurePrefab] = playerId;
        
        string displayName = GetFigureDisplayName(figurePrefab);
        Debug.Log($"Player {playerId} assigned unique figure: {displayName}");
        
        return figurePrefab;
    }
    
    /// <summary>
    /// Assign multiple specific figures to a player by name (unique assignment)
    /// </summary>
    public List<GameObject> AssignFiguresToPlayerByName(int playerId, string[] figureNames)
    {
        List<GameObject> assignedFigures = new List<GameObject>();
        
        if (figureNames == null || figureNames.Length == 0)
            return assignedFigures;
        
        foreach (string name in figureNames)
        {
            GameObject figure = AssignFigureToPlayerByName(playerId, name);
            if (figure != null)
            {
                assignedFigures.Add(figure);
            }
        }
        
        if (assignedFigures.Count > 0)
        {
            List<string> names = assignedFigures.ConvertAll(fig => GetFigureDisplayName(fig));
        }
        
        return assignedFigures;
    }
    
    #endregion
    
    #region Method 3: Named Assignment with Duplicates
    
    /// <summary>
    /// Assign figure to player by name, allowing multiple copies of the same figure
    /// </summary>
    public GameObject AssignDuplicateFigureToPlayerByName(int playerId, string figureName)
    {
        // Find the figure prefab with the given name
        GameObject figurePrefab = FindFigurePrefabByName(figureName);
        
        // If not found, return null
        if (figurePrefab == null)
        {
            Debug.LogWarning($"Figure with name '{figureName}' not found in pool");
            return null;
        }
        
        // Get the figure's display name
        string displayName = GetFigureDisplayName(figurePrefab);
        
        // Track how many of this figure the player now has
        if (!figureCopyCounts.ContainsKey(displayName))
        {
            figureCopyCounts[displayName] = new Dictionary<int, int>();
        }
        
        if (!figureCopyCounts[displayName].ContainsKey(playerId))
        {
            figureCopyCounts[displayName][playerId] = 0;
        }
        
        figureCopyCounts[displayName][playerId]++;
        
        int copyNumber = figureCopyCounts[displayName][playerId];
        Debug.Log($"Player {playerId} assigned duplicate figure: {displayName} (Copy #{copyNumber})");
        
        return figurePrefab;
    }
    
    /// <summary>
    /// Assign multiple figures to a player by name, allowing duplicates
    /// </summary>
    public List<GameObject> AssignDuplicateFiguresToPlayerByName(int playerId, string[] figureNames)
    {
        List<GameObject> assignedFigures = new List<GameObject>();
        Dictionary<string, int> countByName = new Dictionary<string, int>();
        
        if (figureNames == null || figureNames.Length == 0)
            return assignedFigures;
        
        foreach (string name in figureNames)
        {
            GameObject figure = AssignDuplicateFigureToPlayerByName(playerId, name);
            if (figure != null)
            {
                assignedFigures.Add(figure);
                
                // Track counts for logging
                string displayName = GetFigureDisplayName(figure);
                if (!countByName.ContainsKey(displayName))
                {
                    countByName[displayName] = 0;
                }
                countByName[displayName]++;
            }
        }
        
        if (assignedFigures.Count > 0)
        {
            List<string> summaryList = new List<string>();
            foreach (var entry in countByName)
            {
                summaryList.Add($"{entry.Key} x{entry.Value}");
            }
        }
        
        return assignedFigures;
    }
    
    #endregion
    
    #region Get Assigned Figures
    
    /// <summary>
    /// Get prefabs of figures assigned to a player (for spawning)
    /// </summary>
    public List<GameObject> GetFiguresForPlayer(int playerId)
    {
        List<GameObject> result = new List<GameObject>();
        
        // Get uniquely assigned figures
        foreach (var entry in figureAssignments)
        {
            if (entry.Value == playerId)
            {
                result.Add(entry.Key);
            }
        }
        
        // Get duplicate figures
        foreach (var figureEntry in figureCopyCounts)
        {
            string figureName = figureEntry.Key;
            var playerCounts = figureEntry.Value;
            
            if (playerCounts.TryGetValue(playerId, out int count) && count > 0)
            {
                // Find the corresponding prefab
                GameObject prefab = FindFigurePrefabByName(figureName);
                if (prefab != null)
                {
                    // Add it the number of times it's assigned
                    for (int i = 0; i < count; i++)
                    {
                        result.Add(prefab);
                    }
                }
            }
        }
        
        return result;
    }
    
    #endregion
}