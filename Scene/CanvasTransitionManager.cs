using UnityEngine;
using System.Collections;

public class CanvasTransitionManager : MonoBehaviour
{
    public static CanvasTransitionManager Instance { get; private set; }

    public CanvasGroup loginCanvasGroup;
    public CanvasGroup backPageCanvasGroup;
    public CanvasGroup blackScreenCanvasGroup;
    public CanvasGroup classSelectionCanvasGroup;
    public CanvasGroup factionSelectionCanvasGroup;
    public CanvasGroup inventoryCanvasGroup;
    public CanvasGroup overlayCanvasGroup;
    public CanvasGroup hudCanvasGroup;
    public CanvasGroup tooltipCanvasGroup;
    public float fadeDuration = 0.3f; // Default duration for the fade effect

    private void Awake()
    {
        // Implement the singleton pattern
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
        StartCoroutine(TransitionToClassSelection());
    }

    private IEnumerator TransitionToClassSelection()
    {
        yield return StartCoroutine(FadeIn(blackScreenCanvasGroup, fadeDuration));
        yield return StartCoroutine(FadeOut(loginCanvasGroup, fadeDuration, null));
        yield return StartCoroutine(FadeIn(classSelectionCanvasGroup, fadeDuration));
        yield return StartCoroutine(FadeOut(blackScreenCanvasGroup, fadeDuration, null));
    }

    public void OnClassSelected()
    {
        StartCoroutine(TransitionToFactionSelection());
    }

    private IEnumerator TransitionToFactionSelection()
    {
        yield return StartCoroutine(FadeIn(blackScreenCanvasGroup, fadeDuration));
        yield return StartCoroutine(FadeOut(classSelectionCanvasGroup, fadeDuration, null));
        yield return StartCoroutine(FadeIn(factionSelectionCanvasGroup, fadeDuration));
        yield return StartCoroutine(FadeOut(blackScreenCanvasGroup, fadeDuration, null));
    }

    public void OnFactionSelected()
    {
        StartCoroutine(TransitionToGame());
    }

    private IEnumerator TransitionToGame()
    {
        yield return StartCoroutine(FadeIn(blackScreenCanvasGroup, fadeDuration));
        yield return StartCoroutine(FadeOut(factionSelectionCanvasGroup, fadeDuration, null));
        yield return StartCoroutine(FadeIn(backPageCanvasGroup, fadeDuration));
        yield return StartCoroutine(FadeOut(blackScreenCanvasGroup, fadeDuration, null));
    }

    private IEnumerator FadeOut(CanvasGroup canvasGroup, float duration, System.Action onComplete)
    {
        float startAlpha = canvasGroup.alpha;
        float time = 0;

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
        float startAlpha = canvasGroup.alpha;
        float time = 0;

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
        canvasGroup.alpha = alpha;
        canvasGroup.blocksRaycasts = blocksRaycasts;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F1))
        {
            ToggleBackPageVisibility();
        }
    }

    public void ToggleBackPageVisibility()
    {
        if (backPageCanvasGroup.alpha > 0)
        {
            // If the back page is visible, fade it out
            StartCoroutine(FadeOut(backPageCanvasGroup, fadeDuration, null));
        }
        else
        {
            // If the back page is not visible, fade it in
            StartCoroutine(FadeIn(backPageCanvasGroup, fadeDuration));
        }
    }
}
