using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    private const string DifficultyKey = "SelectedDifficulty";
    private const string ControlModeKey = "UseGyroscope";
    private const string SkinKey = "SelectedSkin";
    private const string BestScoreKey = "BestScore";
    private const string GameModeKey = "SelectedGameMode";

    private static readonly List<string> GameModeNames = new List<string>
    {
        "Clasico",
        "Zen",
        "Rush"
    };

    private static readonly List<string> SkinNames = new List<string>
    {
        "Clasico",
        "Naranja",
        "Verde",
        "Azul",
        "Negro",
        "Blanco y Negro"
    };

    public Dropdown difficultyDropdown;
    public Dropdown skinDropdown;
    public Dropdown modeDropdown;
    public GameObject startMenu;
    public Text controlStatusText;
    public Text bestScoreText;
    public Text skinPreviewText;
    public Text modePreviewText;
    public Text volumeStatusText;
    public Slider volumeSlider;
    public Toggle muteToggle;
    public Image transitionOverlay;
    public float transitionDuration = 0.35f;
    public float menuIntroDuration = 0.55f;
    public float startButtonPulseAmount = 0.05f;
    public float startButtonPulseSpeed = 2.2f;
    public float previewFloatAmount = 4f;
    public float previewFloatSpeed = 1.8f;
    public Button startButton;

    private bool useGyroscope;
    private bool isTransitioning;
    private Vector3 startButtonBaseScale;
    private RectTransform[] floatingPreviewRects;
    private Vector2[] floatingPreviewBasePositions;
    private CanvasGroup startMenuCanvasGroup;

    void Start()
    {
        if (difficultyDropdown != null)
        {
            difficultyDropdown.value = PlayerPrefs.GetInt(DifficultyKey, 1);
        }

        CacheFloatingPreviewTexts();
        SetupSkinDropdown();
        SetupModeDropdown();
        SetupAudioControls();

        useGyroscope = PlayerPrefs.GetInt(ControlModeKey, 0) == 1;
        UpdateControlStatusText();
        UpdateBestScoreText();
        UpdateSkinPreviewText();
        UpdateModePreviewText();
        UpdateVolumeStatusText();

        if (startButton != null)
        {
            startButton.onClick.RemoveListener(StartGame);
            startButton.onClick.AddListener(StartGame);
            startButtonBaseScale = startButton.transform.localScale;
        }

        SetupMenuCanvasGroup();
        SetOverlayAlpha(1f);
        StartCoroutine(IntroRoutine());
    }

    void Update()
    {
        AnimateStartButton();
        AnimatePreviewTexts();
    }

    public void StartGame()
    {
        if (isTransitioning)
        {
            return;
        }

        if (difficultyDropdown == null)
        {
            Debug.LogError("No hay un Dropdown de dificultad asignado.");
            return;
        }

        int difficultyIndex = difficultyDropdown.value;
        int selectedSkin = skinDropdown != null ? skinDropdown.value : PlayerPrefs.GetInt(SkinKey, 0);
        int selectedMode = modeDropdown != null ? modeDropdown.value : PlayerPrefs.GetInt(GameModeKey, 0);

        PlayerPrefs.SetInt(DifficultyKey, difficultyIndex);
        PlayerPrefs.SetInt(ControlModeKey, useGyroscope ? 1 : 0);
        PlayerPrefs.SetInt(SkinKey, selectedSkin);
        PlayerPrefs.SetInt(GameModeKey, selectedMode);
        PlayerPrefs.Save();

        StartCoroutine(LoadSceneWithFade("GameScene"));
    }

    public void ToggleControlMode()
    {
        useGyroscope = !useGyroscope;
        PlayerPrefs.SetInt(ControlModeKey, useGyroscope ? 1 : 0);
        PlayerPrefs.Save();
        UpdateControlStatusText();
    }

    public void OnSkinChanged()
    {
        if (skinDropdown == null)
        {
            return;
        }

        PlayerPrefs.SetInt(SkinKey, skinDropdown.value);
        PlayerPrefs.Save();
        UpdateSkinPreviewText();
    }

    public void OnModeChanged()
    {
        if (modeDropdown == null)
        {
            return;
        }

        PlayerPrefs.SetInt(GameModeKey, modeDropdown.value);
        PlayerPrefs.Save();
        UpdateModePreviewText();
    }

    public void OnVolumeChanged()
    {
        if (volumeSlider == null)
        {
            return;
        }

        GameAudioSettings.SetMasterVolume(volumeSlider.value);
        UpdateVolumeStatusText();
    }

    public void OnMuteChanged()
    {
        if (muteToggle == null)
        {
            return;
        }

        GameAudioSettings.SetMuted(muteToggle.isOn);
        UpdateVolumeStatusText();
    }

    private void CacheFloatingPreviewTexts()
    {
        List<RectTransform> rects = new List<RectTransform>();

        AddPreviewRect(rects, bestScoreText);
        AddPreviewRect(rects, skinPreviewText);
        AddPreviewRect(rects, modePreviewText);
        AddPreviewRect(rects, volumeStatusText);

        floatingPreviewRects = rects.ToArray();
        floatingPreviewBasePositions = new Vector2[floatingPreviewRects.Length];

        for (int i = 0; i < floatingPreviewRects.Length; i++)
        {
            floatingPreviewBasePositions[i] = floatingPreviewRects[i].anchoredPosition;
        }
    }

    private void AddPreviewRect(List<RectTransform> rects, Text textComponent)
    {
        if (textComponent != null)
        {
            RectTransform rect = textComponent.rectTransform;
            if (rect != null)
            {
                rects.Add(rect);
            }
        }
    }

    private void SetupSkinDropdown()
    {
        if (skinDropdown == null)
        {
            return;
        }

        int selectedSkin = Mathf.Clamp(PlayerPrefs.GetInt(SkinKey, 0), 0, SkinNames.Count - 1);

        if (skinDropdown.options.Count != SkinNames.Count)
        {
            skinDropdown.ClearOptions();
            skinDropdown.AddOptions(SkinNames);
        }

        skinDropdown.onValueChanged.RemoveListener(OnSkinDropdownValueChanged);
        skinDropdown.value = selectedSkin;
        skinDropdown.onValueChanged.AddListener(OnSkinDropdownValueChanged);
    }

    private void SetupModeDropdown()
    {
        if (modeDropdown == null)
        {
            return;
        }

        int selectedMode = Mathf.Clamp(PlayerPrefs.GetInt(GameModeKey, 0), 0, GameModeNames.Count - 1);

        if (modeDropdown.options.Count != GameModeNames.Count)
        {
            modeDropdown.ClearOptions();
            modeDropdown.AddOptions(GameModeNames);
        }

        modeDropdown.onValueChanged.RemoveListener(OnModeDropdownValueChanged);
        modeDropdown.value = selectedMode;
        modeDropdown.onValueChanged.AddListener(OnModeDropdownValueChanged);
    }

    private void SetupAudioControls()
    {
        GameAudioSettings.Apply();

        if (volumeSlider != null)
        {
            volumeSlider.onValueChanged.RemoveListener(OnVolumeSliderValueChanged);
            volumeSlider.minValue = 0f;
            volumeSlider.maxValue = 1f;
            volumeSlider.value = GameAudioSettings.GetMasterVolume();
            volumeSlider.onValueChanged.AddListener(OnVolumeSliderValueChanged);
        }

        if (muteToggle != null)
        {
            muteToggle.onValueChanged.RemoveListener(OnMuteToggleValueChanged);
            muteToggle.isOn = GameAudioSettings.GetMuted();
            muteToggle.onValueChanged.AddListener(OnMuteToggleValueChanged);
        }
    }

    private void SetupMenuCanvasGroup()
    {
        if (startMenu == null)
        {
            return;
        }

        startMenuCanvasGroup = startMenu.GetComponent<CanvasGroup>();
        if (startMenuCanvasGroup == null)
        {
            startMenuCanvasGroup = startMenu.AddComponent<CanvasGroup>();
        }

        startMenuCanvasGroup.alpha = 0f;
        startMenuCanvasGroup.interactable = false;
        startMenuCanvasGroup.blocksRaycasts = false;
    }

    private void UpdateControlStatusText()
    {
        if (controlStatusText == null)
        {
            return;
        }

        if (useGyroscope && SystemInfo.supportsGyroscope)
        {
            controlStatusText.text = "Control: Giroscopio";
        }
        else if (useGyroscope)
        {
            controlStatusText.text = "Control: Teclado (sin giroscopio)";
        }
        else
        {
            controlStatusText.text = "Control: Teclado";
        }
    }

    private void UpdateBestScoreText()
    {
        if (bestScoreText != null)
        {
            bestScoreText.text = "Record: " + PlayerPrefs.GetInt(BestScoreKey, 0);
        }
    }

    private void UpdateSkinPreviewText()
    {
        if (skinPreviewText != null)
        {
            int selectedSkin = skinDropdown != null ? skinDropdown.value : PlayerPrefs.GetInt(SkinKey, 0);
            selectedSkin = Mathf.Clamp(selectedSkin, 0, SkinNames.Count - 1);
            skinPreviewText.text = "Skin: " + SkinNames[selectedSkin];
        }
    }

    private void UpdateModePreviewText()
    {
        if (modePreviewText != null)
        {
            int selectedMode = modeDropdown != null ? modeDropdown.value : PlayerPrefs.GetInt(GameModeKey, 0);
            selectedMode = Mathf.Clamp(selectedMode, 0, GameModeNames.Count - 1);
            modePreviewText.text = "Modo: " + GameModeNames[selectedMode];
        }
    }

    private void UpdateVolumeStatusText()
    {
        if (volumeStatusText == null)
        {
            return;
        }

        if (GameAudioSettings.GetMuted())
        {
            volumeStatusText.text = "Audio: Silenciado";
            return;
        }

        int volumePercent = Mathf.RoundToInt(GameAudioSettings.GetMasterVolume() * 100f);
        volumeStatusText.text = "Audio: " + volumePercent + "%";
    }

    private void OnSkinDropdownValueChanged(int value)
    {
        OnSkinChanged();
    }

    private void OnModeDropdownValueChanged(int value)
    {
        OnModeChanged();
    }

    private void OnVolumeSliderValueChanged(float value)
    {
        OnVolumeChanged();
    }

    private void OnMuteToggleValueChanged(bool value)
    {
        OnMuteChanged();
    }

    private IEnumerator IntroRoutine()
    {
        float elapsed = 0f;

        while (elapsed < menuIntroDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / menuIntroDuration);
            float eased = Mathf.SmoothStep(0f, 1f, progress);

            SetOverlayAlpha(1f - eased);
            if (startMenuCanvasGroup != null)
            {
                startMenuCanvasGroup.alpha = eased;
            }

            yield return null;
        }

        SetOverlayAlpha(0f);
        if (startMenuCanvasGroup != null)
        {
            startMenuCanvasGroup.alpha = 1f;
            startMenuCanvasGroup.interactable = true;
            startMenuCanvasGroup.blocksRaycasts = true;
        }
    }

    private IEnumerator LoadSceneWithFade(string sceneName)
    {
        isTransitioning = true;

        if (startMenuCanvasGroup != null)
        {
            startMenuCanvasGroup.interactable = false;
            startMenuCanvasGroup.blocksRaycasts = false;
        }

        float elapsed = 0f;
        while (elapsed < transitionDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / transitionDuration);
            float eased = Mathf.SmoothStep(0f, 1f, progress);

            if (startMenuCanvasGroup != null)
            {
                startMenuCanvasGroup.alpha = 1f - eased;
            }

            SetOverlayAlpha(eased);
            yield return null;
        }

        SceneManager.LoadScene(sceneName);
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

    private void AnimateStartButton()
    {
        if (startButton == null || isTransitioning)
        {
            return;
        }

        float pulse = 1f + (Mathf.Sin(Time.unscaledTime * startButtonPulseSpeed) * startButtonPulseAmount);
        startButton.transform.localScale = startButtonBaseScale * pulse;
    }

    private void AnimatePreviewTexts()
    {
        if (floatingPreviewRects == null)
        {
            return;
        }

        for (int i = 0; i < floatingPreviewRects.Length; i++)
        {
            if (floatingPreviewRects[i] == null)
            {
                continue;
            }

            float offset = Mathf.Sin((Time.unscaledTime * previewFloatSpeed) + i) * previewFloatAmount;
            floatingPreviewRects[i].anchoredPosition = floatingPreviewBasePositions[i] + new Vector2(0f, offset);
        }
    }
}

public static class GameAudioSettings
{
    private const string MasterVolumeKey = "MasterVolume";
    private const string MutedKey = "Muted";

    public static float GetMasterVolume()
    {
        return PlayerPrefs.GetFloat(MasterVolumeKey, 0.8f);
    }

    public static bool GetMuted()
    {
        return PlayerPrefs.GetInt(MutedKey, 0) == 1;
    }

    public static void SetMasterVolume(float value)
    {
        value = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat(MasterVolumeKey, value);
        PlayerPrefs.Save();
        Apply();
    }

    public static void SetMuted(bool value)
    {
        PlayerPrefs.SetInt(MutedKey, value ? 1 : 0);
        PlayerPrefs.Save();
        Apply();
    }

    public static void Apply()
    {
        AudioListener.volume = GetMuted() ? 0f : GetMasterVolume();
    }
}
