using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class PlayerSetupManager : MonoBehaviour
{
    public CanvasGroup classSelectionCanvasGroup;
    public CanvasGroup factionSelectionCanvasGroup;
    public CanvasGroup nameInputCanvasGroup;
    public CanvasGroup playerSetupCanvasGroup;

    public InputField playerNameInput;
    private string selectedClass;
    private string selectedFaction;

    private void Start()
    {
        ShowClassSelection();
    }

    public void ShowClassSelection()
    {
        SetCanvasGroupVisibility(classSelectionCanvasGroup, true);
        SetCanvasGroupVisibility(factionSelectionCanvasGroup, false);
        SetCanvasGroupVisibility(nameInputCanvasGroup, false);
    }

    public void OnClassSelected(string className)
    {
        selectedClass = className;
        ShowFactionSelection();
    }

    public void ShowFactionSelection()
    {
        SetCanvasGroupVisibility(classSelectionCanvasGroup, false);
        SetCanvasGroupVisibility(factionSelectionCanvasGroup, true);
        SetCanvasGroupVisibility(nameInputCanvasGroup, false);
    }

    public void OnFactionSelected(string factionName)
    {
        selectedFaction = factionName;
        ShowNameInput();
    }

    public void ShowNameInput()
    {
        SetCanvasGroupVisibility(classSelectionCanvasGroup, false);
        SetCanvasGroupVisibility(factionSelectionCanvasGroup, false);
        SetCanvasGroupVisibility(nameInputCanvasGroup, true);
    }

    public void OnNameInputConfirmed()
    {
        string playerName = playerNameInput.text;
        // Save or use the selected class, faction, and player name
        Debug.Log($"Player Setup: Class={selectedClass}, Faction={selectedFaction}, Name={playerName}");

        // Hide the setup canvas
        SetCanvasGroupVisibility(playerSetupCanvasGroup, false);
    }

    private void SetCanvasGroupVisibility(CanvasGroup canvasGroup, bool visible)
    {
        canvasGroup.alpha = visible ? 1 : 0;
        canvasGroup.blocksRaycasts = visible;
        canvasGroup.interactable = visible;
    }
}
