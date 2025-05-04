using UnityEngine;

/// <summary>
/// Handles figure selection and highlighting
/// </summary>
public class FigureSelectionController : MonoBehaviour
{
    // Core references
    private IndicatorManager indicatorManager;
    
    // State
    private Figure selectedFigure;

    /// <summary>
    /// Initialize the selection controller
    /// </summary>
    public void Initialize(IndicatorManager indicatorManager)
    {
        this.indicatorManager = indicatorManager;
    }
    
    /// <summary>
    /// Select a figure and display its movement options
    /// </summary>
    public void SelectFigure(Figure figure, int availableSteps)
    {
        // First deselect any currently selected figure
        DeselectFigure();
        
        // Select the new figure
        selectedFigure = figure;
        if (selectedFigure != null)
        {
            selectedFigure.Select();
            
            // Show movement range
            if (availableSteps > 0)
            {
                indicatorManager.ShowPossibleMoves(figure.CurrentQ, figure.CurrentR, availableSteps, figure);
            }
        }
    }
    
    /// <summary>
    /// Deselect the currently selected figure
    /// </summary>
    public void DeselectFigure()
    {
        if (selectedFigure != null)
        {
            selectedFigure.Deselect();
            selectedFigure = null;
            
            ClearAllIndicators();
        }
    }

    public void ClearAllIndicators()
    {
        indicatorManager.ClearHighlightedPath();
        indicatorManager.ClearAttackIndicators();
        indicatorManager.ClearAllIndicators();
    }
    
    /// <summary>
    /// Get the currently selected figure
    /// </summary>
    public Figure GetSelectedFigure()
    {
        return selectedFigure;
    }
}