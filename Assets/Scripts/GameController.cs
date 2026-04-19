using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameController : MonoBehaviour
{
    private const string DifficultyKey = "SelectedDifficulty";
    private const string BestScoreKey = "BestScore";
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
    private float startDistance;
    private int selectedDifficulty;
    private int selectedGameMode;
    private CanvasGroup gameOverCanvasGroup;
    private Vector3 startTextBaseScale = Vector3.one;

    void Start()
    {
        GameAudioSettings.Apply();
        selectedDifficulty = PlayerPrefs.GetInt(DifficultyKey, 1);
        selectedGameMode = PlayerPrefs.GetInt(GameModeKey, 0);
        SetDifficulty(selectedDifficulty);
        bestScore = PlayerPrefs.GetInt(BestScoreKey, 0);
        startDistance = drone != null ? drone.position.z : 0f;

        RegisterButtonCallbacks();
        SetupMenuCanvasGroup();
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
            UpdateScore();
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
        UpdateScore();

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
                "<size=60><color=red>Game Over!</color></size>\n\n" +
                "Tiempo: " + elapsedTime.ToString("F2") + "s\n" +
                "Puntuacion: " + currentScore + "\n" +
                "Record: " + bestScore + "\n\n" +
                "Presiona Espacio, Enter o el boton para reiniciar.";
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
                "<size=60><color=yellow>Pausa</color></size>\n\n" +
                "Tiempo: " + elapsedTime.ToString("F2") + "s\n" +
                "Puntuacion: " + currentScore + "\n" +
                "Record: " + bestScore + "\n\n" +
                "Presiona ESC o el boton para reanudar.";
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
        StartCoroutine(ReloadSceneWithFade());
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

    private void UpdateScore()
    {
        int timeScore = Mathf.FloorToInt(elapsedTime * 10f);
        int distanceScore = 0;

        if (drone != null)
        {
            distanceScore = Mathf.Max(0, Mathf.FloorToInt((drone.position.z - startDistance) * 2f));
        }

        int difficultyBonus = (selectedDifficulty + 1) * 25;
        int modeBonus = GetModeScoreBonus();
        currentScore = timeScore + distanceScore + difficultyBonus + modeBonus;
    }

    private void RefreshHud()
    {
        if (timerText != null)
        {
            timerText.text = "Tiempo: " + elapsedTime.ToString("F2") + "s";
        }

        if (scoreText != null)
        {
            scoreText.text = "Puntuacion: " + currentScore;
        }

        if (bestScoreText != null)
        {
            bestScoreText.text = "Record: " + bestScore;
        }

        if (gameModeText != null)
        {
            gameModeText.text = "Modo: " + GetModeName();
        }
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

    private int GetModeScoreBonus()
    {
        switch (selectedGameMode)
        {
            case 1:
                return 10;
            case 2:
                return 40;
            default:
                return 20;
        }
    }

    private string GetModeName()
    {
        switch (selectedGameMode)
        {
            case 1:
                return "Zen";
            case 2:
                return "Rush";
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
