using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

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
    public Button mainMenuButton;
    public GameObject gameOverMenu;
    public Transform drone;
    public Image transitionOverlay;
    public string mainMenuSceneName = "MainScene";
    public float transitionDuration = 0.3f;
    public float countdownPopScale = 1.2f;
    public float countdownPulseSpeed = 8f;
    public float menuFadeDuration = 0.25f;
    public float scorePopScale = 1.18f;
    public float scorePopDuration = 0.18f;
    public AudioClip gameStartClip;
    public AudioClip gameOverClip;
    [Range(0f, 1f)] public float gameStartVolume = 0.75f;
    [Range(0f, 1f)] public float gameOverVolume = 0.85f;

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
    private Image impactFlashImage;
    private TextMeshProUGUI scorePopupText;
    private TextMeshProUGUI recordPopupText;
    private TextMeshProUGUI objectiveText;
    private TextMeshProUGUI objectivePopupText;
    private Vector3 startTextBaseScale = Vector3.one;
    private Vector3 scoreTextBaseScale = Vector3.one;
    private Vector3 bestScoreTextBaseScale = Vector3.one;
    private bool reachedNewRecord;
    private bool objectiveCompleted;
    private int objectiveScoreTarget;
    private float objectiveTimeTarget;
    private string objectiveTitle;
    private Coroutine scorePulseRoutine;
    private Coroutine scorePopupRoutine;
    private Coroutine impactFlashRoutine;
    private Coroutine objectivePopupRoutine;
    private AudioSource feedbackAudioSource;
    private AudioClip scoreBlipClip;
    private AudioClip recordBlipClip;

    public int CurrentScore => currentScore;

#if UNITY_EDITOR
    private void OnValidate()
    {
        AutoAssignFeedbackClips();
    }
#endif

    void Start()
    {
        GameAudioSettings.Apply();
        AutoAssignFeedbackClips();
        EnsureEnvironmentStyler();
        selectedDifficulty = PlayerPrefs.GetInt(DifficultyKey, 1);
        selectedGameMode = PlayerPrefs.GetInt(GameModeKey, 0);
        SetDifficulty(selectedDifficulty);
        bestScore = PlayerPrefs.GetInt(BestScoreKey, 0);
        ConfigureRunObjective();

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

        if (scoreText != null)
        {
            scoreTextBaseScale = scoreText.transform.localScale;
        }

        if (bestScoreText != null)
        {
            bestScoreTextBaseScale = bestScoreText.transform.localScale;
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
            EvaluateRunObjective();
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
            PlayGameStartSound();
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
        PlayGameStartSound();
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
        PlayGameOverSound();

        if (currentScore > bestScore)
        {
            bestScore = currentScore;
            reachedNewRecord = true;
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
                (reachedNewRecord ? "<size=28><color=#FFE27A><b>NUEVO RECORD</b></color></size>\n" : "") +
                (objectiveCompleted ? "<size=24><color=#6DFF9C><b>OBJETIVO COMPLETADO</b></color></size>\n" : "") +
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

        if (mainMenuButton != null)
            mainMenuButton.gameObject.SetActive(true);

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
                "<size=24>Objetivo: <color=#6DFF9C>" + GetObjectiveProgressText() + "</color></size>\n" +
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

        if (mainMenuButton != null)
            mainMenuButton.gameObject.SetActive(true);

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

    public void ReturnToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuSceneName);
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

        bool isNewRecord = currentScore > bestScore;

        if (isNewRecord)
        {
            bestScore = currentScore;
            reachedNewRecord = true;
            PlayerPrefs.SetInt(BestScoreKey, bestScore);
            PlayerPrefs.Save();
        }

        PlayScoreFeedback(amount, isNewRecord);
        RefreshHud();
        EvaluateRunObjective();
    }

    public void PlayImpactFeedback(Vector3 worldPosition)
    {
        CreateImpactParticles(worldPosition);

        if (impactFlashRoutine != null)
        {
            StopCoroutine(impactFlashRoutine);
        }

        impactFlashRoutine = StartCoroutine(ImpactFlashRoutine());
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

        RefreshObjectiveHud();
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
        CreateScorePopupText(hudParent);
        CreateRecordPopupText(hudParent);
        CreateObjectiveText(hudParent);
        CreateObjectivePopupText(hudParent);
        CreateImpactFlashImage(hudParent);
        ConfigureFeedbackAudio();
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

    private void CreateScorePopupText(Transform parent)
    {
        if (scorePopupText != null || scoreText == null)
        {
            return;
        }

        GameObject popupObject = new GameObject("ScorePopupText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        popupObject.transform.SetParent(parent, false);
        scorePopupText = popupObject.GetComponent<TextMeshProUGUI>();
        scorePopupText.text = "";
        scorePopupText.font = scoreText.font;
        scorePopupText.fontSize = 28f;
        scorePopupText.fontStyle = FontStyles.Bold;
        scorePopupText.alignment = TextAlignmentOptions.TopLeft;
        scorePopupText.color = new Color(0.45f, 1f, 0.68f, 0f);
        scorePopupText.raycastTarget = false;

        RectTransform rect = scorePopupText.rectTransform;
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(154f, -38f);
        rect.sizeDelta = new Vector2(150f, 42f);
    }

    private void CreateRecordPopupText(Transform parent)
    {
        if (recordPopupText != null || bestScoreText == null)
        {
            return;
        }

        GameObject popupObject = new GameObject("RecordPopupText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        popupObject.transform.SetParent(parent, false);
        recordPopupText = popupObject.GetComponent<TextMeshProUGUI>();
        recordPopupText.text = "";
        recordPopupText.font = bestScoreText.font;
        recordPopupText.fontSize = 24f;
        recordPopupText.fontStyle = FontStyles.Bold;
        recordPopupText.alignment = TextAlignmentOptions.Center;
        recordPopupText.color = new Color(1f, 0.88f, 0.28f, 0f);
        recordPopupText.raycastTarget = false;

        RectTransform rect = recordPopupText.rectTransform;
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, -92f);
        rect.sizeDelta = new Vector2(360f, 48f);
    }

    private void CreateObjectiveText(Transform parent)
    {
        if (objectiveText != null)
        {
            return;
        }

        GameObject objectiveObject = new GameObject("ObjectiveText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        objectiveObject.transform.SetParent(parent, false);
        objectiveText = objectiveObject.GetComponent<TextMeshProUGUI>();
        objectiveText.text = "";
        objectiveText.font = scoreText != null ? scoreText.font : null;
        objectiveText.fontSize = 22f;
        objectiveText.fontStyle = FontStyles.Bold;
        objectiveText.enableAutoSizing = true;
        objectiveText.fontSizeMin = 14f;
        objectiveText.fontSizeMax = 24f;
        objectiveText.alignment = TextAlignmentOptions.Center;
        objectiveText.color = new Color(0.92f, 0.96f, 1f, 0.95f);
        objectiveText.raycastTarget = false;

        RectTransform rect = objectiveText.rectTransform;
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, -18f);
        rect.sizeDelta = new Vector2(520f, 58f);
    }

    private void CreateObjectivePopupText(Transform parent)
    {
        if (objectivePopupText != null)
        {
            return;
        }

        GameObject popupObject = new GameObject("ObjectivePopupText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        popupObject.transform.SetParent(parent, false);
        objectivePopupText = popupObject.GetComponent<TextMeshProUGUI>();
        objectivePopupText.text = "";
        objectivePopupText.font = objectiveText != null ? objectiveText.font : null;
        objectivePopupText.fontSize = 34f;
        objectivePopupText.fontStyle = FontStyles.Bold;
        objectivePopupText.alignment = TextAlignmentOptions.Center;
        objectivePopupText.color = new Color(0.42f, 1f, 0.62f, 0f);
        objectivePopupText.raycastTarget = false;

        RectTransform rect = objectivePopupText.rectTransform;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(0f, 120f);
        rect.sizeDelta = new Vector2(560f, 72f);
    }

    private void CreateImpactFlashImage(Transform parent)
    {
        if (impactFlashImage != null)
        {
            return;
        }

        GameObject flashObject = new GameObject("ImpactFlash", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        flashObject.transform.SetParent(parent, false);
        flashObject.transform.SetAsLastSibling();
        impactFlashImage = flashObject.GetComponent<Image>();
        impactFlashImage.color = new Color(1f, 0.05f, 0.05f, 0f);
        impactFlashImage.raycastTarget = false;

        RectTransform rect = impactFlashImage.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private void ConfigureRunObjective()
    {
        int difficultyBonus = Mathf.Max(0, selectedDifficulty) * 10;

        switch (selectedGameMode)
        {
            case 1:
                objectiveScoreTarget = 40 + difficultyBonus;
                objectiveTimeTarget = 75f;
                objectiveTitle = "Vuelo limpio";
                break;
            case 2:
                objectiveScoreTarget = 35 + difficultyBonus;
                objectiveTimeTarget = 45f;
                objectiveTitle = "Ruta express";
                break;
            case 3:
                objectiveScoreTarget = 30 + difficultyBonus;
                objectiveTimeTarget = 60f;
                objectiveTitle = "Escapa del perseguidor";
                break;
            default:
                objectiveScoreTarget = 50 + difficultyBonus;
                objectiveTimeTarget = 60f;
                objectiveTitle = "Supera la ciudad";
                break;
        }
    }

    private void RefreshObjectiveHud()
    {
        if (objectiveText == null)
        {
            return;
        }

        string statusColor = objectiveCompleted ? "#6DFF9C" : "#AAB6FF";
        string status = objectiveCompleted ? "COMPLETADO" : GetObjectiveProgressText();
        objectiveText.text = "<size=16><color=#AEB8C8>OBJETIVO</color></size>\n" +
                             "<color=" + statusColor + ">" + objectiveTitle + " - " + status + "</color>";
    }

    private string GetObjectiveProgressText()
    {
        int shownTime = Mathf.FloorToInt(Mathf.Min(elapsedTime, objectiveTimeTarget));
        return currentScore + "/" + objectiveScoreTarget + " obstaculos  |  " + shownTime + "/" + Mathf.FloorToInt(objectiveTimeTarget) + "s";
    }

    private void EvaluateRunObjective()
    {
        if (objectiveCompleted)
        {
            return;
        }

        if (currentScore >= objectiveScoreTarget || elapsedTime >= objectiveTimeTarget)
        {
            objectiveCompleted = true;
            RefreshObjectiveHud();
            ShowObjectiveCompletedPopup();
        }
    }

    private void ShowObjectiveCompletedPopup()
    {
        if (objectivePopupText == null)
        {
            return;
        }

        if (objectivePopupRoutine != null)
        {
            StopCoroutine(objectivePopupRoutine);
        }

        objectivePopupRoutine = StartCoroutine(ObjectivePopupRoutine());
    }

    private IEnumerator ObjectivePopupRoutine()
    {
        objectivePopupText.text = "OBJETIVO COMPLETADO";
        RectTransform rect = objectivePopupText.rectTransform;
        Vector2 startPosition = new Vector2(0f, 110f);
        Vector2 endPosition = new Vector2(0f, 145f);
        float duration = 1.4f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            rect.anchoredPosition = Vector2.Lerp(startPosition, endPosition, progress);
            Color color = objectivePopupText.color;
            color.a = progress < 0.7f ? 1f : Mathf.Lerp(1f, 0f, (progress - 0.7f) / 0.3f);
            objectivePopupText.color = color;
            objectivePopupText.transform.localScale = Vector3.one * (1f + Mathf.Sin(progress * Mathf.PI) * 0.08f);
            yield return null;
        }

        objectivePopupText.text = "";
        objectivePopupText.transform.localScale = Vector3.one;
        rect.anchoredPosition = startPosition;
        Color finalColor = objectivePopupText.color;
        finalColor.a = 0f;
        objectivePopupText.color = finalColor;
        objectivePopupRoutine = null;
    }

    private IEnumerator ImpactFlashRoutine()
    {
        if (impactFlashImage == null)
        {
            yield break;
        }

        float duration = 0.28f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            Color color = impactFlashImage.color;
            color.a = Mathf.Lerp(0.32f, 0f, Mathf.SmoothStep(0f, 1f, progress));
            impactFlashImage.color = color;
            yield return null;
        }

        Color clear = impactFlashImage.color;
        clear.a = 0f;
        impactFlashImage.color = clear;
        impactFlashRoutine = null;
    }

    private void CreateImpactParticles(Vector3 worldPosition)
    {
        GameObject particlesObject = new GameObject("DroneImpactParticles");
        particlesObject.transform.position = worldPosition;

        ParticleSystem particles = particlesObject.AddComponent<ParticleSystem>();
        particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        ParticleSystem.MainModule main = particles.main;
        main.playOnAwake = false;
        main.duration = 0.45f;
        main.loop = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.18f, 0.42f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(4f, 9f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.06f, 0.18f);
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.35f, 0.15f, 1f), new Color(0.45f, 0.9f, 1f, 1f));
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        ParticleSystem.EmissionModule emission = particles.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 42) });

        ParticleSystem.ShapeModule shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.35f;

        ParticleSystem.VelocityOverLifetimeModule velocity = particles.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.Local;
        velocity.x = new ParticleSystem.MinMaxCurve(-2.5f, 2.5f);
        velocity.y = new ParticleSystem.MinMaxCurve(-1.5f, 2.5f);
        velocity.z = new ParticleSystem.MinMaxCurve(-2.5f, 1.5f);

        ParticleSystem.ColorOverLifetimeModule color = particles.colorOverLifetime;
        color.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(1f, 0.38f, 0.14f), 0f),
                new GradientColorKey(new Color(0.48f, 0.9f, 1f), 1f)
            },
            new[]
            {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(0f, 1f)
            });
        color.color = gradient;

        particles.Play();
        Destroy(particlesObject, 1.2f);
    }

    private void ConfigureFeedbackAudio()
    {
        if (feedbackAudioSource == null)
        {
            feedbackAudioSource = GetComponent<AudioSource>();
            if (feedbackAudioSource == null)
            {
                feedbackAudioSource = gameObject.AddComponent<AudioSource>();
            }
        }

        feedbackAudioSource.playOnAwake = false;
        feedbackAudioSource.loop = false;
        feedbackAudioSource.spatialBlend = 0f;
        feedbackAudioSource.volume = 1f;

        scoreBlipClip = CreateToneClip("ScoreBlip", 860f, 0.055f, 0.22f);
        recordBlipClip = CreateToneClip("RecordBlip", 1180f, 0.12f, 0.26f);
    }

    private void AutoAssignFeedbackClips()
    {
#if UNITY_EDITOR
        if (gameStartClip == null)
        {
            gameStartClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/foxboytails-game-start-317318.mp3");
        }

        if (gameOverClip == null)
        {
            gameOverClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/alphix-game-over-417465.mp3");
        }
#endif
    }

    private void PlayGameStartSound()
    {
        if (feedbackAudioSource != null && gameStartClip != null)
        {
            feedbackAudioSource.PlayOneShot(gameStartClip, gameStartVolume);
        }
    }

    private void PlayGameOverSound()
    {
        if (feedbackAudioSource != null && gameOverClip != null)
        {
            feedbackAudioSource.PlayOneShot(gameOverClip, gameOverVolume);
        }
    }

    private AudioClip CreateToneClip(string clipName, float frequency, float duration, float volume)
    {
        const int sampleRate = 44100;
        int sampleCount = Mathf.CeilToInt(sampleRate * duration);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float time = i / (float)sampleRate;
            float progress = i / (float)(sampleCount - 1);
            float envelope = Mathf.Sin(progress * Mathf.PI);
            samples[i] = Mathf.Sin(2f * Mathf.PI * frequency * time) * envelope * volume;
        }

        AudioClip clip = AudioClip.Create(clipName, sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private void PlayScoreFeedback(int amount, bool isNewRecord)
    {
        if (scorePulseRoutine != null)
        {
            StopCoroutine(scorePulseRoutine);
        }

        scorePulseRoutine = StartCoroutine(ScorePulseRoutine(isNewRecord));

        if (scorePopupText != null)
        {
            if (scorePopupRoutine != null)
            {
                StopCoroutine(scorePopupRoutine);
            }

            scorePopupRoutine = StartCoroutine(ScorePopupRoutine(amount, isNewRecord));
        }

        if (feedbackAudioSource != null)
        {
            AudioClip clip = isNewRecord ? recordBlipClip : scoreBlipClip;
            if (clip != null)
            {
                feedbackAudioSource.PlayOneShot(clip);
            }
        }
    }

    private IEnumerator ScorePulseRoutine(bool isNewRecord)
    {
        if (scoreText == null)
        {
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < scorePopDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / scorePopDuration);
            float scale = Mathf.Lerp(scorePopScale, 1f, progress);
            scoreText.transform.localScale = scoreTextBaseScale * scale;
            yield return null;
        }

        scoreText.transform.localScale = scoreTextBaseScale;

        if (isNewRecord && bestScoreText != null)
        {
            float recordElapsed = 0f;
            while (recordElapsed < 0.24f)
            {
                recordElapsed += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(recordElapsed / 0.24f);
                float scale = 1f + Mathf.Sin(progress * Mathf.PI) * 0.18f;
                bestScoreText.transform.localScale = bestScoreTextBaseScale * scale;
                yield return null;
            }

            bestScoreText.transform.localScale = bestScoreTextBaseScale;
        }

        scorePulseRoutine = null;
    }

    private IEnumerator ScorePopupRoutine(int amount, bool isNewRecord)
    {
        scorePopupText.text = "+" + amount;
        scorePopupText.color = isNewRecord ? new Color(1f, 0.88f, 0.28f, 1f) : new Color(0.45f, 1f, 0.68f, 1f);

        if (recordPopupText != null && isNewRecord)
        {
            recordPopupText.text = "NUEVO RECORD";
        }

        RectTransform rect = scorePopupText.rectTransform;
        Vector2 startPosition = new Vector2(154f, -38f);
        Vector2 endPosition = startPosition + new Vector2(0f, 28f);
        float duration = isNewRecord ? 0.72f : 0.45f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            float fadeProgress = Mathf.SmoothStep(0f, 1f, progress);
            rect.anchoredPosition = Vector2.Lerp(startPosition, endPosition, progress);
            Color color = scorePopupText.color;
            color.a = Mathf.Lerp(1f, 0f, fadeProgress);
            scorePopupText.color = color;

            if (recordPopupText != null && isNewRecord)
            {
                Color recordColor = recordPopupText.color;
                recordColor.a = progress < 0.72f ? 1f : Mathf.Lerp(1f, 0f, (progress - 0.72f) / 0.28f);
                recordPopupText.color = recordColor;
                float scale = 1f + Mathf.Sin(progress * Mathf.PI) * 0.08f;
                recordPopupText.transform.localScale = Vector3.one * scale;
            }

            yield return null;
        }

        scorePopupText.text = "";
        rect.anchoredPosition = startPosition;

        if (recordPopupText != null)
        {
            recordPopupText.text = "";
            recordPopupText.transform.localScale = Vector3.one;
            Color recordColor = recordPopupText.color;
            recordColor.a = 0f;
            recordPopupText.color = recordColor;
        }

        scorePopupRoutine = null;
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

        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.RemoveListener(ReturnToMainMenu);
            mainMenuButton.onClick.AddListener(ReturnToMainMenu);
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
        EnsureMainMenuButton();
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
            containerRect.sizeDelta = new Vector2(620f, 58f);

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
        StyleMenuButton(mainMenuButton, "Menu", new Color(0.55f, 0.65f, 0.82f, 1f));
        StyleMenuButton(exitButton, "Salir", new Color(0.95f, 0.4f, 0.45f, 1f));
    }

    private void EnsureMainMenuButton()
    {
        if (mainMenuButton != null)
        {
            return;
        }

        Transform buttonParent = restartButton != null ? restartButton.transform.parent : gameOverMenu.transform;
        Transform existing = buttonParent.Find("MainMenuButton");
        if (existing != null)
        {
            mainMenuButton = existing.GetComponent<Button>();
            if (mainMenuButton != null)
            {
                return;
            }
        }

        GameObject buttonObject = new GameObject("MainMenuButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(LayoutElement));
        buttonObject.transform.SetParent(buttonParent, false);
        mainMenuButton = buttonObject.GetComponent<Button>();

        GameObject labelObject = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        labelObject.transform.SetParent(buttonObject.transform, false);
        TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
        label.text = "Menu";
        label.alignment = TextAlignmentOptions.Center;
        label.fontSize = 22f;
        label.fontStyle = FontStyles.Bold;
        label.color = Color.white;

        RectTransform labelRect = label.rectTransform;
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        if (restartButton != null)
        {
            buttonObject.transform.SetSiblingIndex(restartButton.transform.GetSiblingIndex() + 1);
        }

        mainMenuButton.onClick.AddListener(ReturnToMainMenu);
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
        layoutElement.preferredWidth = 150f;
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

        if (mainMenuButton != null)
            mainMenuButton.gameObject.SetActive(false);

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
        reachedNewRecord = false;
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
