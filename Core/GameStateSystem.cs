using UnityEngine;

/// <summary>
/// Manages the high-level game states and transitions between them.
/// </summary>
public class GameStateSystem : MonoBehaviour
{
    #region Enums
    
    // Game-level state machine
    public enum GameState
    {
        PreGame,        // Setup phase, setting game mode, player order
        PlayerTurns,    // Players taking turns in order
        SystemTurn,     // System turn (monsters, events, etc.)
        Victory,        // Game has ended in victory
        GameOver        // Game has ended
    }
    
    #endregion
    
   #region Variables

// Game state tracking
private GameState currentGameState = GameState.PreGame;
private int roundNumber = 0;

// Properties
public GameState CurrentGameState => currentGameState;
public int RoundNumber => roundNumber;

#endregion

    #region Unity Lifecycle Methods
    
    private void Start()
    {
        // We'll now only subscribe to events in Start, not OnEnable
        SafeSubscribeToEvents();
    }
    
    private void OnDisable()
    {
        UnsubscribeFromEvents();
    }
    
    #endregion
    
    #region Event Handling
    
    private void SafeSubscribeToEvents()
    {
        if (ServiceLocator.Instance?.Events != null)
        {
            Debug.Log("<color=blue>[SYSTEM]</color> GameStateSystem subscribing to events");
            ServiceLocator.Instance.Events.Subscribe<GameResetEvent>(HandleGameReset);
            ServiceLocator.Instance.Events.Subscribe<SystemTurnEndedEvent>(HandleSystemTurnEnded);
        }
        else
        {
            Debug.LogError("GameStateSystem: Cannot subscribe to events - EventSystem not available!");

            // Retry after a short delay
            Invoke("SafeSubscribeToEvents", 0.5f);
        }
    }

    private void UnsubscribeFromEvents()
    {
        if (ServiceLocator.Instance?.Events != null)
        {
            ServiceLocator.Instance.Events.Unsubscribe<GameResetEvent>(HandleGameReset);
            ServiceLocator.Instance.Events.Unsubscribe<SystemTurnEndedEvent>(HandleSystemTurnEnded);
        }
    }

    private void HandleSystemTurnEnded(SystemTurnEndedEvent evt)
    {
        // Increase the round counter
        roundNumber++;

        Debug.Log($"System Turn ended, advancing to round {roundNumber}");

        // Back to player turns
        SetGameState(GameState.PlayerTurns);
    }
    
    private void HandleGameReset(GameResetEvent evt)
    {
        ResetGameState();
    }
    
    #endregion
    
    #region Public Methods
    
    /// <summary>
    /// Start the game after initialization and pregame setup
    /// </summary>
    public void StartGame()
    {
        // Start the first round
        roundNumber = 1;
        Debug.Log($"Game started with Round {roundNumber}");

        // Transition to player turns
        SetGameState(GameState.PlayerTurns);
        
    }
    
    /// <summary>
    /// Change the game state with proper transition handling
    /// </summary>
    public void SetGameState(GameState newState)
    {
        if (currentGameState == newState) return;

        GameState oldState = currentGameState;
        Debug.Log($"<color=yellow>[STATE CHANGE]</color> {oldState} → {newState} (Frame: {Time.frameCount}, Time: {Time.time:F2}s)");
        
        // Handle entry logic for new state
        switch (newState)
        {
            case GameState.PreGame:
                // Reset game state
                roundNumber = 0;
                break;

            case GameState.PlayerTurns:
                if (oldState == GameState.PreGame || oldState == GameState.SystemTurn)
                {
                    // Find PlayerTurnSystem when needed rather than storing a reference
                    PlayerTurnSystem turnSystem = ServiceLocator.Instance?.PlayerTurnSystem;

                    // Start with the first player
                    if (turnSystem != null)
                    {
                        turnSystem.StartFirstPlayerTurn();
                    }
                }
                break;

            case GameState.SystemTurn:
                // Start system turn (monsters, events)
                StartSystemTurn();
                break;

            case GameState.Victory:
                Debug.Log("<color=green>GAME VICTORY!</color> Boss has been defeated!");
                // Display victory UI, play victory music, etc.
                break;

            case GameState.GameOver:
                Debug.Log("<color=red>GAME OVER!</color>");
                // Display game over UI, play failure music, etc.
                break;
        }

        // Update the current state
        currentGameState = newState;

        // Publish game state changed event
        PublishGameStateChanged(oldState, newState);
    }
    
    #endregion
    
    #region Private Methods
    
    private void StartSystemTurn()
    {        
        // Publish system turn started event
        PublishSystemTurnStarted();

    }
    
    // Update ResetGameState to find PlayerTurnSystem when needed
    private void ResetGameState()
    {
        currentGameState = GameState.PreGame;
        roundNumber = 0;
        
        // Find and reset the turn controller
        PlayerTurnSystem turnSystem = ServiceLocator.Instance?.PlayerTurnSystem;
        if (turnSystem != null)
        {
            turnSystem.ResetTurnController();
        }
        
        Debug.Log("Game state reset");
    }
    
    #endregion
    
    #region Event Publishing
    
    private void PublishSystemTurnStarted()
    {
        ServiceLocator.Instance?.Events.Publish(new SystemTurnStartedEvent
        {
            RoundNumber = roundNumber
        });
    }
    
    private void PublishGameStateChanged(GameState oldState, GameState newState)
    {
        ServiceLocator.Instance?.Events.Publish(new GameStateChangedEvent
        {
            OldState = oldState,
            NewState = newState
        });
    }
    
    #endregion
}