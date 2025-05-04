using System.Collections.Generic;
using UnityEngine;

public class PlayerManager : MonoBehaviour
{
    private PlayerTurnSystem turnSystem;
    private FigureManager figureManager;
    private FigurePool figurePool;
    private GameConfig gameConfig;
    private List<Player> players = new List<Player>();

    // Properties
    public List<Player> Players => players;

    // Method to directly spawn figures for all players
    public void SpawnPlayerFigures()
    {
        if (figureManager == null) return;
        
        // Create a dictionary of player IDs to colors
        Dictionary<int, Color> colorMap = new Dictionary<int, Color>();
        foreach (Player player in players)
        {
            colorMap[player.playerId] = player.playerColor;
        }
        
        // Call the figure manager with the color map
        figureManager.SpawnPlayerFigures(colorMap);
    }
    
    /// <summary>
    /// Initialize all players for the game
    /// </summary>
    public void InitializePlayers()
    {
        if (ServiceLocator.Instance != null)
        {
            turnSystem = ServiceLocator.Instance.PlayerTurnSystem;
            figureManager = ServiceLocator.Instance.FigureManager;
            gameConfig = ServiceLocator.Instance.GameManager.GetGameConfig();
            figurePool = ServiceLocator.Instance.FigurePool;
        }
        else
        {
            Debug.LogError("ServiceLocator not found!");
        }

        if (turnSystem == null || figureManager == null || figurePool == null)
        {
            Debug.LogError("Required components not found during PlayerManager initialization!");
            return;
        }

        players.Clear();
        figurePool.ResetAssignments();

        // Define default colors - already covering our max of 8 players
        Color[] defaultColors = new Color[] 
        {
            new Color(0.2f, 0.2f, 0.8f), // Blue
            new Color(0.8f, 0.2f, 0.2f), // Red
            new Color(0.2f, 0.8f, 0.2f), // Green
            new Color(0.8f, 0.8f, 0.2f), // Yellow
            new Color(0.8f, 0.2f, 0.8f), // Purple
            new Color(0.2f, 0.8f, 0.8f), // Cyan
            new Color(0.8f, 0.5f, 0.2f), // Orange
            new Color(0.5f, 0.2f, 0.8f), // Magenta
        };

        // Use config values if available, otherwise use defaults
        int numberOfPlayers = gameConfig.numberOfPlayers; // bounded 2-8
        int initFiguresPerPlayer = Mathf.Min(gameConfig.initFiguresPerPlayer, gameConfig.figuresPerPlayer);
        
        Color[] playerColors = (gameConfig.playerColors != null && gameConfig.playerColors.Length >= numberOfPlayers) 
            ? gameConfig.playerColors 
            : defaultColors;
    
        // Create players
        for (int i = 1; i <= numberOfPlayers; i++)
        {
            Color playerColor = playerColors[i - 1];
    
            // Create player with the color
            Player player = new Player(i, $"Player {i}", playerColor);
            
            // Randomly assign figures to this player
            // figurePool.RandomlyAssignFiguresToPlayer(i, initFiguresPerPlayer);
            string[] figureNames = new string[initFiguresPerPlayer];
            for (int j = 0; j < initFiguresPerPlayer; j++)
            {
                figureNames[j] = "Default";
            }
            figurePool.AssignDuplicateFiguresToPlayerByName(i, figureNames);

            players.Add(player);
        }

        turnSystem.SetPlayers(players);
        SpawnPlayerFigures();

    }

    /// <summary>
    /// Get player by ID
    /// </summary>
    public Player GetPlayerById(int playerId)
    {
        return players.Find(p => p.playerId == playerId);
    }

}