using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Handles figure combat interactions and attack-related logic
/// </summary>
public class FigureCombatController : MonoBehaviour
{
    #region Fields
    
    // Core references
    private FieldManager fieldManager;
    private FigureManager figureManager;
    private FigureSelectionController selectionController;
    private IndicatorManager indicatorManager;
    
    // Attack state tracking
    private Dictionary<Figure, bool> lastActionWasAttack = new Dictionary<Figure, bool>();
    private Dictionary<Figure, Vector2Int> defeatedFigurePositions = new Dictionary<Figure, Vector2Int>();
    
    #endregion

    #region Initialization
    
    /// <summary>
    /// Initialize the combat controller with required dependencies
    /// </summary>
    public void Initialize(FieldManager fieldManager, FigureManager figureManager, IndicatorManager indicatorManager)
    {
        this.fieldManager = fieldManager;
        this.figureManager = figureManager;
        this.indicatorManager = indicatorManager;
    }
    
    /// <summary>
    /// Set the selection controller reference
    /// </summary>
    public void SetController()
    {
        selectionController = GetComponent<FigureSelectionController>();
        if (selectionController == null)
        {
            Debug.LogError("FigureSelectionController not found on the same GameObject");
        }
    }
    
    #endregion

    #region Attack State Tracking
    
    /// <summary>
    /// Check if a figure can attack (has moved since last attack)
    /// </summary>
    public bool CanAttack(Figure figure)
    {
        if (figure == null) return false;

        // Can't attack if last action was attack
        return !lastActionWasAttack.TryGetValue(figure, out bool wasAttack) || !wasAttack;
    }

    /// <summary>
    /// Mark that a figure has just attacked
    /// </summary>
    public void MarkFigureAttacked(Figure figure)
    {
        if (figure == null) return;
        lastActionWasAttack[figure] = true;
    }

    /// <summary>
    /// Mark that a figure has moved (resetting the attack restriction)
    /// </summary>
    public void MarkFigureMoved(Figure figure)
    {
        if (figure == null) return;
        lastActionWasAttack[figure] = false;
    }

    /// <summary>
    /// Reset all attack tracking for a new turn
    /// </summary>
    public void ResetAttackTracking()
    {
        lastActionWasAttack.Clear();
    }
    
    #endregion

    #region Attack Logic
    
    /// <summary>
    /// Attack from the selected figure to a target figure
    /// </summary>
    /// <returns>True if the attack was successful</returns>
    public bool AttackFigure(Figure targetFigure, int availableActionPoints, System.Action onComplete = null)
    {
        // Keep minimal validation for safety
        Figure selectedFigure = selectionController?.GetSelectedFigure();
        
        if (selectedFigure == null || targetFigure == null || availableActionPoints <= 0)
        {
            Debug.Log("Attack failed: Invalid figures or no action points");
            onComplete?.Invoke();
            return false;
        }
    
        // Clear any indicators since we're executing the attack
        selectionController.ClearAllIndicators();
    
        // Store health to check if defeated after attack
        int initialHealth = targetFigure.CurrentHealth;
        
        // Create a proper event subscription that we can unsubscribe from
        System.Action<Figure> defeatHandler = null;
        defeatHandler = (defeatedFigure) => {
            targetFigure.OnDefeated -= defeatHandler;
            HandleFigureDefeat(defeatedFigure, selectedFigure);
        };
        
        // Subscribe to defeat event
        targetFigure.OnDefeated += defeatHandler;
        
        // Attack animation and logic
        StartCoroutine(PerformAttackAnimation(selectedFigure, targetFigure, () => {
            // Apply damage
            int damage = selectedFigure.AttackPower;
            targetFigure.TakeDamage(damage, selectedFigure.DamageType, selectedFigure);
    
            // Mark as attacked from this position
            MarkFigureAttacked(selectedFigure);
    
            // If the target wasn't defeated, we need to unsubscribe 
            if (targetFigure != null && targetFigure.CurrentHealth > 0)
            {
                targetFigure.OnDefeated -= defeatHandler;
            }
    
            // Wait longer for the completion if we're capturing
            if (initialHealth <= damage)
            {
                StartCoroutine(DelayedComplete(onComplete, 0.6f));
            }
            else
            {
                onComplete?.Invoke();
            }
        }));
    
        return true;
    }

    private IEnumerator DelayedComplete(System.Action onComplete, float delay)
    {
        yield return new WaitForSeconds(delay);
        onComplete?.Invoke();
    }

    /// <summary>
    /// Check if a figure can attack to a hex (no barriers, valid altitude)
    /// </summary>
    private bool CanReachForAttack(Vector2Int from, Vector2Int to)
    {
        // Get the actual hexes
        GameObject fromHex = fieldManager?.GetHex(from.x, from.y);
        GameObject toHex = fieldManager?.GetHex(to.x, to.y);

        if (fromHex == null || toHex == null)
            return false;

        // Check for altitude differences (same as movement restriction)
        int fromAltitude = HexUtils.GetHexAltitude(from.x, from.y);
        int toAltitude = HexUtils.GetHexAltitude(to.x, to.y);

        // Too big altitude difference prevents attack
        if (Mathf.Abs(fromAltitude - toAltitude) > 1)
            return false;

        return true;
    }
    
    /// <summary>
    /// Handle figure defeat and capturing
    /// </summary>
    private void HandleFigureDefeat(Figure defeatedFigure, Figure attacker)
    {
        if (defeatedFigure == null || attacker == null) return;

        // Store the position of the defeated figure
        Vector2Int capturePosition = new Vector2Int(defeatedFigure.CurrentQ, defeatedFigure.CurrentR);
        defeatedFigurePositions[defeatedFigure] = capturePosition;

        // Start capturing process
        StartCoroutine(MoveToCapturedPosition(attacker, capturePosition));
    }

    private IEnumerator MoveToCapturedPosition(Figure attacker, Vector2Int capturePosition)
    {
        // Wait a moment for the defeat animation
        yield return new WaitForSeconds(0.4f);

        // Verify the hex is now vacant (in case of resurrection effects)
        if (figureManager.IsHexOccupied(capturePosition.x, capturePosition.y))
        {
            Debug.Log("Cannot capture position - hex is still occupied");
            yield break;
        }

        // Move the attacker to the captured position
        figureManager.MoveFigure(attacker, capturePosition.x, capturePosition.y);

        // Mark as "moved" after capturing so it can attack again
        // This is important to allow the figure to attack again after capturing
        // MarkFigureMoved(attacker);
    }
    
    #endregion

    #region Animation
    
    /// <summary>
    /// Perform attack animation between two figures
    /// </summary>
    private IEnumerator PerformAttackAnimation(Figure attacker, Figure defender, System.Action onComplete)
    {
        if (attacker == null || defender == null)
        {
            onComplete?.Invoke();
            yield break;
        }

        Vector3 originalPosition = attacker.transform.position;
        Vector3 targetPosition = defender.transform.position;
        Vector3 midPoint = (originalPosition + targetPosition) / 2f;

        // Quick attack animation
        float duration = 0.3f;
        float elapsed = 0;

        // Move toward target
        while (elapsed < duration / 2)
        {
            attacker.transform.position = Vector3.Lerp(originalPosition, midPoint, elapsed / (duration / 2));
            elapsed += Time.deltaTime;
            yield return null;
        }

        // Slight "hit" vibration on the defender
        Vector3 defenderOriginalPosition = defender.transform.position;
        float shakeIntensity = 0.1f;
        for (int i = 0; i < 5; i++)
        {
            defender.transform.position = defenderOriginalPosition + Random.insideUnitSphere * shakeIntensity;
            yield return new WaitForSeconds(0.03f);
        }
        defender.transform.position = defenderOriginalPosition;

        // Move back
        elapsed = 0;
        while (elapsed < duration / 2)
        {
            attacker.transform.position = Vector3.Lerp(midPoint, originalPosition, elapsed / (duration / 2));
            elapsed += Time.deltaTime;
            yield return null;
        }

        // Ensure proper final position
        attacker.transform.position = originalPosition;

        // Invoke completion callback
        onComplete?.Invoke();
    }
    
    #endregion

    #region Attack Indicators
    
    /// <summary>
    /// Get all attackable figures for the selected figure
    /// </summary>
    public List<Figure> GetAttackableTargets()
    {
        List<Figure> targets = new List<Figure>();
        Figure selectedFigure = selectionController?.GetSelectedFigure();

        if (selectedFigure == null || figureManager == null)
            return targets;

        // Get the figure's current position
        Vector2Int position = new Vector2Int(selectedFigure.CurrentQ, selectedFigure.CurrentR);

        // Use a HashSet to avoid duplicate targets (important for bosses)
        HashSet<Figure> uniqueTargets = new HashSet<Figure>();

        // Check all neighboring hexes
        foreach (Vector2Int neighbor in HexUtils.GetHexNeighbors(position.x, position.y))
        {
            // Check if there's an enemy figure (will also find bosses that occupy this hex)
            Figure figureAtHex = figureManager.GetFigureAt(neighbor.x, neighbor.y);

            if (figureAtHex != null && figureAtHex.PlayerId != selectedFigure.PlayerId)
            {
                // Check if we can reach this hex for attack
                if (CanReachForAttack(position, neighbor))
                {
                    // Add to set to avoid duplicates
                    uniqueTargets.Add(figureAtHex);
                }
            }
        }

        // Convert set back to list
        targets.AddRange(uniqueTargets);
        return targets;
    }
    
    /// <summary>
    /// Show attack indicators for all possible targets
    /// </summary>
    public void ShowAttackIndicators()
    {
        if (indicatorManager == null || selectionController == null)
        {
            Debug.LogWarning("Missing required components for showing attack indicators");
            return;
        }
        
        Figure selectedFigure = selectionController.GetSelectedFigure();
        
        // Clear any existing attack indicators first
        indicatorManager.ClearAttackIndicators();
        
        if (selectedFigure == null) return;
    
        // Don't show attack indicators if the figure can't attack (last action was attack)
        if (!CanAttack(selectedFigure))
        {
            Debug.Log($"{selectedFigure.FigureName} cannot attack now - must move first");
            return;
        }
    
        // Get all attackable targets
        List<Figure> targets = GetAttackableTargets();
    
        // Create attack indicators for each target
        foreach (Figure target in targets)
        {
            indicatorManager.CreateAttackIndicator(target.CurrentQ, target.CurrentR, target);
        }
        
        Debug.Log($"Found {targets.Count} attackable targets for {selectedFigure.FigureName}");
    }

    /// <summary>
    /// Called when hovering over an attack indicator
    /// </summary>
    public void OnAttackIndicatorHover(AttackIndicator indicator)
    {
        // You could add visual feedback here if needed
        if (indicator?.targetFigure != null)
        {
            Debug.Log($"Hovering attack indicator for {indicator.targetFigure.FigureName}. Attack cost: 1 action point");
        }
    }
    
    /// <summary>
    /// Called when no longer hovering over an attack indicator
    /// </summary>
    public void OnAttackIndicatorHoverExit(AttackIndicator indicator)
    {
        // Reset any visual feedback added in OnAttackIndicatorHover
    }
    
    /// <summary>
    /// Attack from an indicator click
    /// </summary>
    public bool AttackFromIndicator(AttackIndicator indicator, int availableActionPoints, System.Action onComplete = null)
    {
        if (indicator == null || indicator.targetFigure == null)
        {
            Debug.LogError("Invalid attack indicator or missing target figure");
            onComplete?.Invoke();
            return false;
        }
        
        // Delegate to our existing attack method
        return AttackFigure(indicator.targetFigure, availableActionPoints, onComplete);
    }
    
    #endregion
}