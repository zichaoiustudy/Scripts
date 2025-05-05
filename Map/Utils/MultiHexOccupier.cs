using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Figure))]
/// <summary>
/// Allows a figure (typically a boss) to occupy multiple hex tiles
/// </summary>
public class MultiHexOccupier : MonoBehaviour
{
    private List<Vector2Int> occupiedPositions = new List<Vector2Int>();
    private FigureManager figureManager;
    private Figure parentFigure;
    
    public void Initialize(List<Vector2Int> positions, FigureManager manager)
    {
        figureManager = manager;
        parentFigure = GetComponent<Figure>();
        
        if (parentFigure == null)
        {
            Debug.LogError("MultiHexOccupier must be attached to a Figure!");
            return;
        }
        
        // Store all positions
        occupiedPositions = new List<Vector2Int>(positions);
        
        // Register all positions with the figure manager
        RegisterOccupiedPositions();
        
        // Subscribe to figure's OnDefeated event
        parentFigure.OnDefeated += HandleFigureDefeated;
    }
    
    private void RegisterOccupiedPositions()
    {
        if (figureManager == null || parentFigure == null)
            return;
            
        // Skip main position (already tracked by the figure itself)
        Vector2Int mainPos = new Vector2Int(parentFigure.CurrentQ, parentFigure.CurrentR);
        
        foreach (var pos in occupiedPositions)
        {
            // Skip the main position
            if (pos.x == mainPos.x && pos.y == mainPos.y)
                continue;
                
            // Register additional positions
            figureManager.RegisterAdditionalPosition(parentFigure, pos.x, pos.y);
        }
    }

    // Ccheck if a position is occupied by this boss
    public bool OccupiesPosition(int q, int r)
    {
        foreach (Vector2Int pos in occupiedPositions)
        {
            if (pos.x == q && pos.y == r)
            {
                return true;
            }
        }
        return false;
    }
    
    private void ClearOccupiedPositions()
    {
        if (figureManager == null)
            return;
            
        figureManager.ClearAdditionalPositions(occupiedPositions, parentFigure);
    }
    
    private void HandleFigureDefeated(Figure figure)
    {
        // Clear all occupied positions when the figure is defeated
        ClearOccupiedPositions();
    }
    
    private void OnDestroy()
    {
        // Unsubscribe from events
        if (parentFigure != null)
        {
            parentFigure.OnDefeated -= HandleFigureDefeated;
        }
        
        // Clear all occupied positions when destroyed
        ClearOccupiedPositions();
    }
}