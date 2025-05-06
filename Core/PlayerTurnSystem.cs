using UnityEngine;
using System.Collections.Generic;
using System;

public class PlayerTurnSystem : MonoBehaviour
{
    #region Enums
    
    // Player-level turn phases
    public enum PlayerTurnPhase
    {
        None,               // Initial state
        Prepare,            // Prepare for the turn
        Draw,               // Draw cards phase
        Roll,               // Roll dice phase
        ActivateTreasure,   // Activate treasure cards (if any)
        Actions,            // Use action points (move figures, etc.)
        TreasureCards,      // Play cards from hand (optional)
        EndTurn,            // End of turn processing
        Discard             // Discard down to hand limit
    }
    
    #endregion
    
    #region Variables
    
    // Player management
    private List<Player> players = new List<Player>();
    private int currentPlayerIndex = 0;
    private Player currentPlayer;
    
    // Phase management
    private PlayerTurnPhase currentPhase = PlayerTurnPhase.None;
    private bool isWaitingForInput = false;
    
    // Configuration
    [SerializeField] private bool autoAdvancePhases = true;
    [SerializeField] private float autoAdvanceDelay = 0.5f;

    // References
    private GameStateSystem gameStateSystem;
    private EventSystem eventSystem;
    private PlayerController playerController;
    private FigureManager figureManager;

    // Events
    public delegate void PlayerTurnChangedHandler(Player newActivePlayer);
    public event PlayerTurnChangedHandler OnPlayerTurnChanged;
    public event Action<PlayerTurnPhase> PhaseChanged;
    
    // Properties
    public PlayerTurnPhase CurrentPhase => currentPhase;
    public bool IsWaitingForInput => isWaitingForInput;
    public Player CurrentPlayer => currentPlayer;
    public int CurrentPlayerIndex => currentPlayerIndex;
    
    #endregion

    #region Initialization

    /// <summary>
    /// Initialize the turn system and gather required service references
    /// </summary>
    public void Initialize()
    {        
        if (ServiceLocator.Instance == null)
        {
            Debug.LogError("PlayerTurnSystem: ServiceLocator not available!");
            return;
        }
        
        // Get service references
        gameStateSystem = ServiceLocator.Instance.GameStateSystem;
        eventSystem = ServiceLocator.Instance.Events;
        playerController = ServiceLocator.Instance.PlayerController;
        figureManager = ServiceLocator.Instance.FigureManager;
        
        // Check for missing dependencies
        if (gameStateSystem == null || eventSystem == null || playerController == null || figureManager == null)
        {
            Debug.LogError("PlayerTurnSystem: Required services not found!");
            return;
        }
    }
    
    #endregion

    #region Unity Lifecycle Methods

    private void Update()
    {
        // Simple testing functionality: Press Enter to advance to the next phase
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            // Only allow manual advance if we're waiting for input or in auto-advance phases
            if (currentPlayer != null && (isWaitingForInput || autoAdvancePhases))
            {
                Debug.Log("<color=cyan>[TEST] Manually advancing to next phase</color>");
                AdvanceToNextPhase();
            }
        }
    }

    #endregion
    
    #region Player Turn Management

    public void SetPlayers(List<Player> playerList)
    {
        players = new List<Player>(playerList);
    }
    
    public void StartFirstPlayerTurn()
    {
        // Reset to the first player
        ChangeActivePlayer(0);
        
        Debug.Log($"Starting first player turn: Player {currentPlayer.playerId}");
    }

    public void NextPlayerTurn()
    {
        if (players.Count == 0) return;

        // Calculate next player index
        int nextPlayerIndex = (currentPlayerIndex + 1) % players.Count;
        
        // Set the new active player
        ChangeActivePlayer(nextPlayerIndex);
        
        Debug.Log($"Next turn: Player {currentPlayer.playerId}");
    }

    /// <summary>
    /// Check if the current player is the last one in the turn order
    /// </summary>
    public bool IsLastPlayer()
    {
        return currentPlayerIndex == players.Count - 1;
    }
    
    /// <summary>
    /// Change the active player
    /// </summary>
    private void ChangeActivePlayer(int newPlayerIndex)
    {
        if (players.Count == 0) return;
        if (newPlayerIndex < 0 || newPlayerIndex >= players.Count) return;
        
        // Deactivate current player if one exists
        if (currentPlayer != null)
        {
            currentPlayer.isActive = false;
        }
        
        // Set new player index
        currentPlayerIndex = newPlayerIndex;
        
        // Set new active player
        currentPlayer = players[currentPlayerIndex];
        currentPlayer.isActive = true;
        
        // Update the player controller
        if (playerController != null)
        {
            playerController.SetCurrentPlayer(currentPlayer.playerId);
        }
        
        // Notify listeners about player change
        OnPlayerTurnChanged?.Invoke(currentPlayer);
        
        // Publish global events
        PublishPlayerTurnStarted();
        
        // Start the turn phases for this player
        BeginPlayerTurnPhases();
    }
    
    /// <summary>
    /// Start the phases for the current player
    /// </summary>
    private void BeginPlayerTurnPhases()
    {
        // Start with the first phase
        AdvanceToPhase(PlayerTurnPhase.Prepare);
    }
    
    /// <summary>
    /// End the current player's turn
    /// </summary>
    private void EndCurrentPlayerTurn()
    {
        if (currentPlayer == null) return;
        
        // Publish event for other systems that need to know the turn ended
        if (eventSystem != null)
        {
            eventSystem.Publish(new PlayerTurnEndedEvent
            {
                PlayerId = currentPlayer.playerId
            });
        }
        
        // Clear phase state
        currentPhase = PlayerTurnPhase.None;
        isWaitingForInput = false;
        
        // Handle turn transition
        if (IsLastPlayer())
        {
            // Last player finished - go to system turn
            if (gameStateSystem != null)
            {
                gameStateSystem.SetGameState(GameStateSystem.GameState.SystemTurn);
            }
        }
        else
        {
            // Move to the next player
            NextPlayerTurn();
        }
    }
    
    #endregion
    
    #region Phase Management

    /// <summary>
    /// Force a specific player turn phase
    /// </summary>
    public void AdvanceToPhase(PlayerTurnPhase newPhase)
    {
        if (currentPhase == newPhase) return;
        
        PlayerTurnPhase oldPhase = currentPhase;
        currentPhase = newPhase;
        
        // Handle phase transition
        HandlePhaseTransition(oldPhase, newPhase);
        
        // Notify listeners
        PhaseChanged?.Invoke(newPhase);
    }
    
    /// <summary>
    /// Advance to the next player turn phase
    /// </summary>
    public void AdvanceToNextPhase()
    {
        PlayerTurnPhase nextPhase;
        
        switch (currentPhase)
        {
            case PlayerTurnPhase.None:
                nextPhase = PlayerTurnPhase.Prepare;
                break;
            case PlayerTurnPhase.Prepare:
                nextPhase = PlayerTurnPhase.Draw;
                break;
            case PlayerTurnPhase.Draw:
                nextPhase = PlayerTurnPhase.Roll;
                break;
            case PlayerTurnPhase.Roll:
                nextPhase = PlayerTurnPhase.ActivateTreasure;
                break;
            case PlayerTurnPhase.ActivateTreasure:
                nextPhase = PlayerTurnPhase.Actions;
                break;
            case PlayerTurnPhase.Actions:
                nextPhase = PlayerTurnPhase.TreasureCards;
                break;
            case PlayerTurnPhase.TreasureCards:
                nextPhase = PlayerTurnPhase.EndTurn;
                break;
            case PlayerTurnPhase.EndTurn:
                nextPhase = PlayerTurnPhase.Discard;
                break;
            case PlayerTurnPhase.Discard:
                // When leaving discard, just end the turn directly
                EndCurrentPlayerTurn();
                return;
            default:
                nextPhase = PlayerTurnPhase.None;
                break;
        }
        
        AdvanceToPhase(nextPhase);
    }
    
    /// <summary>
    /// Handle the transition between phases
    /// </summary>
    private void HandlePhaseTransition(PlayerTurnPhase oldPhase, PlayerTurnPhase newPhase)
    {
        Debug.Log($"<color=green>[PHASE CHANGE]</color> {oldPhase} → {newPhase} (Player: {currentPlayer?.playerId})");
        
        // Cancel any pending auto-advances
        CancelInvoke("AdvanceToNextPhase");
        
        // Default to not waiting for input, we'll set it explicitly where needed
        isWaitingForInput = false;
        
        switch (newPhase)
        {
            case PlayerTurnPhase.Prepare:
                Debug.Log($"Player {currentPlayer?.playerId} Prepare Phase");

                // Spawn captured monsters for the current player
                if (currentPlayer != null)
                {
                    figureManager.SpawnCapturedMonsters(currentPlayer);
                }

                // Automatically advance to the next phase after preparation
                if (autoAdvancePhases) Invoke("AdvanceToNextPhase", autoAdvanceDelay);
                break;
        
            case PlayerTurnPhase.Draw:
                Debug.Log($"Player {currentPlayer?.playerId} Draw Phase");
                if (autoAdvancePhases) Invoke("AdvanceToNextPhase", autoAdvanceDelay);
                break;
                
            case PlayerTurnPhase.Roll:
                Debug.Log($"Player {currentPlayer?.playerId} Roll Phase");
                isWaitingForInput = true;
                break;
                
            case PlayerTurnPhase.ActivateTreasure:
                Debug.Log($"Player {currentPlayer?.playerId} Activate Treasure Phase");
                if (autoAdvancePhases) Invoke("AdvanceToNextPhase", autoAdvanceDelay);
                break;
                
            case PlayerTurnPhase.Actions:
                Debug.Log($"Player {currentPlayer?.playerId} Actions Phase");
                isWaitingForInput = true;
                break;
                
            case PlayerTurnPhase.TreasureCards:
                Debug.Log($"Player {currentPlayer?.playerId} Play Treasure Card Phase");
                if (autoAdvancePhases) Invoke("AdvanceToNextPhase", autoAdvanceDelay);
                break;
                
            case PlayerTurnPhase.EndTurn:
                Debug.Log($"Player {currentPlayer?.playerId} End Turn Phase");
                if (autoAdvancePhases) Invoke("AdvanceToNextPhase", autoAdvanceDelay);
                break;
                
            case PlayerTurnPhase.Discard:
                Debug.Log($"Player {currentPlayer?.playerId} Discard Phase");
                if (autoAdvancePhases) Invoke("AdvanceToNextPhase", autoAdvanceDelay);
                break;
        }
    }
    
    #endregion
    
    #region Utility Methods

    /// <summary>
    /// Set the waiting for input flag
    /// </summary>
    public void WaitForPlayerInput(bool waiting)
    {
        isWaitingForInput = waiting;
    }
    
    /// <summary>
    /// Reset the turn controller state
    /// </summary>
    public void ResetTurnController()
    {
        CancelInvoke(); // Cancel any pending auto-advances
        
        // Reset state
        currentPlayer = null;
        currentPhase = PlayerTurnPhase.None;
        isWaitingForInput = false;
        currentPlayerIndex = 0;
        
        Debug.Log("Turn controller reset");
    }
    
    #endregion
    
    #region Event Publishing
    
    private void PublishPlayerTurnStarted()
    {
        if (currentPlayer == null || eventSystem == null) return;

        eventSystem.Publish(new PlayerTurnStartedEvent
        {
            PlayerId = currentPlayer.playerId
        });

        eventSystem.Publish(new ActivePlayerChangedEvent
        {
            PlayerId = currentPlayer.playerId,
            PlayerColor = currentPlayer.playerColor
        });
    }
    
    #endregion
}