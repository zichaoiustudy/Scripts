using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Contains only events that need to be observed globally by multiple unrelated systems
/// </summary>

// Core game events
public struct GameResetEvent { }

// Player management events
public struct PlayerTurnEndedEvent
{
    public int PlayerId;
}

public struct PlayerTurnStartedEvent
{
    public int PlayerId;
}

public struct ActivePlayerChangedEvent
{
    public int PlayerId;
    public Color PlayerColor;
}

// Game state events
public struct GameStateChangedEvent
{
    public GameStateSystem.GameState OldState;
    public GameStateSystem.GameState NewState;
}

public struct SystemTurnStartedEvent
{
    public int RoundNumber;
}

public struct SystemTurnEndedEvent
{
    public int RoundNumber;
}

public struct FigureDefeatedEvent
{
    public Figure DefeatedFigure;
    public Figure AttackerFigure;
    public Vector2Int Position;
}

public struct GameVictoryEvent
{
    public int WinningPlayerId;
    public string DefeatReason;
}
