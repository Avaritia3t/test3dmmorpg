using System.Collections.Generic;
using UnityEngine;

public class ItemIconManager : MonoBehaviour
{
    public static ItemIconManager Instance { get; private set; }
    private Dictionary<string, Sprite> itemIcons;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Initialize();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Initialize()
    {
        itemIcons = new Dictionary<string, Sprite>();

        // Load all sprites from the EquipmentIcons folder in the Assets directory
        Sprite[] icons = Resources.LoadAll<Sprite>("EquipmentIcons"); // Ensure the folder is under Resources
        foreach (Sprite icon in icons)
        {
            itemIcons[icon.name.ToLower()] = icon; // Store the icon with a lowercase key
        }
    }

    public Sprite GetIcon(string itemName)
    {
        if (itemIcons.TryGetValue(itemName.ToLower(), out Sprite icon)) // Convert the item name to lowercase when looking it up
        {
            return icon;
        }
        Debug.LogError($"Icon not found for item: {itemName}");
        return null;
    }
}
