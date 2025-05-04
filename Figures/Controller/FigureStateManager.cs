using UnityEngine;

/// <summary>
/// Manages figure state across turns
/// </summary>
public class FigureStateManager : MonoBehaviour
{
    // Reference to other controllers
    private FigureMovementController movementController;
    private FigureCombatController combatController;
    private FigureSelectionController selectionController;

    /// <summary>
    /// Initialize the state manager
    /// </summary>
    public void Initialize()
    {
        // Get references to other controllers
        movementController = GetComponent<FigureMovementController>();
        combatController = GetComponent<FigureCombatController>();
        selectionController = GetComponent<FigureSelectionController>();
        
    }

    /// <summary>
    /// Handle phase changes from the turn system
    /// </summary>
    public void OnPhaseChanged(PlayerTurnSystem.PlayerTurnPhase newPhase)
    {
        // Reset figure state when entering TreasureCards phase
        if (newPhase == PlayerTurnSystem.PlayerTurnPhase.TreasureCards)
        {
            ResetFigureTurnState();
        }
    }
    
    /// <summary>
    /// Reset figure state for a new turn
    /// </summary>
    public void ResetFigureTurnState()
    {
        // Clear visited positions for all figures
        if (movementController != null)
        {
            movementController.ResetVisitedPositions();
        }

        // Reset attack tracking
        if (combatController != null)
        {
            combatController.ResetAttackTracking();
        }

        // Deselect any selected figure
        if (selectionController != null)
        {
            selectionController.DeselectFigure();
        }
    }
}