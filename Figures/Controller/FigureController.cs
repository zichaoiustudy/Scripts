using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Core controller that coordinates figure systems
/// </summary>
[RequireComponent(typeof(FigureSelectionController))]
[RequireComponent(typeof(FigureMovementController))]
[RequireComponent(typeof(FigureCombatController))]
[RequireComponent(typeof(FigureStateManager))]
public class FigureController : MonoBehaviour
{
    [Header("Component Controllers")]
    private FigureSelectionController selectionController;
    private FigureMovementController movementController;
    private FigureCombatController combatController;
    private FigureStateManager stateManager;

    // Core dependencies
    private FigureManager figureManager;
    private FieldManager fieldManager;
    private IndicatorManager indicatorManager;
    private PlayerTurnSystem turnSystem;

    #region Initialization

    /// <summary>
    /// Initialize the figure system
    /// </summary>
    public void Initialize()
    {   
        // Get dependencies
        if (ServiceLocator.Instance != null)
        {
            fieldManager = ServiceLocator.Instance.FieldManager;
            figureManager = ServiceLocator.Instance.FigureManager;
            indicatorManager = ServiceLocator.Instance.IndicatorManager;
            turnSystem = ServiceLocator.Instance.PlayerTurnSystem;
        }
        else
        {
            Debug.LogError("ServiceLocator not found!");
            return;
        }

        // Ensure this GameObject is active (for coroutines)
        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
        }

        selectionController = GetComponent<FigureSelectionController>();
        movementController = GetComponent<FigureMovementController>();
        combatController = GetComponent<FigureCombatController>();
        stateManager = GetComponent<FigureStateManager>();

        try
        {
            // Initialize sub-controllers (pass only what they need)
            selectionController.Initialize(indicatorManager);
            movementController.Initialize(this, fieldManager, figureManager, indicatorManager, turnSystem);
            combatController.Initialize(fieldManager, figureManager, indicatorManager);
            stateManager.Initialize();

            // Link controllers to each other
            movementController.SetController();
            combatController.SetController();

            // Subscribe to events
            if (turnSystem != null)
            {
                turnSystem.PhaseChanged += stateManager.OnPhaseChanged;
            }

            Debug.Log("FigureController initialization complete");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"FigureController initialization failed: {e.Message}");
        }
    }

    private void OnDestroy()
    {
        if (turnSystem != null)
        {
            turnSystem.PhaseChanged -= stateManager.OnPhaseChanged;
        }
    }

    #endregion

    #region Public API (Facade Methods)

    // Figure Selection
    public void SelectFigure(Figure figure, int availableActionPoints) => 
        selectionController.SelectFigure(figure, availableActionPoints);

    public void DeselectFigure() => 
        selectionController.DeselectFigure();

    public void ClearAllIndicators() =>
        selectionController.ClearAllIndicators();
    
    public Figure GetSelectedFigure() => 
        selectionController.GetSelectedFigure();

    public bool IsMovingFigure() => 
        movementController.IsMovingFigure();

    // Path Visualization
    public void OnIndicatorHover(PathIndicator indicator) => 
        movementController.OnIndicatorHover(indicator);

    public void OnIndicatorHoverExit(PathIndicator indicator) => 
        movementController.OnIndicatorHoverExit(indicator);

    public void ProcessPathCyclingInput() => 
        movementController.ProcessPathCyclingInput();

    // Movement
    public int MoveToIndicator(PathIndicator indicator, int availableActionPoints, System.Action onComplete = null) => 
        movementController.MoveToIndicator(indicator, availableActionPoints, onComplete);

    public void MarkPositionVisited(Figure figure, int q, int r) =>
        movementController.MarkPositionVisited(figure, q, r);

    public bool IsValidHex(int q, int r, Figure movingFigure = null) => 
        movementController.IsValidHex(q, r, movingFigure);

    public HashSet<Vector2Int> GetFigureVisitedPositions(Figure figure) => 
        movementController.GetFigureVisitedPositions(figure);

    // Combat
    public bool AttackFromIndicator(AttackIndicator indicator, int availableActionPoints, System.Action onComplete = null) => 
        combatController.AttackFromIndicator(indicator, availableActionPoints, onComplete);

    public void ShowAttackIndicators() => 
        combatController.ShowAttackIndicators();

    public List<Figure> GetAttackableTargets() => 
        combatController.GetAttackableTargets();

    public void OnAttackIndicatorHover(AttackIndicator indicator) =>
        combatController.OnAttackIndicatorHover(indicator);

    public void OnAttackIndicatorHoverExit(AttackIndicator indicator) =>
        combatController.OnAttackIndicatorHoverExit(indicator);

    // State Management
    public void ResetFigureTurnState() => 
        stateManager.ResetFigureTurnState();

    #endregion
}