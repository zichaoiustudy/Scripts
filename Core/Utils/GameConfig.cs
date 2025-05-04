using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Scriptable object to centralize game configuration settings
/// </summary>
[CreateAssetMenu(fileName = "GameConfig", menuName = "Hex Chess/Configuration")]
public class GameConfig : ScriptableObject
{
    [Header("Players")]
    [Range(2, 8)]
    public int numberOfPlayers = 6;
    [Range(1, 10)]
    public int figuresPerPlayer = 3;
    [Range(1, 6)]
    public int initFiguresPerPlayer = 1;
    public Color[] playerColors = new Color[]
    {
        new Color(0.2f, 0.2f, 0.8f), // Blue
        new Color(0.8f, 0.2f, 0.2f), // Red
        new Color(0.2f, 0.8f, 0.2f), // Green
        new Color(0.8f, 0.8f, 0.2f), // Yellow
        new Color(0.8f, 0.2f, 0.8f), // Purple
        new Color(0.2f, 0.8f, 0.8f)  // Cyan
    };
    
    [Header("Figure Pool")]
    public List<GameObject> figurePrefabs = new List<GameObject>();
    
    [Header("Map Generation")]
    public MapData mapData;
    [Range(2, 7)]
    public int fieldRadius = 6;
}
