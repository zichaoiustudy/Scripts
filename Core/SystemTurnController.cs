using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Controls the system turn phase where monsters spawn and attack
/// </summary>
public class SystemTurnController : MonoBehaviour
{
    #region Dependencies
    
    private FigureManager figureManager;
    private FieldManager fieldManager;
    private PlayerManager playerManager;
    
    #endregion
    
    #region State Tracking
    
    private List<Figure> activeBosses = new List<Figure>();
    private List<Figure> activeMonsters = new List<Figure>();
    // Track when each monster spawn location became empty
    private Dictionary<Vector2Int, float> spawnLocationEmptyTimes = new Dictionary<Vector2Int, float>();
    private List<Figure> newlySpawnedMonsters = new List<Figure>();
    private int currentRound = 0;
    
    #endregion
    
    #region Initialization
    
    public void Initialize()
    {
        // Get required dependencies
        figureManager = ServiceLocator.Instance?.FigureManager;
        fieldManager = ServiceLocator.Instance?.FieldManager;
        playerManager = ServiceLocator.Instance?.PlayerManager;
        
        // Validate dependencies
        if (figureManager == null || fieldManager == null || playerManager == null)
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
    
    #endregion
    
    #region Event Handlers
    
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
            if (evt.DefeatedFigure.PlayerId == -1 && evt.AttackerFigure.PlayerId >= 0)
            {
                // Handle monster capture
                figureManager.CaptureMonster(evt.DefeatedFigure, evt.AttackerFigure.PlayerId);

                // Record when this spawn location became empty
                Vector2Int defeatPos = evt.Position;
                spawnLocationEmptyTimes[defeatPos] = Time.time;
                Debug.Log($"Recorded monster defeat at spawn location {defeatPos} at time {Time.time}");
                
            }
        }
    }
    
    #endregion
    
    #region System Turn Logic
    
    /// <summary>
    /// Start system turn sequence: spawn monsters, then process attacks
    /// </summary>
    private void StartSystemTurn()
    {
        Debug.Log($"Starting System Turn (Round {currentRound})");

        // First clean up any null references
        activeMonsters.RemoveAll(m => m == null);
        activeBosses.RemoveAll(b => b == null);

        // Identify all system figures (existing monsters and bosses)
        CollectSystemFigures();
        
        // Spawn new monsters at unoccupied spawn locations
        SpawnNewMonsters();

        // Process attacks by all system figures
        StartCoroutine(ProcessSystemFigureAttacks());
    }
    
    /// <summary>
    /// Collect all existing system figures (monsters and bosses)
    /// </summary>
    private void CollectSystemFigures()
    {
        foreach (Figure figure in figureManager.GetAllSystemFigures())
        {
            // Bosses have PlayerId = -2
            if (figure.PlayerId == -2)
            {
                if (!activeBosses.Contains(figure))
                {
                    activeBosses.Add(figure);
                }
            }
            // Regular monsters have PlayerId = -1
            else if (figure.PlayerId == -1)
            {
                if (!activeMonsters.Contains(figure))
                {
                    activeMonsters.Add(figure);
                }
            }
        }
        
        Debug.Log($"System turn: Found {activeBosses.Count} active bosses and {activeMonsters.Count} active monsters");
    }
    
    /// <summary>
    /// Spawn new monsters at unoccupied spawn locations, prioritizing oldest empty locations
    /// </summary>
    private void SpawnNewMonsters()
    {
        if (figureManager == null)
            return;

        // Clear the list of newly spawned monsters from previous turn
        newlySpawnedMonsters.Clear();

        // Create a sorted list of spawn locations by empty time
        List<Vector2Int> prioritySpawnLocations = GetPrioritySpawnLocations();

        // Spawn monsters using the priority list and config setting
        List<Figure> newMonsters = figureManager.SpawnMonsters(prioritySpawnLocations);

        if (newMonsters.Count > 0)
        {
            Debug.Log($"Spawned {newMonsters.Count} new monsters");
            activeMonsters.AddRange(newMonsters);

            // Track which monsters are newly spawned this turn
            newlySpawnedMonsters.AddRange(newMonsters);

            // Clear timestamps for positions that now have monsters
            foreach (Figure monster in newMonsters)
            {
                Vector2Int pos = new Vector2Int(monster.CurrentQ, monster.CurrentR);
                spawnLocationEmptyTimes.Remove(pos);
            }
        }
        else
        {
            Debug.Log("No new monsters spawned this turn");
        }
    }

    /// <summary>
    /// Get spawn locations ordered by how long they've been empty
    /// </summary>
    private List<Vector2Int> GetPrioritySpawnLocations()
    {
        // Get all monster spawn locations
        List<Vector2Int> allSpawnLocations = figureManager.GetMonsterSpawnLocations();

        // Find unoccupied spawn locations
        List<Vector2Int> emptySpawnLocations = new List<Vector2Int>();
        foreach (Vector2Int spawnPos in allSpawnLocations)
        {
            if (!figureManager.IsHexOccupied(spawnPos.x, spawnPos.y))
            {
                emptySpawnLocations.Add(spawnPos);

                // If we haven't recorded when this location became empty, record it now
                // (This handles locations that were empty from the start)
                if (!spawnLocationEmptyTimes.ContainsKey(spawnPos))
                {
                    spawnLocationEmptyTimes[spawnPos] = 0f; // Priority to initial empty spots
                }
            }
        }

        // Create a sorted list of spawn locations by empty time
        List<(Vector2Int pos, float emptyTime)> sortedLocations = new List<(Vector2Int, float)>();

        foreach (Vector2Int pos in emptySpawnLocations)
        {
            float emptyTime = spawnLocationEmptyTimes.ContainsKey(pos) ? 
                spawnLocationEmptyTimes[pos] : float.MaxValue;

            sortedLocations.Add((pos, emptyTime));
        }

        // Sort by empty time (oldest first)
        sortedLocations.Sort((a, b) => a.emptyTime.CompareTo(b.emptyTime));

        // Extract just the positions in priority order
        return sortedLocations.Select(item => item.pos).ToList();
    }
    
    /// <summary>
    /// Process attacks for all system figures (monsters and bosses)
    /// </summary>
    private IEnumerator ProcessSystemFigureAttacks()
    {
        // Small delay before attacks begin for visual clarity
        yield return new WaitForSeconds(0.5f);

        // First process monster attacks - ONLY for monsters that weren't just spawned
        foreach (Figure monster in activeMonsters)
        {
            if (monster == null) continue;

            // Skip newly spawned monsters - they don't attack on their spawn turn
            if (newlySpawnedMonsters.Contains(monster))
            {
                Debug.Log($"Monster {monster.FigureName} was just spawned and will not attack this turn");
                continue;
            }

            yield return StartCoroutine(ProcessMonsterAttack(monster));

            // Small delay between monster attacks
            yield return new WaitForSeconds(0.3f);
        }

        // Then process boss attacks
        foreach (Figure boss in activeBosses)
        {
            if (boss == null) continue;

            yield return StartCoroutine(ProcessBossAttack(boss));

            // Delay between boss actions
            yield return new WaitForSeconds(0.5f);
        }

        // All system figures have acted, end the system turn
        EndSystemTurn();
    }
    
    /// <summary>
    /// Handle attack logic for a boss figure
    /// </summary>
    private IEnumerator ProcessBossAttack(Figure boss)
    {
        // Find all attackable targets for the boss
        List<Figure> attackableTargets = GetAttackableTargetsForSystemFigure(boss);
        
        if (attackableTargets.Count > 0)
        {
            Debug.Log($"Boss {boss.FigureName} found {attackableTargets.Count} attackable targets");
            
            // Bosses can attack all targets in range (special ability)
            foreach (Figure target in attackableTargets)
            {
                // Special boss attack with increased damage
                bool attacked = AttackWithSystemFigure(boss, target, true);
                
                if (attacked)
                {
                    // Wait for attack animation
                    yield return new WaitForSeconds(1.0f);
                }
            }
        }
        else
        {
            Debug.Log($"Boss {boss.FigureName} has no targets in range");
        }
    }
    
    /// <summary>
    /// Handle attack logic for a monster figure
    /// </summary>
    private IEnumerator ProcessMonsterAttack(Figure monster)
    {
        // Find all attackable targets for this monster
        List<Figure> attackableTargets = GetAttackableTargetsForSystemFigure(monster);
        
        if (attackableTargets.Count > 0)
        {
            // For regular monsters, just attack the weakest target
            Figure targetToAttack = FindWeakestTarget(attackableTargets);
            Debug.Log($"Monster {monster.FigureName} attacking {targetToAttack.FigureName}");
            
            bool attacked = AttackWithSystemFigure(monster, targetToAttack, false);
            
            if (attacked)
            {
                // Wait for attack animation
                yield return new WaitForSeconds(0.8f);
            }
        }
        else
        {
            Debug.Log($"Monster {monster.FigureName} has no targets in range");
        }
    }
    
    /// <summary>
    /// Find all player figures that can be attacked by a system figure
    /// </summary>
    private List<Figure> GetAttackableTargetsForSystemFigure(Figure systemFigure)
    {
        List<Figure> targets = new List<Figure>();
        
        if (systemFigure == null) return targets;
        
        // For multi-hex figures (like bosses), we need to check from all occupied positions
        List<Vector2Int> attackOrigins = GetAttackOrigins(systemFigure);
        
        // Get all player figures
        List<Figure> allPlayerFigures = new List<Figure>();
        foreach (Player player in playerManager.Players)
        {
            allPlayerFigures.AddRange(player.playerFigures);
        }
        
        // Check which player figures are in range from any attack origin
        foreach (Figure playerFigure in allPlayerFigures)
        {
            if (playerFigure == null) continue;
            
            Vector2Int targetPos = new Vector2Int(playerFigure.CurrentQ, playerFigure.CurrentR);
            
            foreach (Vector2Int origin in attackOrigins)
            {
                // Check if in range 1 (adjacent)
                if (HexUtils.HexDistance(origin, targetPos) <= 1)
                {
                    // Also verify nothing blocking attack (like altitude difference)
                    if (CanAttackToPosition(origin, targetPos))
                    {
                        targets.Add(playerFigure);
                        break; // No need to check other origins for this target
                    }
                }
            }
        }
        
        return targets;
    }
    
    /// <summary>
    /// Get all positions a figure can attack from (accounts for multi-hex bosses)
    /// </summary>
    private List<Vector2Int> GetAttackOrigins(Figure figure)
    {
        List<Vector2Int> origins = new List<Vector2Int>
        {
            // Start with the figure's main position
            new Vector2Int(figure.CurrentQ, figure.CurrentR)
        };
        
        // For bosses, add all occupied hexes
        if (figure.PlayerId == -2)
        {
            MultiHexOccupier occupier = figure.GetComponent<MultiHexOccupier>();
            if (occupier != null)
            {
                origins.AddRange(occupier.GetOccupiedPositions());
            }
        }
        
        return origins;
    }
    
    /// <summary>
    /// Check if attack from one position to another is possible
    /// </summary>
    private bool CanAttackToPosition(Vector2Int from, Vector2Int to)
    {
        // Get the actual hexes
        GameObject fromHex = fieldManager?.GetHex(from.x, from.y);
        GameObject toHex = fieldManager?.GetHex(to.x, to.y);

        if (fromHex == null || toHex == null)
            return false;

        // Check for altitude differences (same as movement restriction)
        HexTile fromHexTile = fromHex.GetComponent<HexTile>();
        HexTile toHexTile = toHex.GetComponent<HexTile>();
        
        if (fromHexTile == null || toHexTile == null)
            return false;

        // Too big altitude difference prevents attack
        if (Mathf.Abs(fromHexTile.height - toHexTile.height) > 1)
            return false;

        // Check for barriers (can be extended later)
        
        return true;
    }
    
    /// <summary>
    /// Find the weakest player figure from a list of targets
    /// </summary>
    private Figure FindWeakestTarget(List<Figure> targets)
    {
        Figure weakest = targets[0];
        int lowestHealth = weakest.CurrentHealth;
        
        foreach (Figure target in targets)
        {
            if (target.CurrentHealth < lowestHealth)
            {
                weakest = target;
                lowestHealth = target.CurrentHealth;
            }
        }
        
        return weakest;
    }
    
    /// <summary>
    /// Execute attack with a system figure
    /// </summary>
    private bool AttackWithSystemFigure(Figure attacker, Figure target, bool isBoss)
    {
        if (attacker == null || target == null)
            return false;
            
        // Validate attack
        if (target.PlayerId < 0)
        {
            Debug.LogWarning("System figures cannot attack other system figures");
            return false;
        }
        
        // Calculate attack damage - can be customized for different monster types later
        int damage = attacker.AttackPower;
        
        // For bosses, can apply special rules
        if (isBoss)
        {
            // Example: Bosses deal extra damage
            damage += 1;
        }
        
        // Apply damage directly (bypass combatController for system figures)
        target.TakeDamage(damage, attacker.DamageType, attacker);
        
        Debug.Log($"{attacker.FigureName} attacked {target.FigureName} for {damage} damage");
        return true;
    }
    
    /// <summary>
    /// End the system turn
    /// </summary>
    private void EndSystemTurn()
    {
        Debug.Log("All system figures have acted, ending system turn");
        // Clear the newly spawned monsters list 
        newlySpawnedMonsters.Clear();
        
        // Publish event that system turn is over
        if (ServiceLocator.Instance?.Events != null)
        {
            ServiceLocator.Instance.Events.Publish(new SystemTurnEndedEvent());
        }
    }
    
    #endregion
}