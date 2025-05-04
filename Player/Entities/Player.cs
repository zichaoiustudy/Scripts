using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Player class to store player information and game state.
/// This is implemented as a regular class rather than MonoBehaviour 
/// since it primarily handles data rather than Unity lifecycle events.
/// </summary>
[System.Serializable]
public class Player
{
    public int playerId;
    public string playerName;
    public Color playerColor;
    
    // Game state
    public int actionPoints; // Current steps the player can use
    public bool isActive; // Is it this player's turn
    public int score;
    
    // Reference to all figures owned by this player
    private List<Figure> playerFigures = new List<Figure>();
    
    /// <summary>
    /// Creates a new Player with the given ID
    /// </summary>
    public Player(int id)
    {
        playerId = id;
        playerName = $"Player {id}";
        playerColor = Color.white; 
        isActive = false;
        actionPoints = 0;
        score = 0;
    }
    
    /// <summary>
    /// Creates a new Player with the given ID and name
    /// </summary>
    public Player(int id, string name, Color color)
    {
        playerId = id;
        playerName = name;
        playerColor = color;
        isActive = false;
        actionPoints = 0;
        score = 0;
    }

    public void ResetTurnState()
    {
        isActive = false;
        actionPoints = 0;
    }
    
    /// <summary>
    /// Add a figure to this player's collection
    /// </summary>
    public void AddFigure(Figure figure)
    {
        if (figure != null && !playerFigures.Contains(figure))
        {
            playerFigures.Add(figure);
        }
    }
    
    /// <summary>
    /// Remove a figure from this player's collection
    /// </summary>
    public void RemoveFigure(Figure figure)
    {
        if (figure != null && playerFigures.Contains(figure))
        {
            playerFigures.Remove(figure);
        }
    }
    
    /// <summary>
    /// Get all figures owned by this player
    /// </summary>
    public List<Figure> GetFigures()
    {
        return new List<Figure>(playerFigures); // Return a copy to prevent external modifications
    }
    
}
