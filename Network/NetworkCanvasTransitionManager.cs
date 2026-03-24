using System;
using System.Collections;
using System.Reflection;
using TMPro;
using UnityEngine;

/// <summary>
/// Login → class/faction → optional additive world load + Mirror client-ready gate (ParrelSync: start Host/Client from HUD per clone).
/// Wires to <see cref="DBConnectionManager"/> and <see cref="NetworkPlayerProfileManager"/>; assign UI in the scene later.
/// </summary>
public class NetworkCanvasTransitionManager : MonoBehaviour
{
    public static NetworkCanvasTransitionManager Instance { get; private set; }

    public CanvasGroup loginCanvasGroup;
    public CanvasGroup backPageCanvasGroup;
    public CanvasGroup blackScreenCanvasGroup;
    public CanvasGroup classSelectionCanvasGroup;
    public CanvasGroup factionSelectionCanvasGroup;
    public float fadeDuration = 0.3f;

    public TMP_InputField usernameInputField;
    public TMP_InputField passwordInputField;

    public static event Action OnLoginSuccess;

    public PlayerStats loadedStats;

    public string playerName;

    [Tooltip("Additive scene after setup (e.g. game world). Empty = skip load; still runs Mirror ready wait if a client is active.")]
    [SerializeField] private string additiveGameSceneName;

    [SerializeField] private bool waitForMirrorClientReady = true;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        ShowLoginPage();
    }

    public void ShowLoginPage()
    {
        SetCanvasState(loginCanvasGroup, 1, true);
        SetCanvasState(backPageCanvasGroup, 0, false);
        SetCanvasState(classSelectionCanvasGroup, 0, false);
        SetCanvasState(factionSelectionCanvasGroup, 0, false);
        SetCanvasState(blackScreenCanvasGroup, 0, false);
    }

    public void OnLoginButtonPressed()
    {
        if (DBConnectionManager.Instance == null)
        {
            Debug.LogError("[NetworkCanvasTransitionManager] DBConnectionManager missing.");
            return;
        }

        string username = usernameInputField != null ? usernameInputField.text : "";
        string password = passwordInputField != null ? passwordInputField.text : "";

        if (!DBConnectionManager.Instance.ValidateLogin(username, password))
        {
            Debug.LogError("[NetworkCanvasTransitionManager] Invalid username or password.");
            return;
        }

        playerName = username;
        LoadPlayerStats(username);
        Debug.Log("[NetworkCanvasTransitionManager] Login OK, invoking OnLoginSuccess.");
        OnLoginSuccess?.Invoke();
    }

    public void OnCreateAccountButtonPressed()
    {
        if (DBConnectionManager.Instance == null)
        {
            Debug.LogError("[NetworkCanvasTransitionManager] DBConnectionManager missing.");
            return;
        }

        string username = usernameInputField != null ? usernameInputField.text : "";
        string password = passwordInputField != null ? passwordInputField.text : "";

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            Debug.LogWarning("[NetworkCanvasTransitionManager] Username or password empty.");
            return;
        }

        DBConnectionManager.Instance.CreateUser(username, password);
        playerName = username;
        LoadPlayerStats(username);
        Debug.Log("[NetworkCanvasTransitionManager] Account created, invoking OnLoginSuccess.");
        OnLoginSuccess?.Invoke();
    }

    private void LoadPlayerStats(string username)
    {
        if (NetworkPlayerProfileManager.Instance == null)
        {
            Debug.LogError("[NetworkCanvasTransitionManager] NetworkPlayerProfileManager missing.");
            return;
        }

        loadedStats = NetworkPlayerProfileManager.Instance.LoadStatsFromDatabase(username);
        if (loadedStats == null)
        {
            Debug.LogError("[NetworkCanvasTransitionManager] Failed to load player stats.");
            return;
        }

        StartCoroutine(FadeOut(loginCanvasGroup, fadeDuration, null));

        if (loadedStats.hasSelectedClass && loadedStats.hasSelectedFaction)
        {
            DisableMainCamera();
            StartCoroutine(TransitionToGame());
        }
        else if (!loadedStats.hasSelectedClass)
            StartCoroutine(TransitionToClassSelection());
        else if (!loadedStats.hasSelectedFaction)
            StartCoroutine(TransitionToFactionSelection());
    }

    public void OnClassSelected()
    {
        if (loadedStats == null)
        {
            Debug.LogError("[NetworkCanvasTransitionManager] loadedStats null in OnClassSelected.");
            return;
        }

        StartCoroutine(TransitionToFactionSelection());
    }

    public void OnFactionSelected()
    {
        if (string.IsNullOrEmpty(playerName))
        {
            Debug.LogError("[NetworkCanvasTransitionManager] playerName empty in OnFactionSelected.");
            return;
        }

        if (NetworkPlayerProfileManager.Instance == null)
            return;

        loadedStats = NetworkPlayerProfileManager.Instance.LoadStatsFromDatabase(playerName);
        if (loadedStats == null)
        {
            Debug.LogError("[NetworkCanvasTransitionManager] Failed to reload stats in OnFactionSelected.");
            return;
        }

        if (loadedStats.hasSelectedClass && loadedStats.hasSelectedFaction)
        {
            DisableMainCamera();
            StartCoroutine(TransitionToGame());
        }
        else
            Debug.LogWarning("[NetworkCanvasTransitionManager] Class/faction not complete.");
    }

    private void DisableMainCamera()
    {
        GameObject mainCameraObject = GameObject.FindGameObjectWithTag("MainCamera");
        if (mainCameraObject == null)
            return;

        var audioListener = mainCameraObject.GetComponent<AudioListener>();
        if (audioListener != null)
            audioListener.enabled = false;

        var cam = mainCameraObject.GetComponent<Camera>();
        if (cam != null)
            cam.enabled = false;
    }

    private IEnumerator TransitionToClassSelection()
    {
        yield return StartCoroutine(FadeIn(classSelectionCanvasGroup, fadeDuration));
        yield return StartCoroutine(FadeOut(blackScreenCanvasGroup, fadeDuration, null));
    }

    private IEnumerator TransitionToFactionSelection()
    {
        yield return StartCoroutine(FadeIn(blackScreenCanvasGroup, fadeDuration));
        yield return StartCoroutine(FadeOut(classSelectionCanvasGroup, fadeDuration, null));
        yield return StartCoroutine(FadeIn(factionSelectionCanvasGroup, fadeDuration));
        yield return StartCoroutine(FadeOut(blackScreenCanvasGroup, fadeDuration, null));
    }

    private IEnumerator TransitionToGame()
    {
        if (classSelectionCanvasGroup != null && classSelectionCanvasGroup.alpha > 0)
            yield return StartCoroutine(FadeOut(classSelectionCanvasGroup, fadeDuration, null));

        if (factionSelectionCanvasGroup != null && factionSelectionCanvasGroup.alpha > 0)
            yield return StartCoroutine(FadeOut(factionSelectionCanvasGroup, fadeDuration, null));

        yield return StartCoroutine(FadeIn(blackScreenCanvasGroup, fadeDuration));

        if (Camera.main != null)
            Camera.main.enabled = false;

        yield return StartCoroutine(FadeOut(blackScreenCanvasGroup, fadeDuration, null));

        // ParrelSync: start Host or Client from Mirror HUD in each clone — no auto StartHost here.
        yield return StartCoroutine(
            NetworkedLoadingGate.WaitForAsyncLoadAndMirrorReady(additiveGameSceneName, waitForMirrorClientReady));
    }

    private IEnumerator FadeOut(CanvasGroup canvasGroup, float duration, Action onComplete)
    {
        if (canvasGroup == null)
        {
            onComplete?.Invoke();
            yield break;
        }

        float startAlpha = canvasGroup.alpha;
        float time = 0f;
        while (time < duration)
        {
            time += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, 0, time / duration);
            yield return null;
        }

        canvasGroup.alpha = 0;
        canvasGroup.blocksRaycasts = false;
        onComplete?.Invoke();
    }

    private IEnumerator FadeIn(CanvasGroup canvasGroup, float duration)
    {
        if (canvasGroup == null)
            yield break;

        float startAlpha = canvasGroup.alpha;
        float time = 0f;
        while (time < duration)
        {
            time += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, 1, time / duration);
            yield return null;
        }

        canvasGroup.alpha = 1;
        canvasGroup.blocksRaycasts = true;
    }

    private void SetCanvasState(CanvasGroup canvasGroup, float alpha, bool blocksRaycasts)
    {
        if (canvasGroup == null)
            return;
        canvasGroup.alpha = alpha;
        canvasGroup.blocksRaycasts = blocksRaycasts;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F1))
            ToggleBackPageVisibility();
    }

    public void ToggleBackPageVisibility()
    {
        if (backPageCanvasGroup == null)
            return;

        if (backPageCanvasGroup.alpha > 0)
            StartCoroutine(FadeOut(backPageCanvasGroup, fadeDuration, null));
        else
            StartCoroutine(FadeIn(backPageCanvasGroup, fadeDuration));
    }

    /// <summary>Debug: log scalar properties on <see cref="loadedStats"/>.</summary>
    public void LogLoadedStats()
    {
        if (loadedStats == null)
            return;

        var sb = new System.Text.StringBuilder("Loaded stats: [");
        foreach (var prop in loadedStats.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (!prop.CanRead || prop.GetIndexParameters().Length > 0)
                continue;
            sb.Append(prop.Name).Append(": ").Append(prop.GetValue(loadedStats)).Append(", ");
        }

        if (sb.Length > 2)
            sb.Length -= 2;
        sb.Append(']');
        Debug.Log(sb.ToString());
    }
}
