using UnityEngine;

public class ServiceLocator : MonoBehaviour
{
    // Singleton instance
    private static ServiceLocator instance;
    public static ServiceLocator Instance
    {
        get => instance;
        private set
        {
            if (instance != null && value != null)
            {
                Debug.LogError("ServiceLocator: Attempting to set Instance when it's already set!");
                return;
            }
            instance = value;
        }
    }
    // Core systems
    public EventSystem Events => EventSystem.Instance;
    
    // Service references
    public GameManager GameManager { get; private set; }
    public GameStateSystem GameStateSystem { get; private set; }
    public PlayerTurnSystem PlayerTurnSystem { get; private set; }
    public SystemTurnController SystemTurnController { get; private set; }
    public PlayerManager PlayerManager { get; private set; }
    public PlayerController PlayerController { get; private set; }
    public FigurePool FigurePool { get; private set; }
    public FigureManager FigureManager { get; private set; }
    public BossManager BossManager { get; private set; }
    public FigureController FigureController { get; private set; }
    public FieldManager FieldManager { get; private set; }
    public IndicatorManager IndicatorManager { get; private set; }
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogError($"ServiceLocator: Multiple instances detected! Destroying duplicate on {gameObject.name}");
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        
        // Initialize core systems
        InitializeCoreServices();
    }
    
    public void InitializeCoreServices()
    {
        Debug.Log("ServiceLocator: Initializing core services...");
        // These are always required
        if (EventSystem.Instance == null)
        {
            Debug.LogError("ServiceLocator: Failed to initialize EventSystem!");
            return;
        }
        GameManager = GetComponent<GameManager>();
        GameStateSystem = FindFirstObjectByType<GameStateSystem>();
        
        if (GameManager == null || GameStateSystem == null)
        {
            Debug.LogError("ServiceLocator: GameCore not found!");
            return;
        }
    }
    
    public void InitializeEnvironmentServices()
    {
        Debug.Log("ServiceLocator: Initializing environment services...");
        FieldManager = FindFirstObjectByType<FieldManager>();
        
        // Log warnings for missing services
        if (FieldManager == null) Debug.LogWarning("ServiceLocator: FieldManager not found");
    }
    
    public void InitializeGameplayServices()
    {
        Debug.Log("ServiceLocator: Initializing gameplay services...");
        FigurePool = new FigurePool();
        PlayerTurnSystem = FindFirstObjectByType<PlayerTurnSystem>();
        SystemTurnController = FindFirstObjectByType<SystemTurnController>();
        PlayerManager = FindFirstObjectByType<PlayerManager>();
        PlayerController = FindFirstObjectByType<PlayerController>();
        FigureManager = FindFirstObjectByType<FigureManager>();
        BossManager = FindFirstObjectByType<BossManager>();
        FigureController = FindFirstObjectByType<FigureController>();
        IndicatorManager = FindFirstObjectByType<IndicatorManager>();
        
        // Log warnings for missing services
        if (PlayerTurnSystem == null) Debug.LogWarning("ServiceLocator: PlayerTurnSystem not found");
        if (SystemTurnController == null) Debug.LogWarning("ServiceLocator: SystemTurnController not found");
        if (PlayerManager == null) Debug.LogWarning("ServiceLocator: PlayerManager not found");
        if (PlayerController == null) Debug.LogWarning("ServiceLocator: PlayerController not found");
        if (FigureManager == null) Debug.LogWarning("ServiceLocator: FigureManager not found");
        if (BossManager == null) Debug.LogWarning("ServiceLocator: BossManager not found");
        if (FigureController == null) Debug.LogWarning("ServiceLocator: FigureController not found");
        if (IndicatorManager == null) Debug.LogWarning("ServiceLocator: IndicatorManager not found");
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Events?.ClearAllSubscriptions();
            Instance = null;
        }
    }
}
