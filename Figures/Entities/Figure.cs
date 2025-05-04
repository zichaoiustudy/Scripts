using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Figure : MonoBehaviour
{
    [Header("Basic Information")]
    [SerializeField] private string figureName = "Default";
    [SerializeField] private float moveSpeed = 5f;
    
    [Header("Stats")]
    [SerializeField] private int maxHealth = 4;
    [SerializeField] private int currentHealth;
    [SerializeField] private int attackPower = 1;
    [SerializeField] private DamageType damageType = DamageType.Physical;
    
    // Reference to figure abilities
    [SerializeField] private List<FigureAbility> abilities = new List<FigureAbility>();

    [Header("UI")]
    [SerializeField] private GameObject healthBarPrefab;
    private HealthBar healthBar;

    private int playerId;
    private int currentQ;
    private int currentR;
    private bool isSelected = false;
    private bool isMoving = false;
    
    // Material to store original color before selection
    private Material figureMaterial;
    private Color originalColor;
    private Color selectedColor = Color.yellow;
    
    public event System.Action<Figure> OnDefeated;

    // Public properties
    public int PlayerId => playerId;
    public string FigureName => figureName;
    public bool IsSelected => isSelected;
    public int CurrentQ => currentQ;
    public int CurrentR => currentR;
    public bool IsMoving => isMoving;
    public int MaxHealth => maxHealth;
    public int CurrentHealth => currentHealth;
    public int AttackPower => attackPower;
    public DamageType DamageType => damageType;
    public IReadOnlyList<FigureAbility> Abilities => abilities;
    
    // Initialization
    public void Initialize(int playerID, string name)
    {
        playerId = playerID;
        figureName = name;
        currentHealth = maxHealth;
        
        // Cache renderer and material
        Renderer renderer = GetComponentInChildren<Renderer>();
        if (renderer != null)
        {
            figureMaterial = renderer.material;
        }
        
        // Initialize all abilities
        foreach (var ability in abilities)
        {
            ability.Initialize(this);
        }

        // Create the health bar
        CreateHealthBar();
    }

    private void CreateHealthBar()
    {
        if (healthBarPrefab != null)
        {
            GameObject healthBarObj = Instantiate(healthBarPrefab, transform.position + Vector3.up * 1.5f, Quaternion.identity);
            healthBarObj.transform.SetParent(transform);

            healthBar = healthBarObj.GetComponent<HealthBar>();
            if (healthBar == null)
            {
                healthBar = healthBarObj.AddComponent<HealthBar>();
            }

            healthBar.Initialize(this);
        }
    }
    
    // Health management
    public virtual void TakeDamage(int amount, DamageType type)
    {
        // Check for resistance or vulnerability based on damage type
        float damageMultiplier = 1.0f;
        foreach (var ability in abilities)
        {
            damageMultiplier *= ability.GetDamageMultiplier(type);
        }
        
        int actualDamage = Mathf.RoundToInt(amount * damageMultiplier);
        currentHealth -= actualDamage;

        // Update health bar
        if (healthBar != null)
        {
            healthBar.UpdateHealth(currentHealth, maxHealth);
        }
        
        Debug.Log($"{figureName} took {actualDamage} {type} damage. Health: {currentHealth}/{maxHealth}");
        
        // Check if figure is defeated
        if (currentHealth <= 0)
        {
            HandleDefeat();
        }
    }
    
    public virtual void Heal(int amount)
    {
        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
        // Update health bar
        if (healthBar != null)
        {
            healthBar.UpdateHealth(currentHealth, maxHealth);
        }

        Debug.Log($"{figureName} healed for {amount}. Health: {currentHealth}/{maxHealth}");
    }
    
    // Then modify the existing OnDefeated method (rename it to avoid confusion)
    protected virtual void HandleDefeat()
    {
        Debug.Log($"{figureName} has been defeated!");
        
        // Store position for capture movement
        int defeatQ = currentQ;
        int defeatR = currentR;
        
        // Invoke event before destroying the GameObject
        OnDefeated?.Invoke(this);
        
        // Allow a short delay before destruction for visual effect
        StartCoroutine(DestroyWithDelay(0.3f));
    }
    
    private IEnumerator DestroyWithDelay(float delay)
    {
        // Hide the figure but don't destroy yet
        var renderers = GetComponentsInChildren<Renderer>();
        foreach (var renderer in renderers)
        {
            renderer.enabled = false;
        }
        
        yield return new WaitForSeconds(delay);
        Destroy(gameObject);
    }

    // Ability management
    public void AddAbility(FigureAbility ability)
    {
        if (ability != null && !abilities.Contains(ability))
        {
            abilities.Add(ability);
            ability.Initialize(this);
        }
    }
    
    public void RemoveAbility(FigureAbility ability)
    {
        if (ability != null && abilities.Contains(ability))
        {
            abilities.Remove(ability);
        }
    }
    
    public bool HasAbility<T>() where T : FigureAbility
    {
        foreach (var ability in abilities)
        {
            if (ability is T)
                return true;
        }
        return false;
    }
    
    public T GetAbility<T>() where T : FigureAbility
    {
        foreach (var ability in abilities)
        {
            if (ability is T)
                return ability as T;
        }
        return null;
    }
    
    // Movement and position
    public virtual void MoveToPosition(Vector3 targetPosition, System.Action onComplete = null)
    {
        // Call pre-move abilities
        foreach (var ability in abilities)
        {
            ability.OnBeforeMove();
        }
        
        StartCoroutine(AnimateMovement(targetPosition, () => {
            // Call post-move abilities
            foreach (var ability in abilities)
            {
                ability.OnAfterMove();
            }
            
            onComplete?.Invoke();
        }));
    }
    
    // Base movement animation (unchanged)
    private IEnumerator AnimateMovement(Vector3 targetPosition, System.Action onComplete)
    {
        isMoving = true;
        Vector3 startPosition = transform.position;
        float journeyTime = Vector3.Distance(startPosition, targetPosition) / moveSpeed;
        float elapsedTime = 0;
        
        // Add a slight arc to the movement
        float maxHeight = 0.5f;
        float baseHeight = targetPosition.y;
        
        while (elapsedTime < journeyTime)
        {
            float t = elapsedTime / journeyTime;
            float smoothT = Mathf.SmoothStep(0, 1, t);
            Vector3 position = Vector3.Lerp(startPosition, targetPosition, smoothT);
            float arcHeight = maxHeight * 4f * (smoothT - smoothT * smoothT);
            position.y = baseHeight + arcHeight;
            transform.position = position;
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        
        transform.position = targetPosition;
        isMoving = false;
        onComplete?.Invoke();
    }

    // Standard Figure functions (unchanged)
    public void SetHexCoordinates(int q, int r)
    {
        currentQ = q;
        currentR = r;
    }

    private void SetMaterialColor(Color color)
    {
        if (figureMaterial != null)
        {
            figureMaterial.color = color;
        }
    }

    public void UpdateOriginalColor(Color color)
    {
        originalColor = color;
        if (!isSelected)
        {
            SetMaterialColor(originalColor);
        }
    }
    
    // Selection
    public virtual void Select()
    {
        isSelected = true;
        SetMaterialColor(selectedColor);
        
        // Call ability hooks
        foreach (var ability in abilities)
        {
            ability.OnSelect();
        }
    }

    public virtual void Deselect()
    {
        isSelected = false;
        SetMaterialColor(originalColor);
        
        // Call ability hooks
        foreach (var ability in abilities)
        {
            ability.OnDeselect();
        }
    }
}