using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SystemTurnController : MonoBehaviour
{
    private FigureManager figureManager;
    private BossManager bossManager;
    private FieldManager fieldManager;
    private PlayerManager playerManager;
    private FigureCombatController combatController;
    
    private List<Figure> activeMonsters = new List<Figure>();
    private int currentRound = 0;
    
    public void Initialize()
    {
        figureManager = ServiceLocator.Instance?.FigureManager;
        bossManager = ServiceLocator.Instance?.BossManager;
        fieldManager = ServiceLocator.Instance?.FieldManager;
        playerManager = ServiceLocator.Instance?.PlayerManager;
        
        // Get the FigureCombatController for attack handling
        var figureController = ServiceLocator.Instance?.FigureController;
        if (figureController != null)
        {
            combatController = figureController.GetComponent<FigureCombatController>();
        }
        
        if (figureManager == null || fieldManager == null || playerManager == null || combatController == null)
        {
            Debug.LogError("Required dependencies missing in SystemTurnController!");
        }
        
        // Subscribe to events
        if (ServiceLocator.Instance?.Events != null)
        {
            ServiceLocator.Instance.Events.Subscribe<SystemTurnStartedEvent>(OnSystemTurnStarted);
            ServiceLocator.Instance.Events.Subscribe<FigureDefeatedEvent>(OnFigureDefeated);
        }
    }
    
    private void OnSystemTurnStarted(SystemTurnStartedEvent evt)
    {
        currentRound = evt.RoundNumber;
        StartSystemTurn();
    }
    
    private void OnFigureDefeated(FigureDefeatedEvent evt)
    {
        // Handle monster capture when player defeats a monster
        if (evt.DefeatedFigure != null && evt.AttackerFigure != null)
        {
            // Check if a player figure defeated a monster
            if (evt.DefeatedFigure.PlayerId < 0 && evt.AttackerFigure.PlayerId >= 0)
            {
                figureManager.CaptureMonster(evt.DefeatedFigure, evt.AttackerFigure.PlayerId);
            }
        }
    }
    
    private void StartSystemTurn()
    {
        Debug.Log($"Starting System Turn (Round {currentRound})");

        // First clean up any null references
        activeMonsters.RemoveAll(m => m == null);

        // Spawn new monsters using FigureManager
        if (figureManager != null)
        {
            // Use the consolidated method from FigureManager
            List<Figure> newMonsters = figureManager.SpawnMonsters();
            activeMonsters.AddRange(newMonsters);
        }

        // Process AI for all active monsters
        if (activeMonsters.Count > 0)
        {
            StartCoroutine(ProcessMonsterActions());
        }
        else
        {
            // No monsters to move, end turn immediately
            EndSystemTurn();
        }
    }
    
    private IEnumerator ProcessMonsterActions()
    {
        // Wait a moment for visual clarity
        yield return new WaitForSeconds(0.5f);
        
        foreach (var monster in activeMonsters)
        {
            if (monster == null) continue;
            
            // Find the closest player figure to attack
            Figure target = FindClosestPlayerFigure(monster);
            
            if (target != null)
            {
                // If adjacent, attack
                if (IsAdjacent(monster, target))
                {
                    Debug.Log($"Monster {monster.FigureName} attacking {target.FigureName}");
                    
                    // Simple version - monsters always have at least 1 action point
                    bool attacked = combatController.AttackFigure(target, 1, null);
                    
                    // Wait for attack animation
                    yield return new WaitForSeconds(1.0f);
                }
                // Otherwise move toward target
                else
                {
                    Vector2Int nextPosition = CalculateNextPosition(monster, target);
                    if (nextPosition.x != monster.CurrentQ || nextPosition.y != monster.CurrentR)
                    {
                        Debug.Log($"Monster {monster.FigureName} moving to ({nextPosition.x}, {nextPosition.y})");
                        
                        figureManager.MoveFigure(monster, nextPosition.x, nextPosition.y);
                        
                        // Wait for movement
                        yield return new WaitForSeconds(0.8f);
                    }
                }
            }
            
            // Wait a moment between monsters
            yield return new WaitForSeconds(0.3f);
        }
        
        // All monsters have acted, end the system turn
        EndSystemTurn();
    }
    
    private Figure FindClosestPlayerFigure(Figure monster)
    {
        Figure closest = null;
        float minDistance = float.MaxValue;
        
        foreach (Player player in playerManager.Players)
        {
            foreach (Figure playerFigure in player.playerFigures)
            {
                float distance = HexUtils.HexDistance(
                    new Vector2Int(monster.CurrentQ, monster.CurrentR),
                    new Vector2Int(playerFigure.CurrentQ, playerFigure.CurrentR)
                );
                
                if (distance < minDistance)
                {
                    minDistance = distance;
                    closest = playerFigure;
                }
            }
        }
        
        return closest;
    }
    
    private bool IsAdjacent(Figure a, Figure b)
    {
        return HexUtils.HexDistance(
            new Vector2Int(a.CurrentQ, a.CurrentR),
            new Vector2Int(b.CurrentQ, b.CurrentR)
        ) <= 1;
    }
    
    private Vector2Int CalculateNextPosition(Figure monster, Figure target)
    {
        // Simple A* pathfinding would go here
        // For now, implement a simple greedy approach
        
        Vector2Int monsterPos = new Vector2Int(monster.CurrentQ, monster.CurrentR);
        Vector2Int targetPos = new Vector2Int(target.CurrentQ, target.CurrentR);
        
        // Get all neighbors
        List<Vector2Int> neighbors = HexUtils.GetHexNeighbors(monsterPos.x, monsterPos.y);
        
        Vector2Int bestPos = monsterPos;
        float minDistance = HexUtils.HexDistance(monsterPos, targetPos);
        
        foreach (var pos in neighbors)
        {
            // Skip occupied positions
            if (figureManager.IsHexOccupied(pos.x, pos.y))
                continue;
                
            // Skip invalid positions (e.g., barriers, water)
            if (!IsValidMovementTarget(monsterPos, pos))
                continue;
                
            float distance = HexUtils.HexDistance(pos, targetPos);
            if (distance < minDistance)
            {
                minDistance = distance;
                bestPos = pos;
            }
        }
        
        return bestPos;
    }
    
    private bool IsValidMovementTarget(Vector2Int from, Vector2Int to)
    {
        // Check if a figure can move to this hex (no barriers, valid altitude)
        // This is similar to logic in FigureMovementController
        
        // Default to true for simple version
        return true;
    }
    
    private void EndSystemTurn()
    {
        // Publish event that system turn is over
        if (ServiceLocator.Instance?.Events != null)
        {
            ServiceLocator.Instance.Events.Publish(new SystemTurnEndedEvent());
        }
    }
}