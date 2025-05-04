using UnityEngine;

/// <summary>
/// Base class for all figure abilities
/// </summary>
public abstract class FigureAbility : ScriptableObject
{
    [SerializeField] private string abilityName;
    [SerializeField] private string description;
    [SerializeField] private bool isPassive = true;
    
    protected Figure owner;
    
    public string AbilityName => abilityName;
    public string Description => description;
    public bool IsPassive => isPassive;

    /// <summary>
    /// Initialize the ability with its owner
    /// </summary>
    public virtual void Initialize(Figure owner)
    {
        this.owner = owner;
    }
    
    /// <summary>
    /// Called when the ability is activated (for active abilities)
    /// </summary>
    public virtual bool CanActivate()
    {
        return !isPassive;
    }
    
    /// <summary>
    /// Try to activate the ability
    /// </summary>
    public virtual bool TryActivate(params object[] args)
    {
        if (CanActivate())
        {
            return Activate(args);
        }
        return false;
    }
    
    /// <summary>
    /// Override this to implement active ability behavior
    /// </summary>
    protected virtual bool Activate(params object[] args)
    {
        return false;
    }
    
    /// <summary>
    /// Calculate damage multiplier for this ability (for resistances/vulnerabilities)
    /// </summary>
    public virtual float GetDamageMultiplier(DamageType damageType)
    {
        return 1.0f; // Default: no change to damage
    }
    
    /// <summary>
    /// Called when the figure is selected
    /// </summary>
    public virtual void OnSelect() { }
    
    /// <summary>
    /// Called when the figure is deselected
    /// </summary>
    public virtual void OnDeselect() { }
    
    /// <summary>
    /// Called before movement begins
    /// </summary>
    public virtual void OnBeforeMove() { }
    
    /// <summary>
    /// Called after movement completes
    /// </summary>
    public virtual void OnAfterMove() { }
    
    /// <summary>
    /// Modify the movement rules for this figure
    /// </summary>
    public virtual bool CanMoveToHex(int q, int r, HexTile tile)
    {
        return true; // Default: no restrictions
    }
}