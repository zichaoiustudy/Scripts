using UnityEngine;

public class PathIndicator : MonoBehaviour
{    
    public int q;
    public int r;
    
    private Renderer objectRenderer;
    private Material originalMaterial;
    private TextMesh stepNumberText;
    
    private void Start()
    {
        objectRenderer = GetComponent<Renderer>();
        if (objectRenderer != null)
        {
            originalMaterial = objectRenderer.material;
        }
        CreateStepNumberText();
    }
    
    private void Update()
    {
        // Make text face the camera
        if (stepNumberText != null && Camera.main != null)
        {
            stepNumberText.transform.rotation = Quaternion.LookRotation(
                stepNumberText.transform.position - Camera.main.transform.position
            );
        }
    }
    
    private void OnMouseEnter()
    {        
        // Find the PlayerController to notify about the hover
        FigureController figureController = FindAnyObjectByType<FigureController>();
        if (figureController != null)
        {
            figureController.OnIndicatorHover(this);
        }
    }
    
    private void OnMouseExit()
    {
        // Find the PlayerController to notify about the hover end
        FigureController figureController = FindAnyObjectByType<FigureController>();
        if (figureController != null)
        {
            figureController.OnIndicatorHoverExit(this);
        }
    }
    
    // Method to change the indicator's material color
    public void SetHighlighted(bool highlighted, Material highlightMaterial = null)
    {
        if (objectRenderer != null)
        {
            if (highlighted && highlightMaterial != null)
            {
                objectRenderer.material = highlightMaterial;
            }
            else
            {
                objectRenderer.material = originalMaterial;
            }
        }
    }

    // Create a TextMesh for displaying step numbers
    private void CreateStepNumberText()
    {
        GameObject textObj = new GameObject("StepNumberText");
        textObj.transform.SetParent(transform);
        textObj.transform.localPosition = new Vector3(0, 0.5f, 0); // Position above the indicator
        
        stepNumberText = textObj.AddComponent<TextMesh>();
        stepNumberText.alignment = TextAlignment.Center;
        stepNumberText.anchor = TextAnchor.MiddleCenter;
        stepNumberText.fontSize = 36;
        stepNumberText.characterSize = 0.1f;
        stepNumberText.color = Color.black;
        
        // Make text invisible by default
        SetStepNumber(0, false);
    }
    
    // Set the step number to display
    public void SetStepNumber(int stepNumber, bool visible = true)
    {
        if (stepNumberText == null) return;
        
        stepNumberText.text = stepNumber.ToString();
        stepNumberText.gameObject.SetActive(visible);
    }
}