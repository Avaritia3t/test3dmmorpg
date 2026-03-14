using UnityEngine;
using UnityEngine.UI;

public class PlayerShieldBar : MonoBehaviour
{
    [SerializeField] private Slider shieldSlider;  // Assign this in the Unity Inspector

    private Camera cam;

    private void Start()
    {
        cam = Camera.main;
    }

    public void InitializeShieldBar(float maxShield)
    {
        shieldSlider.maxValue = maxShield;
        shieldSlider.value = maxShield;  // Set slider to full shield initially
    }

    public void UpdateShieldBar(float currentShield)
    {
        shieldSlider.value = currentShield;  // Update the slider as shield changes
    }

    void Update()
    {
        // Make the shield bar always face towards the camera
        transform.LookAt(transform.position + cam.transform.rotation * Vector3.forward, cam.transform.rotation * Vector3.up);
    }
}
