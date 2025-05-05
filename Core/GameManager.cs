using Unity.VisualScripting;
using UnityEngine;

[RequireComponent(typeof(ServiceLocator))]
public class GameManager : MonoBehaviour
{
    [SerializeField] private GameConfig gameConfig;
    [SerializeField] private GameObject gameplayPrefab;
    [SerializeField] private GameObject figureControllerPrefab;
    
    // Track initialization state of different game components
    private bool isEnvironmentInitialized = false;
    private bool isGameplayInitialized = false;
    
    private void Start()
    {
        // Only initialize the environment at startup
        InitializeEnvironment();
    }

    private void Update()
    {
        // For testing purposes, cycle game state on key press
        if ((Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)) && Input.GetKey(KeyCode.LeftControl))
        {
            StartGameplay();
        }
    }

    /// <summary>
    /// Initialize the game environment (map, field, visual indicators)
    /// </summary>
    public void InitializeEnvironment()
    {
        if (gameConfig == null)
        {
            Debug.LogError("GameConfig is not assigned! Please assign a GameConfig asset to the GameManager.");
            return;
        }

        Debug.Log("<color=blue>[INITIALIZATION]</color> Setting up game environment...");
        
        // Initialize environment services
        ServiceLocator.Instance.InitializeEnvironmentServices();
        var fieldManager = ServiceLocator.Instance.FieldManager;
        if (fieldManager != null)
        {
            fieldManager.GenerateField();
        }
        else
        {
            Debug.LogError("FieldManager not found!");
            return;
        }
        
        isEnvironmentInitialized = true;
        Debug.Log("<color=blue>[INITIALIZATION]</color> Game environment initialized successfully.");
        
    }
    
    /// <summary>
    /// Initialize gameplay components (players, figures, turn system)
    /// </summary>
    public void InitializeGameplay()
    {
        if (!isEnvironmentInitialized)
        {
            Debug.LogError("Cannot initialize gameplay - environment not initialized!");
            return;
        }
        
        Debug.Log("<color=blue>[INITIALIZATION]</color> Setting up gameplay components...");

        // 1. Instantiate the gameplay prefab
        Instantiate(gameplayPrefab);

        // 2. Instantiate Figure Controller system first
        if (figureControllerPrefab != null)
        {
            GameObject figureControllerObj = Instantiate(figureControllerPrefab);
            figureControllerObj.name = "Figure Controller";
            figureControllerObj.SetActive(true); // Ensure it's active for coroutines
        }
        else
        {
            Debug.LogError("Figure Controller prefab not assigned!");
        }

        // 3. Initialize gameplay services - will find the controllers via FindFirstObjectByType
        ServiceLocator.Instance.InitializeGameplayServices();
    
        // 4. Get required services
        var playerManager = ServiceLocator.Instance.PlayerManager;
        var playerController = ServiceLocator.Instance.PlayerController;
        var figurePool = ServiceLocator.Instance.FigurePool;
        var figureManager = ServiceLocator.Instance.FigureManager;
        var bossManager = ServiceLocator.Instance.BossManager;
        var figureController = ServiceLocator.Instance.FigureController;
        var indicatorManager = ServiceLocator.Instance.IndicatorManager;
        var playerTurnSystem = ServiceLocator.Instance.PlayerTurnSystem;
        var systemTurnController = ServiceLocator.Instance.SystemTurnController;
        
        if (playerManager == null || 
            playerController == null || 
            figurePool == null || 
            figureManager == null || 
            bossManager == null ||
            systemTurnController == null ||
            figureController == null ||
            indicatorManager == null ||
            playerTurnSystem == null)
        {
            Debug.LogError("Required gameplay services not found!");
            return;
        }
        
        // 5. Initialize figure pool from game config
        figurePool.Initialize(gameConfig);
        playerManager.Initialize();
        playerController.Initialize();
        playerTurnSystem.Initialize();
        systemTurnController.Initialize();
        figureManager.Initialize();
        bossManager.Initialize();
        figureController.Initialize();
        indicatorManager.Initialize();

        playerManager.InitializePlayers();
        bossManager.SpawnBoss("Default");
        
        isGameplayInitialized = true;
        Debug.Log("<color=blue>[INITIALIZATION]</color> Gameplay components initialized successfully.");
    }
    
    /// <summary>
    /// Start the actual gameplay (turns, etc.) once all components are ready
    /// </summary>
    public void StartGameplay()
    {
        if (!isEnvironmentInitialized || !isGameplayInitialized)
        {
            if (!isEnvironmentInitialized) InitializeEnvironment();
            if (!isGameplayInitialized) InitializeGameplay();
            
            // Return if any initialization failed
            if (!isEnvironmentInitialized || !isGameplayInitialized) return;
        }
        
        Debug.Log("<color=blue>[GAMEPLAY]</color> Starting game...");
        
        // Get the game state system
        var gameStateSystem = ServiceLocator.Instance.GameStateSystem;
        if (gameStateSystem == null)
        {
            Debug.LogError("GameStateSystem not found!");
            return;
        }
        
        gameStateSystem.StartGame();
        
        Debug.Log("<color=blue>[GAMEPLAY]</color> Game started successfully.");
    }
    
    /// <summary>
    /// Reset the entire game state
    /// </summary>
    public void ResetGame()
    {
        Debug.Log("<color=blue>[RESET]</color> Resetting game...");
        
        // Reset state flags
        isEnvironmentInitialized = false;
        isGameplayInitialized = false;
        
        // Publish reset event
        if (ServiceLocator.Instance?.Events != null)
        {
            ServiceLocator.Instance.Events.Publish(new GameResetEvent());
        }
        
        // Reinitialize components
        InitializeEnvironment();
    }
    
    public GameConfig GetGameConfig()
    {
        return gameConfig;
    }
    
}