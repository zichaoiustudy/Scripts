using System.Runtime.InteropServices.WindowsRuntime;
using UnityEngine;

[RequireComponent(typeof(PlayerManager))]
public class PlayerController : MonoBehaviour
{
    private int activePlayerId = 0;
    private bool waitingForAnimation = false;
    private Camera mainCamera;
    private PlayerTurnSystem turnSystem;
    private PlayerManager playerManager;
    private FigureController figureController;

    public void Initialize()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;
            
        if (ServiceLocator.Instance != null)
        {
            turnSystem = ServiceLocator.Instance.PlayerTurnSystem;
            playerManager = ServiceLocator.Instance.PlayerManager;
            figureController = ServiceLocator.Instance.FigureController;
        }
        else
        {
            Debug.LogError("ServiceLocator not found!");
            return;
        }
    }
    
    private void Update()
    {
        if (turnSystem == null || playerManager == null || figureController == null)
            return;
            
        // Don't process input if we're animating a figure movement
        if (figureController.IsMovingFigure() || waitingForAnimation)
            return;
            
        // Check action points after animations complete
        Player currentPlayer = playerManager?.GetPlayerById(activePlayerId);
        if (currentPlayer != null && 
            turnSystem.CurrentPhase == PlayerTurnSystem.PlayerTurnPhase.Actions && 
            currentPlayer.actionPoints <= 0)
        {
            // Deselect any figure and move to next phase
            figureController.DeselectFigure();
            turnSystem.AdvanceToNextPhase();
        }
            
        if (Input.GetMouseButtonDown(0))
        {
            HandleClick();
        }
        
        // Cancel current selection with right click
        if (Input.GetMouseButtonDown(1) && figureController?.GetSelectedFigure() != null)
        {
            figureController.DeselectFigure();
        }
        
        // Dice roll input
        if (Input.GetKeyDown(KeyCode.Space))
        {
            RollDice();
        }

        // Let the figure controller handle path cycling
        figureController.ProcessPathCyclingInput();
        
    }
    
    private void HandleClick()
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        // First check what we hit with any layer mask
        if (Physics.Raycast(ray, out hit, 100f))
        {
            GameObject hitObject = hit.collider.gameObject;
            Player currentPlayer = playerManager?.GetPlayerById(activePlayerId);

            if (turnSystem.CurrentPhase == PlayerTurnSystem.PlayerTurnPhase.Actions)
            {
                // First: check if hit friendly figure (select it)
                Figure hitFigure = hit.collider.GetComponent<Figure>();
                if (hitFigure != null && hitFigure.PlayerId == activePlayerId && figureController != null)
                {
                    figureController.SelectFigure(hitFigure, currentPlayer?.actionPoints ?? 0);
                    figureController.ShowAttackIndicators();
                    return;
                }

                // Second: check if hit attack indicator
                AttackIndicator attackIndicator = hit.collider.GetComponent<AttackIndicator>();
                if (attackIndicator != null && figureController.GetSelectedFigure() != null)
                {
                    // This will handle the attack through the indicator
                    figureController.AttackFromIndicator(attackIndicator, currentPlayer?.actionPoints ?? 0, 
                        ConsumeActionPoints(attackIndicator.cost, () => {
                            figureController.DeselectFigure();
                        }));
                    return;
                }

                // Third: check for path indicator
                PathIndicator pathIndicator = hit.collider.GetComponent<PathIndicator>();
                if (pathIndicator != null && figureController.GetSelectedFigure() != null)
                {
                    // Move the figure and get steps used
                    int stepsUsed = 0;
                    stepsUsed = figureController.MoveToIndicator(
                        pathIndicator, 
                        currentPlayer?.actionPoints ?? 0, 
                        () => {
                            // Need to capture stepsUsed in a local variable for the callback
                            var moveComplete = ConsumeActionPoints(stepsUsed);

                            // Execute the completion action
                            moveComplete();
                            figureController.DeselectFigure();
                        }
                    );
                    return;
                }
            }
        }
    }
    
    /// <summary>
    /// Set the current player ID (called from PlayerManager)
    /// </summary>
    public void SetCurrentPlayer(int playerId)
    {
        activePlayerId = playerId;
        figureController.DeselectFigure();
        
        Debug.Log($"PlayerController now controlling Player {playerId}");
    }
    
    /// <summary>
    /// Roll the dice for the current player
    /// </summary>
    private void RollDice()
    {
        // Only allow rolling dice when it's the roll phase
        if (turnSystem.CurrentPhase == PlayerTurnSystem.PlayerTurnPhase.Roll)
        {
            Player currentPlayer = playerManager?.GetPlayerById(activePlayerId);
            if (currentPlayer == null) return;

            // Generate a random roll (1-6)
            // int steps = Random.Range(1, 7);
            int steps = 6; // For testing purposes, always roll 6
            currentPlayer.actionPoints = steps;

            Debug.Log($"Player {activePlayerId} rolled {steps}");

            // Tell the turn controller to advance to the next phase
            turnSystem.AdvanceToNextPhase();

        }
        else
        {
            Debug.Log("Cannot roll dice now - not in Roll phase");
        }
    }

    // Helper method to consume action points and track animations
    private System.Action ConsumeActionPoints(int points, System.Action onComplete = null)
    {
        Player currentPlayer = playerManager?.GetPlayerById(activePlayerId);
        if (currentPlayer == null) 
        {
            return () => onComplete?.Invoke();
        }

        // Execute the callback when complete
        System.Action completeAction = () => {
            // Flag that we're in an animation
            waitingForAnimation = true;

            // Consume points
            currentPlayer.actionPoints -= points;

            // Log remaining points
            if (currentPlayer.actionPoints > 0)
            {
                Debug.Log($"You have {currentPlayer.actionPoints} action points left.");
            }

            // Animation is done
            waitingForAnimation = false;

            // Call any additional completion logic
            onComplete?.Invoke();
        };

        return completeAction;
    }
    
}