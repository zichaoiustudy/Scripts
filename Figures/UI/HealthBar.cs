using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    [SerializeField] private Image fillBar;
    [SerializeField] private float yOffset = 1.5f; // Height above figure
    [SerializeField] private float updateSpeed = 20f; // Speed of health bar updates
    
    private Camera mainCamera;
    private Figure targetFigure;
    private float targetFillAmount = 1f;
    
    public void Initialize(Figure figure)
    {
        targetFigure = figure;
        mainCamera = Camera.main;
        
        // Set initial health
        UpdateHealth(figure.CurrentHealth, figure.MaxHealth);
    }
    
    private void Update()
    {
        if (targetFigure == null)
        {
            Destroy(gameObject);
            return;
        }
        
        // Follow the figure with a Y offset
        transform.position = targetFigure.transform.position + Vector3.up * yOffset;
        
        // Always face the camera
        if (mainCamera != null)
        {
            transform.forward = mainCamera.transform.forward;
        }
        
        // Smooth health bar updates
        if (fillBar.fillAmount != targetFillAmount)
        {
            fillBar.fillAmount = Mathf.Lerp(
                fillBar.fillAmount, 
                targetFillAmount, 
                Time.deltaTime * updateSpeed
            );
        }
    }
    
    public void UpdateHealth(int currentHealth, int maxHealth)
    {
        targetFillAmount = Mathf.Clamp01((float)currentHealth / maxHealth);
    }
}