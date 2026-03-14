using UnityEngine;
using UnityEngine.UI;

public class PlayerHealthBar : MonoBehaviour
{
    [SerializeField] private Slider healthSlider;  // Assign this in the Unity Inspector

    private Camera cam;

    private void Start()
    {
        cam = Camera.main;
    }

    public void InitializeHealthBar(float maxHealth)
    {
        healthSlider.maxValue = maxHealth;
        healthSlider.value = maxHealth;  // Set slider to full health initially
    }

    public void UpdateHealthBar(float currentHealth)
    {
        healthSlider.value = currentHealth;  // Update the slider as health changes
    }

    void Update()
    {
        // Make the health bar always face towards the camera
        transform.LookAt(transform.position + cam.transform.rotation * Vector3.forward, cam.transform.rotation * Vector3.up);
    }
}
