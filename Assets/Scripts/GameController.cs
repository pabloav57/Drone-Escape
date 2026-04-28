using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameController : MonoBehaviour
{
    private const string DifficultyKey = "SelectedDifficulty";
    private const string BestScoreKey = "BestObstacleScore";
    private const string GameModeKey = "SelectedGameMode";

    public GameObject startMenu;
    public TextMeshProUGUI startText;
    public TextMeshProUGUI gameOverText;
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI bestScoreText;
    public TextMeshProUGUI gameModeText;
    public Button restartButton;
    public Button exitButton;
    public Button resumeButton;
    public GameObject gameOverMenu;
    public Transform drone;
    public Image transitionOverlay;
    public float transitionDuration = 0.3f;
    public float countdownPopScale = 1.2f;
    public float countdownPulseSpeed = 8f;
    public float menuFadeDuration = 0.25f;

    private float elapsedTime;
    private bool isGameOver;
    private bool isPaused = true;
    private bool isTransitioning;
    private int currentScore;
    private int bestScore;
    private int selectedDifficulty;
    private int selectedGameMode;
    private bool pursuitActive;
    private float pursuitRemainingTime;
    private CanvasGroup gameOverCanvasGroup;
    private Image gameOverCard;
    private Image hudLeftPanel;
    private Image hudRightPanel;
    private Vector3 startTextBaseScale = Vector3.one;

    public int CurrentScore => currentScore;

    void Start()
    {
        GameAudioSettings.Apply();
        EnsureEnvironmentStyler();
        selectedDifficulty = PlayerPrefs.GetInt(DifficultyKey, 1);
        selectedGameMode = PlayerPrefs.GetInt(GameModeKey, 0);
        SetDifficulty(selectedDifficulty);
        bestScore = PlayerPrefs.GetInt(BestScoreKey, 0);

        RegisterButtonCallbacks();
        ConfigureHudPresentation();
        SetupMenuCanvasGroup();
        ConfigureGameOverPresentation();
        HideMenus();
        RefreshHud();
        SetOverlayAlpha(1f);

        if (startText != null)
        {
            startTextBaseScale = startText.transform.localScale;
        }

        Time.timeScale = 0f;
        StartCoroutine(SceneIntroRoutine());
    }

    void Update()
    {
        if (!isGameOver && !isPaused)
        {
            elapsedTime += Time.deltaTime;
            RefreshHud();
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isGameOver)
            {
                ShowGameOverMenu();
            }
            else if (!isPaused)
            {
                ShowPauseMenu();
            }
            else
            {
                ResumeGame();
            }
        }

        if (isGameOver && (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return)))
        {
            RestartGame();
        }

        AnimateCountdown();
    }

    IEnumerator CountdownToStart()
    {
        if (startText == null)
        {
            isPaused = false;
            Time.timeScale = 1f;
            yield break;
        }

        for (int i = 3; i > 0; i--)
        {
            startText.text = i.ToString();
            startText.gameObject.SetActive(true);
            startText.transform.localScale = startTextBaseScale * countdownPopScale;
            yield return new WaitForSecondsRealtime(1f);
        }

        startText.gameObject.SetActive(false);
        startText.transform.localScale = startTextBaseScale;
        isPaused = false;
        Time.timeScale = 1f;
    }

    void SetDifficulty(int difficultyIndex)
    {
        PlayerPrefs.SetInt(DifficultyKey, difficultyIndex);
    }

    public void EndGame()
    {
        if (isGameOver)
        {
            return;
        }

        isGameOver = true;
        isPaused = true;

        if (currentScore > bestScore)
        {
            bestScore = currentScore;
            PlayerPrefs.SetInt(BestScoreKey, bestScore);
            PlayerPrefs.Save();
        }

        Time.timeScale = 0f;
        ShowGameOverMenu();
    }

    void ShowGameOverMenu()
    {
        if (gameOverMenu != null)
            gameOverMenu.SetActive(true);

        if (gameOverText != null)
        {
            gameOverText.gameObject.SetActive(true);
            gameOverText.text =
                "<size=54><color=#FF4D5E>Game Over</color></size>\n" +
                "<size=26><color=#AAB6FF>Otra ronda y lo rompes.</color></size>\n\n" +
                "<size=34>Obstaculos: <color=#FFFFFF>" + currentScore + "</color></size>\n" +
                "<size=28>Record: <color=#FFE27A>" + bestScore + "</color></size>\n" +
                "<size=24>Tiempo: " + elapsedTime.ToString("F2") + "s</size>\n\n" +
                "<size=22><color=#BFC7D8>Espacio o Enter para reiniciar</color></size>";
        }

        if (resumeButton != null)
            resumeButton.gameObject.SetActive(false);

        if (restartButton != null)
            restartButton.gameObject.SetActive(true);

        if (exitButton != null)
            exitButton.gameObject.SetActive(true);

        StartCoroutine(FadeMenuCanvas(1f));
    }

    void ShowPauseMenu()
    {
        Time.timeScale = 0f;
        isPaused = true;

        if (gameOverMenu != null)
            gameOverMenu.SetActive(true);

        if (gameOverText != null)
        {
            gameOverText.gameObject.SetActive(true);
            gameOverText.text =
                "<size=54><color=#FFE27A>Pausa</color></size>\n" +
                "<size=26><color=#AAB6FF>Respira, ajusta y sigue.</color></size>\n\n" +
                "<size=34>Obstaculos: <color=#FFFFFF>" + currentScore + "</color></size>\n" +
                "<size=28>Record: <color=#FFE27A>" + bestScore + "</color></size>\n" +
                "<size=24>Tiempo: " + elapsedTime.ToString("F2") + "s</size>\n\n" +
                "<size=22><color=#BFC7D8>ESC para reanudar</color></size>";
        }

        if (resumeButton != null)
            resumeButton.gameObject.SetActive(true);

        if (restartButton != null)
            restartButton.gameObject.SetActive(true);

        if (exitButton != null)
            exitButton.gameObject.SetActive(true);

        StartCoroutine(FadeMenuCanvas(1f));
    }

    void ResumeGame()
    {
        if (isGameOver)
        {
            return;
        }

        Time.timeScale = 1f;
        isPaused = false;
        StartCoroutine(FadeOutPauseMenu());
        RefreshHud();
    }

    public void RestartGame()
    {
        if (isTransitioning)
        {
            return;
        }

        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void ExitGame()
    {
        Time.timeScale = 1f;
        Application.Quit();
    }

    public void ResumeGameFromButton()
    {
        ResumeGame();
    }

    public void AddObstaclePoint(int amount = 1)
    {
        if (isGameOver || isPaused || amount <= 0)
        {
            return;
        }

        currentScore += amount;

        if (currentScore > bestScore)
        {
            bestScore = currentScore;
            PlayerPrefs.SetInt(BestScoreKey, bestScore);
            PlayerPrefs.Save();
        }

        RefreshHud();
    }

    public void SetPursuitStatus(bool active, float remainingTime)
    {
        pursuitActive = active;
        pursuitRemainingTime = Mathf.Max(0f, remainingTime);
        RefreshHud();
    }

    private void RefreshHud()
    {
        if (timerText != null)
        {
            timerText.text = "<size=18><color=#AEB8C8>TIEMPO</color></size>\n<size=30><b>" + elapsedTime.ToString("F1") + "s</b></size>";
        }

        if (scoreText != null)
        {
            scoreText.text = "<size=18><color=#AEB8C8>OBSTACULOS</color></size>\n<size=34><b>" + currentScore + "</b></size>";
        }

        if (bestScoreText != null)
        {
            bestScoreText.text = "<size=18><color=#AEB8C8>RECORD</color></size>\n<size=26><color=#FFE27A><b>" + bestScore + "</b></color></size>";
        }

        if (gameModeText != null)
        {
            if (selectedGameMode == 3 && pursuitActive)
            {
                gameModeText.text = "<size=18><color=#AEB8C8>MODO</color></size>\n<size=24><b>Persecucion</b></size>\n<size=20><color=#FF4D5E>Perseguidor " + pursuitRemainingTime.ToString("F1") + "s</color></size>";
            }
            else if (selectedGameMode == 3)
            {
                gameModeText.text = "<size=18><color=#AEB8C8>MODO</color></size>\n<size=24><b>Persecucion</b></size>\n<size=20><color=#BFC7D8>Sin amenaza</color></size>";
            }
            else
            {
                gameModeText.text = "<size=18><color=#AEB8C8>MODO</color></size>\n<size=24><b>" + GetModeName() + "</b></size>";
            }
        }
    }

    private void ConfigureHudPresentation()
    {
        Canvas parentCanvas = FindAnyObjectByType<Canvas>();
        Transform hudParent = parentCanvas != null ? parentCanvas.transform : transform;

        hudLeftPanel = CreateHudPanel("HudLeftPanel", hudParent, new Vector2(16f, -16f), new Vector2(230f, 245f), TextAnchor.UpperLeft);
        hudRightPanel = CreateHudPanel("HudRightPanel", hudParent, new Vector2(-16f, -16f), new Vector2(170f, 86f), TextAnchor.UpperRight);

        StyleHudText(scoreText, new Vector2(18f, -18f), new Vector2(190f, 66f), TextAlignmentOptions.TopLeft);
        StyleHudText(bestScoreText, new Vector2(18f, -92f), new Vector2(190f, 58f), TextAlignmentOptions.TopLeft);
        StyleHudText(gameModeText, new Vector2(18f, -156f), new Vector2(200f, 78f), TextAlignmentOptions.TopLeft);
        StyleHudText(timerText, new Vector2(-18f, -14f), new Vector2(135f, 64f), TextAlignmentOptions.TopRight);
    }

    private Image CreateHudPanel(string objectName, Transform parent, Vector2 position, Vector2 size, TextAnchor anchor)
    {
        Transform existing = parent.Find(objectName);
        Image panel = existing != null ? existing.GetComponent<Image>() : null;
        if (panel == null)
        {
            GameObject panelObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            panelObject.transform.SetParent(parent, false);
            panel = panelObject.GetComponent<Image>();
        }

        panel.color = new Color(0.025f, 0.035f, 0.05f, 0.48f);
        panel.raycastTarget = false;

        RectTransform rect = panel.rectTransform;
        if (anchor == TextAnchor.UpperRight)
        {
            rect.anchorMin = new Vector2(1f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
        }
        else
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
        }

        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        rect.SetAsFirstSibling();
        return panel;
    }

    private void StyleHudText(TextMeshProUGUI text, Vector2 position, Vector2 size, TextAlignmentOptions alignment)
    {
        if (text == null)
        {
            return;
        }

        text.fontSize = 24f;
        text.enableAutoSizing = true;
        text.fontSizeMin = 15f;
        text.fontSizeMax = 34f;
        text.color = new Color(0.92f, 0.96f, 1f, 1f);
        text.alignment = alignment;
        text.raycastTarget = false;
        text.lineSpacing = -8f;

        RectTransform rect = text.rectTransform;
        bool rightAligned = alignment == TextAlignmentOptions.TopRight;

        rect.anchorMin = rightAligned ? new Vector2(1f, 1f) : new Vector2(0f, 1f);
        rect.anchorMax = rightAligned ? new Vector2(1f, 1f) : new Vector2(0f, 1f);
        rect.pivot = rightAligned ? new Vector2(1f, 1f) : new Vector2(0f, 1f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    private void RegisterButtonCallbacks()
    {
        if (restartButton != null)
        {
            restartButton.onClick.RemoveListener(RestartGame);
            restartButton.onClick.AddListener(RestartGame);
        }

        if (exitButton != null)
        {
            exitButton.onClick.RemoveListener(ExitGame);
            exitButton.onClick.AddListener(ExitGame);
        }

        if (resumeButton != null)
        {
            resumeButton.onClick.RemoveListener(ResumeGameFromButton);
            resumeButton.onClick.AddListener(ResumeGameFromButton);
        }
    }

    private void EnsureEnvironmentStyler()
    {
        if (FindAnyObjectByType<EnvironmentStyler>() != null)
        {
            return;
        }

        gameObject.AddComponent<EnvironmentStyler>();
    }

    private void SetupMenuCanvasGroup()
    {
        if (gameOverMenu == null)
        {
            return;
        }

        gameOverCanvasGroup = gameOverMenu.GetComponent<CanvasGroup>();
        if (gameOverCanvasGroup == null)
        {
            gameOverCanvasGroup = gameOverMenu.AddComponent<CanvasGroup>();
        }

        gameOverCanvasGroup.alpha = 0f;
        gameOverCanvasGroup.interactable = false;
        gameOverCanvasGroup.blocksRaycasts = false;
    }

    private void ConfigureGameOverPresentation()
    {
        if (gameOverMenu == null)
        {
            return;
        }

        Image overlay = gameOverMenu.GetComponent<Image>();
        if (overlay != null)
        {
            overlay.color = new Color(0.02f, 0.03f, 0.05f, 0.62f);
        }

        CreateOrUpdateGameOverCard();
        ConfigureMenuText();
        ConfigureMenuButtons();
    }

    private void CreateOrUpdateGameOverCard()
    {
        Transform existingCard = gameOverMenu.transform.Find("GameOverCard");
        if (existingCard != null)
        {
            gameOverCard = existingCard.GetComponent<Image>();
        }

        if (gameOverCard == null)
        {
            GameObject cardObject = new GameObject("GameOverCard", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            cardObject.transform.SetParent(gameOverMenu.transform, false);
            cardObject.transform.SetAsFirstSibling();
            gameOverCard = cardObject.GetComponent<Image>();
        }

        gameOverCard.color = new Color(0.035f, 0.045f, 0.065f, 0.9f);
        gameOverCard.raycastTarget = false;

        RectTransform cardRect = gameOverCard.rectTransform;
        cardRect.anchorMin = new Vector2(0.5f, 0.5f);
        cardRect.anchorMax = new Vector2(0.5f, 0.5f);
        cardRect.pivot = new Vector2(0.5f, 0.5f);
        cardRect.anchoredPosition = new Vector2(0f, -10f);
        cardRect.sizeDelta = new Vector2(620f, 430f);
    }

    private void ConfigureMenuText()
    {
        if (gameOverText == null)
        {
            return;
        }

        gameOverText.alignment = TextAlignmentOptions.Center;
        gameOverText.color = new Color(0.9f, 0.94f, 1f, 1f);
        gameOverText.fontSize = 30f;
        gameOverText.enableAutoSizing = true;
        gameOverText.fontSizeMin = 18f;
        gameOverText.fontSizeMax = 54f;
        gameOverText.lineSpacing = 8f;
        gameOverText.margin = new Vector4(24f, 12f, 24f, 12f);

        RectTransform textRect = gameOverText.rectTransform;
        textRect.anchorMin = new Vector2(0.5f, 0.5f);
        textRect.anchorMax = new Vector2(0.5f, 0.5f);
        textRect.pivot = new Vector2(0.5f, 0.5f);
        textRect.anchoredPosition = new Vector2(0f, 35f);
        textRect.sizeDelta = new Vector2(540f, 300f);
    }

    private void ConfigureMenuButtons()
    {
        Transform buttonContainer = null;

        if (restartButton != null)
        {
            buttonContainer = restartButton.transform.parent;
        }
        else if (exitButton != null)
        {
            buttonContainer = exitButton.transform.parent;
        }

        RectTransform containerRect = buttonContainer as RectTransform;
        if (containerRect != null)
        {
            containerRect.anchorMin = new Vector2(0.5f, 0.5f);
            containerRect.anchorMax = new Vector2(0.5f, 0.5f);
            containerRect.pivot = new Vector2(0.5f, 0.5f);
            containerRect.anchoredPosition = new Vector2(0f, -150f);
            containerRect.sizeDelta = new Vector2(520f, 58f);

            HorizontalLayoutGroup layout = containerRect.GetComponent<HorizontalLayoutGroup>();
            if (layout != null)
            {
                layout.padding = new RectOffset(0, 0, 0, 0);
                layout.spacing = 14f;
                layout.childAlignment = TextAnchor.MiddleCenter;
                layout.childControlWidth = true;
                layout.childControlHeight = true;
                layout.childForceExpandWidth = true;
                layout.childForceExpandHeight = true;
            }
        }

        StyleMenuButton(resumeButton, "Reanudar", new Color(0.34f, 0.75f, 1f, 1f));
        StyleMenuButton(restartButton, "Reintentar", new Color(0.38f, 0.84f, 0.58f, 1f));
        StyleMenuButton(exitButton, "Salir", new Color(0.95f, 0.4f, 0.45f, 1f));
    }

    private void StyleMenuButton(Button button, string label, Color color)
    {
        if (button == null)
        {
            return;
        }

        Image buttonImage = button.GetComponent<Image>();
        if (buttonImage != null)
        {
            buttonImage.color = color;
        }

        ColorBlock colors = button.colors;
        colors.normalColor = color;
        colors.highlightedColor = Color.Lerp(color, Color.white, 0.18f);
        colors.pressedColor = Color.Lerp(color, Color.black, 0.12f);
        colors.selectedColor = colors.highlightedColor;
        colors.disabledColor = new Color(color.r, color.g, color.b, 0.35f);
        colors.fadeDuration = 0.08f;
        button.colors = colors;

        LayoutElement layoutElement = button.GetComponent<LayoutElement>();
        if (layoutElement == null)
        {
            layoutElement = button.gameObject.AddComponent<LayoutElement>();
        }

        layoutElement.minWidth = 150f;
        layoutElement.preferredWidth = 170f;
        layoutElement.minHeight = 52f;
        layoutElement.preferredHeight = 52f;

        TextMeshProUGUI labelText = button.GetComponentInChildren<TextMeshProUGUI>(true);
        if (labelText != null)
        {
            labelText.text = label;
            labelText.color = Color.white;
            labelText.fontSize = 22f;
            labelText.fontStyle = FontStyles.Bold;
            labelText.alignment = TextAlignmentOptions.Center;
        }
    }

    private void HideMenus()
    {
        if (gameOverText != null)
            gameOverText.gameObject.SetActive(false);

        if (restartButton != null)
            restartButton.gameObject.SetActive(false);

        if (exitButton != null)
            exitButton.gameObject.SetActive(false);

        if (resumeButton != null)
            resumeButton.gameObject.SetActive(false);

        if (gameOverCanvasGroup != null)
        {
            gameOverCanvasGroup.alpha = 0f;
            gameOverCanvasGroup.interactable = false;
            gameOverCanvasGroup.blocksRaycasts = false;
        }

        if (gameOverMenu != null)
            gameOverMenu.SetActive(false);

        if (startMenu != null)
            startMenu.SetActive(false);
    }

    private string GetModeName()
    {
        switch (selectedGameMode)
        {
            case 1:
                return "Zen";
            case 2:
                return "Rush";
            case 3:
                return "Persecucion";
            default:
                return "Clasico";
        }
    }

    private IEnumerator SceneIntroRoutine()
    {
        float elapsed = 0f;

        while (elapsed < transitionDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / transitionDuration);
            SetOverlayAlpha(1f - Mathf.SmoothStep(0f, 1f, progress));
            yield return null;
        }

        SetOverlayAlpha(0f);
        yield return StartCoroutine(CountdownToStart());
    }

    private IEnumerator ReloadSceneWithFade()
    {
        isTransitioning = true;

        float elapsed = 0f;
        while (elapsed < transitionDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / transitionDuration);
            SetOverlayAlpha(Mathf.SmoothStep(0f, 1f, progress));
            yield return null;
        }

        elapsedTime = 0f;
        currentScore = 0;
        isGameOver = false;
        isPaused = false;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private IEnumerator FadeMenuCanvas(float targetAlpha)
    {
        if (gameOverCanvasGroup == null)
        {
            yield break;
        }

        float initialAlpha = gameOverCanvasGroup.alpha;
        float elapsed = 0f;

        gameOverCanvasGroup.interactable = false;
        gameOverCanvasGroup.blocksRaycasts = false;

        while (elapsed < menuFadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / menuFadeDuration);
            gameOverCanvasGroup.alpha = Mathf.Lerp(initialAlpha, targetAlpha, progress);
            yield return null;
        }

        gameOverCanvasGroup.alpha = targetAlpha;
        bool isVisible = targetAlpha > 0.95f;
        gameOverCanvasGroup.interactable = isVisible;
        gameOverCanvasGroup.blocksRaycasts = isVisible;
    }

    private IEnumerator FadeOutPauseMenu()
    {
        yield return StartCoroutine(FadeMenuCanvas(0f));
        HideMenus();
    }

    private void SetOverlayAlpha(float alpha)
    {
        if (transitionOverlay == null)
        {
            return;
        }

        transitionOverlay.gameObject.SetActive(alpha > 0.001f);
        Color color = transitionOverlay.color;
        color.a = alpha;
        transitionOverlay.color = color;
    }

    private void AnimateCountdown()
    {
        if (startText == null || !startText.gameObject.activeSelf)
        {
            return;
        }

        float pulse = 1f + (Mathf.Sin(Time.unscaledTime * countdownPulseSpeed) * (countdownPopScale - 1f));
        startText.transform.localScale = startTextBaseScale * pulse;
    }
}
