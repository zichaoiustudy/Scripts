using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Creates and manages visual indicators for possible movement paths on the hex grid
/// </summary>
public class IndicatorManager : MonoBehaviour
{
    #region Inspector Fields
    
    [SerializeField] private GameObject indicatorPrefab;
    [SerializeField] private GameObject indicatorBarrier;
    [SerializeField] private GameObject attackableIndicatorPrefab;
    [SerializeField] private Material pathHighlightMaterial;
    [SerializeField] private float indicatorHeightOffset = 1f;
    
    #endregion
    
    #region Private Fields
    
    private FigureController figureController;
    private Dictionary<string, GameObject> activeIndicators = new Dictionary<string, GameObject>();
    private List<GameObject> currentHighlightedPath = new List<GameObject>(); // Track highlighted indicators
    
    #endregion
    
    #region Initialization
    
    public void Initialize()
    {
        if (ServiceLocator.Instance != null)
        {
            figureController = ServiceLocator.Instance.FigureController;
        }
        else
        {
            Debug.LogError("ServiceLocator not found!");
        }
    }
    
    #endregion
    
    #region Public API
    
    /// <summary>
    /// Clear all active movement indicators
    /// </summary>
    public void ClearAllIndicators()
    {
        foreach (GameObject indicator in activeIndicators.Values)
        {
            Destroy(indicator);
        }
        
        activeIndicators.Clear();
    }

    /// <summary>
    /// Check if an indicator exists at a position
    /// </summary>
    public bool HasIndicatorAt(int q, int r)
    {
        string key = GetPositionKey(q, r);
        return activeIndicators.ContainsKey(key);
    }
    
    /// <summary>
    /// Get the indicator at a position
    /// </summary>
    public GameObject GetIndicatorAt(int q, int r)
    {
        string key = GetPositionKey(q, r);
        if (activeIndicators.TryGetValue(key, out GameObject indicator))
        {
            return indicator;
        }
        return null;
    }
    
    #endregion
    
    #region Path Visualization
    
    /// <summary>
    /// Show movement indicators for all possible destinations within steps
    /// </summary>
    public void ShowPossibleMoves(int startQ, int startR, int steps, Figure movingFigure = null)
    {
        ClearAllIndicators();

        if (steps <= 0) return; 

        // Update FigureController's tracking directly if needed
        if (figureController != null && movingFigure != null)
        {
            figureController.MarkPositionVisited(movingFigure, startQ, startR);
        }

        // Find all reachable positions within steps
        List<Vector2Int> reachablePositions = CalculateReachablePositions(startQ, startR, steps, movingFigure);

        // Create indicators for each reachable position
        foreach (Vector2Int pos in reachablePositions)
        {
            CreateIndicator(pos.x, pos.y);
        }
    }
    
    /// <summary>
    /// Highlight a path from start to end and show step numbers
    /// </summary>
    public void HighlightCustomPath(List<Vector2Int> hexPath)
    {
        // First clear any existing highlighted path
        ClearHighlightedPath();

        // Skip the first position (starting position) if path has more than one hex
        int startIndex = hexPath.Count > 1 ? 1 : 0;

        // Highlight each position in the path
        for (int i = startIndex; i < hexPath.Count; i++)
        {
            Vector2Int pos = hexPath[i];
            GameObject indicator = GetIndicatorAt(pos.x, pos.y);

            if (indicator != null)
            {
                // Highlight this indicator
                PathIndicator indicatorObj = indicator.GetComponent<PathIndicator>();
                if (indicatorObj != null)
                {
                    indicatorObj.SetHighlighted(true, pathHighlightMaterial);
                    
                    // Set step number (i is the step number from the start)
                    indicatorObj.SetStepNumber(i, true);
                }

                // Add to our tracking list
                currentHighlightedPath.Add(indicator);
            }
        }
    }

    /// <summary>
    /// Clear any currently highlighted path
    /// </summary>
    public void ClearHighlightedPath()
    {
        foreach (GameObject obj in currentHighlightedPath)
        {
            PathIndicator indicator = obj.GetComponent<PathIndicator>();
            if (indicator != null)
            {
                indicator.SetHighlighted(false);
                indicator.SetStepNumber(0, false);
            }
        }
        
        currentHighlightedPath.Clear();
    }
    
    #endregion
    
    #region Movement Calculation
    
    /// <summary>
    /// Calculate all reachable positions, taking into account obstacles and altitude restrictions
    /// </summary>
    private List<Vector2Int> CalculateReachablePositions(int startQ, int startR, int steps, Figure movingFigure = null)
    {
        List<Vector2Int> result = new List<Vector2Int>();
        Vector2Int start = new Vector2Int(startQ, startR);

        // Use modified BFS algorithm with altitude checks
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();
        Queue<(Vector2Int pos, int stepsLeft, int prevAltitude)> queue = new Queue<(Vector2Int, int, int)>();

        // Get the starting altitude
        int startAltitude = HexUtils.GetHexAltitude(startQ, startR);

        // Add only this figure's starting position
        visited.Add(start); 

        // Get figure-specific visited positions from FigureController if available
        if (figureController != null && movingFigure != null)
        {
            // Use the figure-specific visited positions instead of the global one
            var figureVisited = figureController.GetFigureVisitedPositions(movingFigure);
            foreach (Vector2Int pos in figureVisited)
            {
                visited.Add(pos);
            }
        }

        queue.Enqueue((start, steps, startAltitude));

        while (queue.Count > 0)
        {
            (Vector2Int pos, int stepsLeft, int prevAltitude) = queue.Dequeue();

            if (stepsLeft <= 0) continue;

            // Process each neighbor
            foreach (Vector2Int neighbor in HexUtils.GetHexNeighbors(pos.x, pos.y))
            {
                // Skip if already visited or inaccessible
                if (visited.Contains(neighbor)) continue;

                // Check if this hex can be moved to
                if (!CanMoveTo(neighbor.x, neighbor.y)) continue;

                // Get the altitude of this neighbor
                int neighborAltitude = HexUtils.GetHexAltitude(neighbor.x, neighbor.y);

                // Check if the altitude difference is acceptable
                int altitudeDiff = Mathf.Abs(neighborAltitude - prevAltitude);
                if (altitudeDiff > 1) continue;

                // Mark as visited to avoid revisiting
                visited.Add(neighbor);

                // Add as a valid destination
                result.Add(neighbor);

                // Continue search with one less step
                if (stepsLeft > 1)
                {
                    queue.Enqueue((neighbor, stepsLeft - 1, neighborAltitude));
                }
            }
        }

        return result;
    }

    /// <summary>
    /// Check if a position is valid for movement (delegates to FigureController)
    /// </summary>
    private bool CanMoveTo(int q, int r)
    {
        return figureController != null && figureController.IsValidHex(q, r);
    }
    
    #endregion
    
    #region Indicator Creation
    
    /// <summary>
    /// Create an indicator at the specified hex position
    /// </summary>
    private void CreateIndicator(int q, int r)
    {
        string key = GetPositionKey(q, r);
        
        // Check if we already have an indicator for this position
        if (activeIndicators.ContainsKey(key))
            return;
            
        // Get hex altitude information
        int hexAltitude = HexUtils.GetHexAltitude(q, r);
    
        // Calculate world position with fixed height
        Vector3 position = HexUtils.HexToWorldPosition(q, r, indicatorHeightOffset);
    
        // Create the indicator game object
        GameObject indicator = Instantiate(indicatorPrefab, position, indicatorPrefab.transform.rotation, transform);
        indicator.name = $"PathIndicator_{q}_{r}";
        
        // Store coordinates in the indicator object
        PathIndicator indicatorObj = indicator.GetComponent<PathIndicator>();
        if (indicatorObj == null)
        {
            indicatorObj = indicator.AddComponent<PathIndicator>();
        }
        indicatorObj.q = q;
        indicatorObj.r = r;
    
        // Hide step number by default
        indicatorObj.SetStepNumber(0, false); 
        
        // Add to tracking dictionary
        activeIndicators.Add(key, indicator);
        
        // Create barriers for edges with invalid altitude transitions
        CreateBarriersForIndicator(q, r, hexAltitude);
    }
    
    /// <summary>
    /// Creates barrier objects at hex edges that cannot be crossed due to altitude restrictions
    /// </summary>
    private void CreateBarriersForIndicator(int q, int r, int altitude)
    {
        // Check all six neighboring hexes
        for (int i = 0; i < HexUtils.HexDirections.Length; i++)
        {
            Vector2Int dir = HexUtils.HexDirections[i];
            int neighborQ = q + dir.x;
            int neighborR = r + dir.y;
            
            // Get neighbor's altitude
            int neighborAltitude = HexUtils.GetHexAltitude(neighborQ, neighborR);
            
            // Check if altitude difference is too great
            if (Mathf.Abs(neighborAltitude - altitude) > 1)
            {
                // Create a barrier at this edge
                CreateBarrierAtEdge(q, r, i);
            }
        }
    }

    /// <summary>
    /// Creates a barrier at the specified edge of a hex
    /// </summary>
    private void CreateBarrierAtEdge(int q, int r, int edgeIndex)
    {
        // Skip if barrier prefab is missing
        if (indicatorBarrier == null) return;
    
        // Calculate position for barrier (centered on the edge between hexes)
        Vector3 hexCenter = HexUtils.HexToWorldPosition(q, r, indicatorHeightOffset);
        Vector2Int dir = HexUtils.HexDirections[edgeIndex];
    
        // Calculate the edge midpoint (halfway between hex center and neighbor center)
        Vector3 neighborCenter = HexUtils.HexToWorldPosition(q + dir.x, r + dir.y, indicatorHeightOffset);
        // Set the offset to 0.45f to move the barrier slightly closer to the center of the hex
        Vector3 edgePosition = Vector3.Lerp(hexCenter, neighborCenter, 0.45f);
    
        // Create with the initial rotation from prefab
        GameObject barrier = Instantiate(indicatorBarrier, edgePosition, indicatorBarrier.transform.rotation, transform);
        barrier.name = $"Barrier_{q}_{r}_Edge{edgeIndex}";

        // Then apply the edge-specific rotation in world space
        barrier.transform.Rotate(Vector3.up, 60f * (edgeIndex+1), Space.World);
    
        // Add to tracking by using a special key
        string barrierKey = $"Barrier_{q}_{r}_Edge{edgeIndex}";
        activeIndicators[barrierKey] = barrier;
    }

    /// <summary>
    /// Create attack indicator at the specified hex position
    /// </summary>
    public void CreateAttackIndicator(int q, int r, Figure targetFigure)
    {
        string key = GetPositionKey(q, r) + "_attack";
        
        // Check if we already have an attack indicator for this position
        if (activeIndicators.ContainsKey(key))
            return;
            
        Vector3 position = HexUtils.HexToWorldPosition(q, r, indicatorHeightOffset);
        
        // Create the attack indicator game object
        GameObject indicator = Instantiate(attackableIndicatorPrefab, position, attackableIndicatorPrefab.transform.rotation, transform);
        indicator.name = $"AttackIndicator_{q}_{r}";

        // Add AttackIndicator component and set properties
        AttackIndicator attackIndicator = indicator.GetComponent<AttackIndicator>();
        if (attackIndicator == null)
        {
            attackIndicator = indicator.AddComponent<AttackIndicator>();
        }
        attackIndicator.q = q;
        attackIndicator.r = r;
        attackIndicator.targetFigure = targetFigure;
        
        // Default cost for attack is 1 action point
        attackIndicator.SetCost(1, false);
        
        // Add to tracking dictionary
        activeIndicators.Add(key, indicator);
        
        // Add visual feedback (pulsing animation, etc.)
        // You can add a simple animation script to the attack indicator here if desired
    }

    /// <summary>
    /// Clear all attack indicators
    /// </summary>
    public void ClearAttackIndicators()
    {
        List<string> attackIndicatorKeys = new List<string>();
        
        // Find all attack indicator keys
        foreach (KeyValuePair<string, GameObject> pair in activeIndicators)
        {
            if (pair.Key.EndsWith("_attack"))
            {
                attackIndicatorKeys.Add(pair.Key);
            }
        }
        
        // Remove all attack indicators
        foreach (string key in attackIndicatorKeys)
        {
            if (activeIndicators.TryGetValue(key, out GameObject indicator))
            {
                Destroy(indicator);
                activeIndicators.Remove(key);
            }
        }
    }
    
    #endregion
    
    #region Utilities
    
    /// <summary>
    /// Generate a unique key for a hex position
    /// </summary>
    private string GetPositionKey(int q, int r)
    {
        return $"{q}_{r}";
    }
    
    #endregion
}