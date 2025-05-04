using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Handles figure movement, path finding, and position tracking
/// </summary>
public class FigureMovementController : MonoBehaviour
{
    // Core references
    private FigureController controller;
    private FieldManager fieldManager;
    private FigureManager figureManager;
    private IndicatorManager indicatorManager;
    private FigureSelectionController selectionController;
    private PlayerTurnSystem turnSystem;
    
    // State
    private bool isMovingFigure = false;
    private PathIndicator hoveredIndicator;
    private List<List<Vector2Int>> possiblePaths = new List<List<Vector2Int>>();

    private int currentPathIndex = 0;
    
    // Position tracking
    private Dictionary<Figure, HashSet<Vector2Int>> figureVisitedPositions = new Dictionary<Figure, HashSet<Vector2Int>>();

    /// <summary>
    /// Initialize the movement controller
    /// </summary>
    public void Initialize(FigureController controller, FieldManager fieldManager, 
        FigureManager figureManager, IndicatorManager indicatorManager, PlayerTurnSystem turnSystem)
    {
        this.controller = controller;
        this.fieldManager = fieldManager;
        this.figureManager = figureManager;
        this.indicatorManager = indicatorManager;
        this.turnSystem = turnSystem;
    }
    
    /// <summary>
    /// Set the selection controller reference
    /// </summary>
    public void SetController()
    {
        selectionController = GetComponent<FigureSelectionController>();
    }

    #region Path Visualization & Interaction
    
    /// <summary>
    /// Called when the mouse hovers over a path indicator
    /// </summary>
    public void OnIndicatorHover(PathIndicator indicator)
    {
        hoveredIndicator = indicator;
        Figure selectedFigure = selectionController.GetSelectedFigure();

        // Only highlight paths when we have a figure selected
        if (selectedFigure != null && !isMovingFigure)
        {
            // Get the visited positions for this figure
            HashSet<Vector2Int> obstacles = GetFigureVisitedPositions(selectedFigure);

            // Calculate possible paths from selected figure to the hovered indicator
            Vector2Int start = new Vector2Int(selectedFigure.CurrentQ, selectedFigure.CurrentR);
            Vector2Int end = new Vector2Int(indicator.q, indicator.r);

            // Find multiple paths with altitude restrictions
            possiblePaths = HexUtils.FindMultiplePathsWithRestrictions(
                start, 
                end, 
                obstacles, 
                (pos) => IsValidHex(pos.x, pos.y)
            );

            // Reset path index
            currentPathIndex = 0;

            // Only highlight if at least one valid path was found
            if (possiblePaths.Count > 0)
            {
                // Display the first path
                indicatorManager.HighlightCustomPath(possiblePaths[currentPathIndex]);

                // Show how many paths are available
                int pathLength = possiblePaths[currentPathIndex].Count - 1;
                Debug.Log($"Path to ({indicator.q}, {indicator.r}) would use {pathLength} steps " +
                          $"[Path {currentPathIndex + 1} of {possiblePaths.Count}]");
            }
        }
    }

    /// <summary>
    /// Called when the mouse stops hovering over a path indicator
    /// </summary>
    public void OnIndicatorHoverExit(PathIndicator indicator)
    {
        // Only clear if this is the indicator we were previously hovering
        if (hoveredIndicator == indicator)
        {
            hoveredIndicator = null;
            possiblePaths.Clear();
            currentPathIndex = 0;
            
            // Clear the highlighted path
            indicatorManager.ClearHighlightedPath();
        }
    }

    /// <summary>
    /// Cycle to the next or previous path
    /// </summary>
    private void CycleToPath(bool forward)
    {
        if (possiblePaths.Count <= 1) return;

        // Update the path index
        if (forward)
        {
            currentPathIndex = (currentPathIndex + 1) % possiblePaths.Count;
        }
        else
        {
            currentPathIndex = (currentPathIndex - 1 + possiblePaths.Count) % possiblePaths.Count;
        }

        // Highlight the new path
        indicatorManager.HighlightCustomPath(possiblePaths[currentPathIndex]);

        // Display info about current path
        int pathLength = possiblePaths[currentPathIndex].Count - 1;
        Debug.Log($"Path to ({hoveredIndicator.q}, {hoveredIndicator.r}) would use {pathLength} steps " +
                  $"[Path {currentPathIndex + 1} of {possiblePaths.Count}]");
    }
    
    /// <summary>
    /// Handle path cycling via keyboard or mouse input
    /// </summary>
    public void ProcessPathCyclingInput()
    {
        if (hoveredIndicator == null || possiblePaths.Count <= 1) return;
        
        // Keyboard input
        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            CycleToPath(true);
        }
        else if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            CycleToPath(false);
        }

        // Mouse scroll input
        float scrollDelta = Input.GetAxis("Mouse ScrollWheel");
        if (scrollDelta > 0.01f)
        {
            CycleToPath(true);
        }
        else if (scrollDelta < -0.01f)
        {
            CycleToPath(false);
        }
    }
    
    #endregion

    #region Movement Execution
    
    /// <summary>
    /// Move the selected figure to the target indicator
    /// </summary>
    /// <returns>Number of steps used for the movement</returns>
    public int MoveToIndicator(PathIndicator indicator, int availableSteps, System.Action onComplete = null)
    {
        int stepsUsed = 0;
        Figure selectedFigure = selectionController.GetSelectedFigure();
        selectionController.ClearAllIndicators();

        if (selectedFigure == null)
        {
            onComplete?.Invoke();
            return stepsUsed;
        }

        // Disable player input while moving
        if (turnSystem != null && turnSystem.CurrentPhase == PlayerTurnSystem.PlayerTurnPhase.Actions)
        {
            turnSystem.WaitForPlayerInput(false);
        }

        // Get the path to follow
        List<Vector2Int> hexPath = GetPathToIndicator(indicator);

        // Check if a valid path exists
        if (hexPath.Count < 2)
        {
            onComplete?.Invoke();
            return stepsUsed;
        }

        // Calculate total distance (number of steps in the path minus the starting position)
        stepsUsed = hexPath.Count - 1;

        // Check if we have enough steps
        if (stepsUsed > availableSteps)
        {
            onComplete?.Invoke();
            return 0;
        }

        // Begin movement
        isMovingFigure = true;

        // Mark ALL positions in the path as visited for this figure
        for (int i = 0; i < hexPath.Count; i++)
        {
            Vector2Int pos = hexPath[i];
            MarkPositionVisited(selectedFigure, pos.x, pos.y);
        }

        // Animate the movement along the path
        StartCoroutine(AnimateMovementAlongPath(hexPath, () => {
            // Movement completed callback
            isMovingFigure = false;

            // Call the completion callback
            onComplete?.Invoke();
        }));
        
        return stepsUsed;
    }
    
    /// <summary>
    /// Get the path to follow to reach the target indicator
    /// </summary>
    private List<Vector2Int> GetPathToIndicator(PathIndicator indicator)
    {
        Figure selectedFigure = selectionController.GetSelectedFigure();
        if (selectedFigure == null) return new List<Vector2Int>();
        
        // Use the currently selected path if available
        if (hoveredIndicator == indicator && currentPathIndex < possiblePaths.Count)
        {
            return possiblePaths[currentPathIndex];
        }
        
        // Otherwise (direct click without hovering) calculate new paths
        Vector2Int startPos = new Vector2Int(selectedFigure.CurrentQ, selectedFigure.CurrentR);
        Vector2Int targetPos = new Vector2Int(indicator.q, indicator.r);

        // Get the visited positions to avoid
        HashSet<Vector2Int> obstacles = GetFigureVisitedPositions(selectedFigure);

        // Find multiple paths
        var paths = HexUtils.FindMultiplePathsWithRestrictions(
            startPos, 
            targetPos, 
            obstacles,
            (pos) => IsValidHex(pos.x, pos.y)
        );

        // Use the first (optimal) path if any exist
        if (paths.Count > 0)
            return paths[0];
        
        return new List<Vector2Int>(); // Empty path if none found
    }

    /// <summary>
    /// Animate figure movement along a path
    /// </summary>
    private IEnumerator AnimateMovementAlongPath(List<Vector2Int> path, System.Action onComplete)
    {
        Figure selectedFigure = selectionController.GetSelectedFigure();
        
        if (path.Count < 2 || selectedFigure == null)
        {
            onComplete?.Invoke();
            yield break;
        }

        // Skip the first position as it's the starting position
        for (int i = 1; i < path.Count; i++)
        {
            Vector2Int position = path[i];

            // Use a semaphore to wait for the animation to complete
            bool stepComplete = false;

            // Move the figure to the next position in the path
            figureManager.MoveFigure(selectedFigure, position.x, position.y, () => {
                stepComplete = true;
            });

            // Wait for movement animation to complete
            while (!stepComplete)
            {
                yield return null;
            }
        }

        // Notify that the figure has moved (for combat system)
        controller.SendMessage("MarkFigureMoved", selectedFigure, SendMessageOptions.DontRequireReceiver);

        // Call the completion callback
        onComplete?.Invoke();
    }
    
    /// <summary>
    /// Check if a figure is currently being moved
    /// </summary>
    public bool IsMovingFigure()
    {
        return isMovingFigure;
    }
    
    #endregion

    #region Position Validation & Tracking
    
    /// <summary>
    /// Check if a hex position is valid for movement
    /// </summary>
    public bool IsValidHex(int q, int r, Figure movingFigure = null)
    {
        // Use the current selected figure if none specified
        if (movingFigure == null)
        {
            movingFigure = selectionController.GetSelectedFigure();
        }

        // Check if we have dependencies
        if (figureManager == null || fieldManager == null)
        {
            return false;
        }

        // Check if the hex exists on the map
        GameObject hexObj = fieldManager.GetHex(q, r);
        if (hexObj == null)
            return false;

        // Check if the position is occupied by another figure
        if (figureManager.IsHexOccupied(q, r))
            return false;

        // Get terrain information - Commented out for now as mentioned in the original
        // HexTile hexTile = hexObj.GetComponent<HexTile>();
        // if (hexTile == null)
        //     return true;
        
        // if (hexTile.terrainType == "Unwalkable")
        //     return false;
        
        // if (movingFigure != null)
        // {
        //     if (hexTile.terrainType == "Water" && !CanTraverseWater(movingFigure))
        //         return false;
            
        //     if (hexTile.terrainType == "Mountain" && !CanClimbMountains(movingFigure))
        //         return false;
            
        //     if (!CheckFigureAbilityMovement(movingFigure, q, r, hexTile))
        //         return false;
        // }

        return true;
    }
    
    /// <summary>
    /// Mark a position as visited by a figure during this turn
    /// </summary>
    public void MarkPositionVisited(Figure figure, int q, int r)
    {
        if (figure == null) return;
        
        if (!figureVisitedPositions.TryGetValue(figure, out HashSet<Vector2Int> positions))
        {
            positions = new HashSet<Vector2Int>();
            figureVisitedPositions[figure] = positions;
        }
        
        positions.Add(new Vector2Int(q, r));
    }
    
    /// <summary>
    /// Get all positions visited by a figure during this turn
    /// </summary>
    public HashSet<Vector2Int> GetFigureVisitedPositions(Figure figure)
    {
        if (figure == null) return new HashSet<Vector2Int>();
        
        if (figureVisitedPositions.TryGetValue(figure, out HashSet<Vector2Int> positions))
        {
            return new HashSet<Vector2Int>(positions); // Return a copy to prevent external modification
        }
        
        return new HashSet<Vector2Int>();
    }
    
    /// <summary>
    /// Reset all visited position tracking
    /// </summary>
    public void ResetVisitedPositions()
    {
        figureVisitedPositions.Clear();
    }
    
    #endregion
}