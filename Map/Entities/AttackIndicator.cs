using UnityEngine;

/// <summary>
/// Visual indicator for attackable targets
/// </summary>
public class AttackIndicator : MonoBehaviour
{
    // Target figure and coordinates
    public int q;
    public int r;
    public int cost = 1;
    public Figure targetFigure;
    
    // UI elements
    private TextMesh costText;
    private bool isHighlighted = false;

    private void Awake()
    {
        // Get or create the cost text component
        costText = GetComponentInChildren<TextMesh>();
        if (costText == null)
        {
            GameObject textObj = new GameObject("CostText");
            textObj.transform.SetParent(transform, false);
            textObj.transform.localPosition = new Vector3(0, 0.5f, 0);
            textObj.transform.localRotation = Quaternion.Euler(90, 0, 0);
            
            costText = textObj.AddComponent<TextMesh>();
            costText.fontSize = 14;
            costText.alignment = TextAlignment.Center;
            costText.anchor = TextAnchor.MiddleCenter;
            costText.color = Color.white;
        }
        
        // Set cost but hide the text initially
        SetCost(1, false);
    }

    /// <summary>
    /// Set the cost text to show on this indicator
    /// </summary>
    public void SetCost(int cost, bool show = true)
    {
        if (costText != null)
        {
            this.cost = cost;
            costText.text = cost.ToString();
            costText.gameObject.SetActive(show);
        }
    }

    /// <summary>
    /// Set whether this indicator is highlighted
    /// </summary>
    public void SetHighlighted(bool highlight, Material highlightMaterial = null)
    {
        isHighlighted = highlight;
        
        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null && highlightMaterial != null && highlight)
        {
            renderer.material = highlightMaterial;
        }
    }

    private void OnMouseEnter()
    {
        // Show the cost when hovering
        SetCost(cost, true);
        
        FigureController controller = ServiceLocator.Instance?.FigureController;
        if (controller != null)
        {
            controller.OnAttackIndicatorHover(this);
        }
    }

    private void OnMouseExit()
    {
        // Hide the cost when not hovering
        SetCost(cost, false);
        
        FigureController controller = ServiceLocator.Instance?.FigureController;
        if (controller != null)
        {
            controller.OnAttackIndicatorHoverExit(this);
        }
    }
}