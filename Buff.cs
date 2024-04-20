using UnityEngine;

[System.Serializable]
public class Buff
{
    public enum BuffType
    {
        MoveSpeed,
        Damage,
        Health,
        Shields,
        ResourceGeneration,
        CaptureSpeed
        // adding more as needed
    }
    public string ID;
    public string Name;
    public BuffType Type;
    public float ModifierValue; // The value used in the calculation of the application
    public string TargetValue; // Could be "MoveSpeed", "HP", "Shields", etc.
    public bool IsFaction;
    public bool AffectsPlayer;
    public bool AffectsNPC;
    public float Duration; // Duration in seconds, 0 for instant application
    public bool PermanentOnMap; // True if the buff lasts as long as the player is on the map

    // Constructor
    public Buff(string id, string name, BuffType type, float modifierValue, string targetValue, bool isFaction,
                bool affectsPlayer, bool affectsNPC, float duration, bool permanentOnMap)
    {
        ID = id;
        Name = name;
        Type = type;
        ModifierValue = modifierValue;
        TargetValue = targetValue;
        IsFaction = isFaction;
        AffectsPlayer = affectsPlayer;
        AffectsNPC = affectsNPC;
        Duration = duration;
        PermanentOnMap = permanentOnMap;
    }

    // You will need to expand this with your game's logic to handle each target value
    public void Apply(GameObject target)
    {
        DomainController controller = target.GetComponent<DomainController>();
        if (controller != null && AffectsPlayer)
        {
            // Apply buff based on type
            switch (Type)
            {
                case BuffType.MoveSpeed:
                    controller.moveSpeed *= ModifierValue;
                    break;
                    // Extend with other cases as necessary
            }

            // Log for debugging
            Debug.Log($"Applied {Name} to {target.name}: MoveSpeed now {controller.moveSpeed}");

            if (!PermanentOnMap)
            {
                // Implement logic for temporary buffs if necessary
            }
        }
    }

}
