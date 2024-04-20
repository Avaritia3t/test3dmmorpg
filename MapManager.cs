using System.Collections.Generic;
using UnityEngine;
using System;
using static Buff;

public class MapManager : MonoBehaviour
{
    public static MapManager Instance;

    [SerializeField]
    private List<MapData> allMapsData; // Assign in the editor
    private Dictionary<string, MapData> mapsDictionary = new Dictionary<string, MapData>();
    public MapData currentMap;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeMaps();
        }
    }

    private void InitializeMaps()
    {
        foreach (var map in allMapsData)
        {
            mapsDictionary[map.mapID] = map;
        }
    }

    // Call this method to switch to a different map by ID
    public void SwitchMap(string mapID)
    {
        if (mapsDictionary.TryGetValue(mapID, out MapData newMap))
        {
            // Here, you would likely also handle unloading the old map and loading the new one
            currentMap = newMap;

            // Example: Find the player and apply new map buffs
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player)
            {
                ApplyMapBuffs(player); // Assumes buffs from the previous map are removed when leaving
            }
        }
        else
        {
            Debug.LogError($"Map with ID {mapID} not found.");
        }
    }

    // Example method to apply a map's buff to the player's domain
    public void ApplyMapBuffs(GameObject player)
    {
        DomainController playerController = player.GetComponent<DomainController>();
        if (playerController == null) return;

        string playerFaction = playerController.FactionName;

        foreach (var buff in currentMap.Buffs)
        {
            bool applyBuff = false;

            // Faction-specific buff logic
            if (buff.IsFaction)
            {
                // Apply buff if player faction matches map faction, for buffs meant for this faction
                if (playerFaction == currentMap.FactionName && buff.ID == "001") // Assuming Buff 001 is the positive faction buff
                {
                    applyBuff = true;
                }
                // Apply buff if player faction does NOT match map faction, for debuffs or other factions
                else if (playerFaction != currentMap.FactionName && buff.ID == "002") // Assuming Buff 002 is meant for other factions
                {
                    applyBuff = true;
                }
            }
            else // Non-faction-specific buffs
            {
                applyBuff = true;
            }

            // Apply the buff if conditions met
            if (applyBuff)
            {
                buff.Apply(player);
            }
        }
    }


    public void RemoveMapBuffs(GameObject player)
    {
        DomainController playerController = player.GetComponent<DomainController>();
        if (playerController == null) return;

        foreach (var buff in currentMap.Buffs)
        {
            // This example assumes buffs directly modify a single attribute for simplicity
            // and that you can reverse the buff by applying the inverse operation.
            switch (buff.Type)
            {
                case Buff.BuffType.MoveSpeed:
                    playerController.moveSpeed /= buff.ModifierValue; // Reverse the move speed buff
                    break;
                    // Implement logic for other buff types similarly
                    // Note: This simple reversal assumes buffs are multiplicative. 
                    // If buffs can be additive or have other complex effects, you'll need a more sophisticated approach.
            }
        }
    }


    // Add other methods to handle transitions, weather effects, etc., here
}


